using ActualLab.CommandR.Configuration;
using ActualLab.Fusion.Authentication;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Audit;
using IST.Core.Entities.Auth;
using IST.Core.Entities.BaseEntities;
using IST.Infrastructure.Security;
using IST.Services.Features.Audit;
using IST.Services.Features.Auth.Authentication;
using MapsterMapper;

namespace IST.Services.Features.Auth;

public class AuthCommands : IAuthCommands
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IAuthQueries _queries;
    private readonly IAuth _auth;
    private readonly IMapper _mapper;
    private readonly ICurrentUserStore _users;
    private readonly IAuditService _audit;

    public AuthCommands(
        DbHub<AppDbContext> dbHub,
        IAuthQueries queries,
        IAuth auth,
        IMapper mapper,
        ICurrentUserStore users,
        IAuditService audit)
    {
        _dbHub = dbHub;
        _queries = queries;
        _auth = auth;
        _mapper = mapper;
        _users = users;
        _audit = audit;
    }

    /// <summary>
    /// Возвращает CallerContext для текущей Fusion-сессии или <c>null</c> — без выброса
    /// исключений. Используется только для аудита (актор может быть неизвестен).
    /// </summary>
    private async ValueTask<CallerContext?> TryGetCallerAsync(Session session, CancellationToken ct)
        => await _users.TryFindCallerAsync(_auth, session, ct);

    /// <summary>
    /// Активирует <see cref="AuditContext"/> с UserId текущего вызывающего —
    /// чтобы SaveChangesAsync проставил CreatedBy/UpdatedBy/DeletedBy.
    /// Использовать <c>using var _ = await BeginAuditScopeAsync(command.Session, ct);</c>
    /// в начале каждого пишущего обработчика.
    /// </summary>
    private async ValueTask<IDisposable> BeginAuditScopeAsync(Session session, CancellationToken ct)
    {
        var caller = await TryGetCallerAsync(session, ct);
        return AuditContext.Begin(caller?.UserId);
    }
    // Auth
    [CommandHandler]
    public virtual async Task<ResponseDTO<SessionUserDto>> LoginAsync(
        LoginCommand command, CancellationToken cancellationToken = default)
    {
        // LoginAsync не инвалидирует кэш — факт входа не меняет
        // ни список пользователей, ни ролей. Блок обязателен, иначе
        // Fusion вызовет метод второй раз в режиме инвалидации.
        if (Invalidation.IsActive)
            return default!;

        try
        {
            // Используем обычный (НЕ операционный) контекст для чтения —
            // CreateOperationDbContext пишет в _Operations при каждом Save,
            // что запускает NpgsqlWatcher и инвалидирует весь кэш.
            await using var readCtx = await _dbHub.CreateDbContext(cancellationToken);

            var normalizedLogin = command.Login.ToLower().Trim();
            var user = await readCtx.Users.AsNoTracking()
                .Include(u => u.UserRoles.Where(ur => !ur.IsDeleted))
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Login == normalizedLogin, cancellationToken);

            if (user is null || !PasswordUtils.VerifyPassword(command.Password, user.Password))
            {
                await _audit.LogAsync(new AuditEntry(
                    EventType: SecurityAuditEventType.LoginFailed,
                    Success: false,
                    ActorUserId: user?.Id,
                    ActorLogin: command.Login,
                    IpAddress: command.IpAddress,
                    UserAgent: command.UserAgent,
                    Message: user is null ? "Пользователь не найден" : "Неверный пароль"
                ), cancellationToken);

                return new()
                {
                    Status = false,
                    StatusMessage = "Неверный логин или пароль.",
                    StatusCode = ResponseStatusCode.Unauthorized
                };
            }

            if (!user.IsActive)
            {
                await _audit.LogAsync(new AuditEntry(
                    EventType: SecurityAuditEventType.LoginInactive,
                    Success: false,
                    ActorUserId: user.Id,
                    ActorLogin: user.Login,
                    IpAddress: command.IpAddress,
                    UserAgent: command.UserAgent,
                    Message: "Учётная запись отключена"
                ), cancellationToken);

                return new()
                {
                    Status = false,
                    StatusMessage = "Ваша учётная запись отключена. Свяжитесь с администратором.",
                    StatusCode = ResponseStatusCode.Forbidden
                };
            }

            // Проверка срока действия пароля
            if (user.PasswordExpiryDate <= DateTime.UtcNow)
            {
                await _audit.LogAsync(new AuditEntry(
                    EventType: SecurityAuditEventType.LoginPasswordExpired,
                    Success: false,
                    ActorUserId: user.Id,
                    ActorLogin: user.Login,
                    IpAddress: command.IpAddress,
                    UserAgent: command.UserAgent,
                    Message: "Срок действия пароля истёк"
                ), cancellationToken);

                return new()
                {
                    Status = false,
                    StatusMessage = "Срок действия пароля истёк. Необходимо сменить пароль.",
                    StatusCode = ResponseStatusCode.PasswordExpired,
                    Data = new SessionUserDto
                    {
                        Id = user.Id,
                        Login = user.Login,
                        FullName = user.FullName,
                    }
                };
            }

            // Обновляем LastDateLogin через отдельный обычный контекст.
            // Это side-effect, не бизнес-операция — не должен создавать
            // запись в _Operations и тем самым инвалидировать кэш.
            await using var writeCtx = await _dbHub.CreateDbContext(true, cancellationToken);
            var writableUser = await writeCtx.Users
                .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
            if (writableUser != null)
            {
                writableUser.LastDateLogin = DateTime.UtcNow;
                await writeCtx.SaveChangesAsync(cancellationToken);
            }

            var activeUserRoles = user.UserRoles
                .Where(ur => ur.StartDate <= DateTime.UtcNow
                          && (ur.EndDate == null || ur.EndDate > DateTime.UtcNow))
                .ToList();

            var activeRoles = activeUserRoles
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToList();

            var permissionsSet = activeUserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Where(rp => !rp.IsDeleted)
                .Select(rp => rp.Permission.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Регистрируем CallerContext в серверном реестре активных сессий.
            // По UserId — последующие RPC-команды резолвят user-id через
            // Fusion IAuth.GetUser(session) и достают этот CallerContext.
            _users.Set(user.Id, new CallerContext(
                UserId: user.Id,
                Login: user.Login,
                FullName: user.FullName,
                Roles: activeRoles,
                Permissions: permissionsSet)
            {
                IpAddress = command.IpAddress,
                UserAgent = command.UserAgent,
            });

            await _audit.LogAsync(new AuditEntry(
                EventType: SecurityAuditEventType.LoginSuccess,
                Success: true,
                ActorUserId: user.Id,
                ActorLogin: user.Login,
                IpAddress: command.IpAddress,
                UserAgent: command.UserAgent
            ), cancellationToken);

            return new()
            {
                Status = true,
                StatusMessage = "Вход выполнен успешно.",
                StatusCode = ResponseStatusCode.Ok,
                Data = new SessionUserDto
                {
                    Id = user.Id,
                    Login = user.Login,
                    FullName = user.FullName,
                    Email = user.EMail,
                    IsActive = user.IsActive,
                    Roles = activeRoles,
                    Permissions = permissionsSet.ToList(),
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw new Exception(ex.ToString());
        }
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> LogoutAsync(
        LogoutCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
            return default!;

        var caller = _users.Find(command.UserId);
        _users.Remove(command.UserId);

        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.Logout,
            Success: true,
            ActorUserId: command.UserId,
            ActorLogin: caller?.Login,
            IpAddress: caller?.IpAddress,
            UserAgent: caller?.UserAgent
        ), cancellationToken);

        return new ResponseDTO<string>
        {
            Status = true,
            StatusMessage = "Выход выполнен.",
            StatusCode = ResponseStatusCode.Ok,
        };
    }

    // Users
    [CommandHandler]
    public virtual async Task<ResponseDTO<UserResponseDTO>> CreateUserAsync(
     CreateUserCommand command,
     CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        // 1. Инвалидация: Fusion вызывает метод второй раз для инвалидации кэша
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            // Id ещё не знаем (генерируется в entity), поэтому инвалидируем только список
            return default!;
        }

        // 2. Валидация сложности пароля
        var passwordValidation = PasswordUtils.ValidateStrength(request.Password);
        if (!passwordValidation.IsValid)
        {
            return new ResponseDTO<UserResponseDTO>
            {
                Status = false,
                StatusMessage = passwordValidation.ErrorMessage!,
                Data = null
            };
        }

        // 3. Открываем транзакционный контекст
        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        // 4. Проверка уникальности Login
        var normalizedLogin = request.Login.ToLower().Trim();
        var loginExists = await dbContext.Users
            .AnyAsync(u => u.Login == normalizedLogin, cancellationToken);

        if (loginExists)
        {
            return new ResponseDTO<UserResponseDTO>
            {
                Status = false,
                StatusMessage = "Пользователь с таким логином уже существует",
                Data = null
            };
        }

        // 5. Проверка уникальности Email
        var normalizedEmail = request.EMail.ToLower().Trim();
        var emailExists = await dbContext.Users
            .AnyAsync(u => u.EMail.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return new ResponseDTO<UserResponseDTO>
            {
                Status = false,
                StatusMessage = "Пользователь с таким email уже существует",
                Data = null
            };
        }

        // 7. Создаём сущность
        var userEntity = new UserEntity
        {
            Surname = request.Surname.Trim(),
            Name = request.Name.Trim(),
            Patronymic = string.IsNullOrWhiteSpace(request.Patronymic)
                ? null
                : request.Patronymic.Trim(),
            Position = request.Position.Trim(),
            OrganizationId = request.Organization,
            Department = request.Department.Trim(),
            EMail = normalizedEmail,
            PhoneNumber = request.PhoneNumber.Trim(),
            IsActive = request.IsActive,

            Login = normalizedLogin,
            Password = PasswordUtils.HashPassword(request.Password),
            PasswordExpiryDate = DateTime.UtcNow.AddMonths(6),

            LastDateLogin = null,
        };

        await dbContext.Users.AddAsync(userEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var caller = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserCreated,
            Success: true,
            ActorUserId: caller?.UserId,
            ActorLogin: caller?.Login,
            TargetUserId: userEntity.Id,
            TargetLogin: userEntity.Login
        ), cancellationToken);

        // 8. Формируем ответ
        return new ResponseDTO<UserResponseDTO>
        {
            Status = true,
            StatusMessage = "Пользователь успешно создан",
            Data = _mapper.Map<UserResponseDTO>(userEntity)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<UserResponseDTO>> UpdateUserAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        // 1. Инвалидация
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByIdAsync(request.Id, default);
            _ = _queries.GetUserByIdWithRolesAsync(request.Id, default);
            return default!;
        }

        // 2. Открываем транзакционный контекст
        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        // 3. Находим пользователя
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            return new ResponseDTO<UserResponseDTO>
            {
                Status = false,
                StatusMessage = $"Пользователь с Id={request.Id} не найден",
                Data = null
            };
        }

        // 4. Если меняется email — проверить уникальность
        var normalizedEmail = request.EMail.ToLower().Trim();
        if (!string.Equals(user.EMail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await dbContext.Users
                .AnyAsync(u => u.Id != request.Id
                            && u.EMail.ToLower() == normalizedEmail,
                          cancellationToken);

            if (emailExists)
            {
                return new ResponseDTO<UserResponseDTO>
                {
                    Status = false,
                    StatusMessage = "Пользователь с таким email уже существует",
                    Data = null
                };
            }
        }

        user.Surname = request.Surname.Trim();
        user.Name = request.Name.Trim();
        user.Patronymic = string.IsNullOrWhiteSpace(request.Patronymic)
            ? null
            : request.Patronymic.Trim();
        user.Position = request.Position.Trim();
        user.OrganizationId = request.Organization;
        user.Department = request.Department;
        user.EMail = normalizedEmail;
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserUpdated,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            TargetUserId: user.Id,
            TargetLogin: user.Login
        ), cancellationToken);

        // 7. Ответ
        return new ResponseDTO<UserResponseDTO>
        {
            Status = true,
            StatusMessage = "Данные пользователя успешно обновлены",
            Data = _mapper.Map<UserResponseDTO>(user)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteUserAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Инвалидация
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByIdAsync(command.UserId, default);
            _ = _queries.GetUserByIdWithRolesAsync(command.UserId, default);
            _ = _queries.GetUserByLoginAsync(string.Empty, default);
            return default!;
        }

        // 2. ПОЛУЧАЕМ ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ ИЗ СЕССИИ
        var currentUser = await _auth.GetUser(command.Session, cancellationToken);

        // Предполагаю, что command.UserId у тебя Guid или число, поэтому приводим к строке:
        bool isSelfDelete = currentUser?.Id == command.UserId.ToString();

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var userToDelete = await dbContext.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        // Выполняем проверку и сразу получаем сообщение и код статуса
        (string message, ResponseStatusCode statusCode) = (true) switch
        {
            _ when userToDelete is null
               => ("Ресурс не найден", ResponseStatusCode.NotFound),

            // Проверка: попытка удалить самого себя
            _ when isSelfDelete
                => ("Нельзя удалить собственную учётную запись.", ResponseStatusCode.ValidationError),

            // В остальных случаях считаем, что валидация прошла успешно
            _ => ("Валидация пройдена успешно.", ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode,
            };
        }

        userToDelete.IsDeleted = true;

        foreach (var userRole in userToDelete.UserRoles)
        {
            userRole.IsDeleted = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Принудительно выкидываем все сессии удалённого пользователя.
        _users.Remove(userToDelete.Id);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserDeleted,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            TargetUserId: userToDelete.Id,
            TargetLogin: userToDelete.Login
        ), cancellationToken);

        return new()
        {
            Status = true,
            StatusMessage = "Ok",
            Data = userToDelete.FullName
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> ChangeUserPasswordAsync(ChangeUserPasswordCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Инвалидация Fusion
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByLoginAsync(command.Request.Login, default);
            return default!;
        }

        // 2. Валидация формы (без БД)
        var strengthCheck = PasswordUtils.ValidateStrength(command.Request.NewPassword);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when command.Request.NewPassword != command.Request.ConfirmPassword
                => ("Новый пароль и его подтверждение не совпадают.", ResponseStatusCode.ValidationError),

            _ when command.Request.CurrentPassword == command.Request.NewPassword
                => ("Новый пароль должен отличаться от текущего.", ResponseStatusCode.ValidationError),

            _ when !strengthCheck.IsValid
                => (strengthCheck.ErrorMessage!, ResponseStatusCode.ValidationError),

            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }

        // 3. Проверки с БД
        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Login == command.Request.Login, cancellationToken);

        var passValid = user != null
            && PasswordUtils.VerifyPassword(command.Request.CurrentPassword, user.Password);

        (message, statusCode) = true switch
        {
            _ when user is null || !passValid
                => ("Неверный логин или пароль.", ResponseStatusCode.Unauthorized),

            _ when !user.IsActive
                => ("Ваша учётная запись отключена. Пожалуйста, свяжитесь с администратором.",
                    ResponseStatusCode.Unauthorized),

            _ when PasswordUtils.VerifyPassword(command.Request.NewPassword, user.Password)
                => ("Новый пароль должен отличаться от текущего.", ResponseStatusCode.ValidationError),

            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }

        // 4. Обновляем пароль
        user.Password = PasswordUtils.HashPassword(command.Request.NewPassword);
        user.PasswordExpiryDate = DateTime.UtcNow.AddMonths(6);

        await dbContext.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserPasswordChanged,
            Success: true,
            ActorUserId: user.Id,
            ActorLogin: user.Login,
            TargetUserId: user.Id,
            TargetLogin: user.Login
        ), cancellationToken);

        return new()
        {
            Data = user.Login,
            Status = true,
            StatusMessage = "Пароль успешно изменён.",
            StatusCode = ResponseStatusCode.Ok
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> ResetUserPasswordAsync(ResetUserPasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByLoginAsync(command.Request.Login, default);
            return default!;
        }

        // Сразу деконструируем результат в переменные с понятными именами
        (string message, ResponseStatusCode statusCode) = true switch
        {
            // Проверка на совпадение с подтверждением
            _ when command.Request.ConfirmPassword != command.Request.NewPassword
                => ("Новый пароль и его подтверждение не совпадают.", ResponseStatusCode.ValidationError),

            // Случай по умолчанию: все проверки пройдены
            _ => ("Валидация пройдена успешно.", ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode,
            };
        }

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Login == command.Request.Login, cancellationToken);


        (message, statusCode) = true switch
        {
            _ when user is null
                => ("Неверный логин", ResponseStatusCode.NotFound),

            _ when !user.IsActive
                => ("Ваша учетная запись отключена. Пожалуйста, свяжитесь с администратором.", ResponseStatusCode.Forbidden),

            // Случай по умолчанию: все проверки пройдены
            _ => ("Валидация пройдена успешно.", ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }
        else
        {
            user.Password = PasswordUtils.HashPassword(command.Request.NewPassword);
            user.PasswordExpiryDate = command.Request.ResetPassword ? DateTime.UtcNow.AddMonths(-1) : DateTime.UtcNow.AddMonths(6);

            dbContext.Users.Update(user);

            await dbContext.SaveChangesAsync(cancellationToken);

            var actor = await TryGetCallerAsync(command.Session, cancellationToken);
            await _audit.LogAsync(new AuditEntry(
                EventType: SecurityAuditEventType.UserPasswordReset,
                Success: true,
                ActorUserId: actor?.UserId,
                ActorLogin: actor?.Login,
                TargetUserId: user.Id,
                TargetLogin: user.Login
            ), cancellationToken);

            return new()
            {
                Data = user.Login,
                Status = true,
                StatusMessage = "Пароль успешно изменен",
                StatusCode = ResponseStatusCode.Ok
            };
        }

    }

    // Roles
    [CommandHandler]
    public virtual async Task<ResponseDTO<RoleResponseDTO>> CreateRoleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Инвалидация
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllRolesAsync(default);
            return default!;
        }
        // 3. Открываем контекст
        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var normalizedName = command.Request.Name.ToLower().Trim();

        // 4. Валидация: проверка уникальности имени
        var nameExists = await dbContext.Roles
            .AnyAsync(r => r.Name == normalizedName, cancellationToken);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when nameExists
                => ("Роль с таким названием уже существует.", ResponseStatusCode.ValidationError),

            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }

        // 5. Создаём роль — CreatedBy/At проставятся через ApplyAudit
        var role = new RoleEntity
        {
            Name = normalizedName,
            Description = command.Request.Description.Trim()
        };

        await dbContext.Roles.AddAsync(role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.RoleCreated,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            Message: $"Создана роль '{role.Name}'",
            DetailsJson: $"{{\"roleId\":\"{role.Id}\",\"roleName\":\"{role.Name}\"}}"
        ), cancellationToken);

        // 6. Формируем ответ
        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно создана.",
            StatusCode = ResponseStatusCode.Ok,
            Data = _mapper.Map<RoleResponseDTO>(role)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<RoleResponseDTO>> UpdateRoleAsync(UpdateRoleCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Инвалидация
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllRolesAsync(default);
            _ = _queries.GetRoleByIdAsync(command.Request.Id, default);
            _ = _queries.GetAllUsersAsync(default); // Role name change affects UserDto.UserRoles
            return default!;
        }


        // 3. Находим роль
        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == command.Request.Id, cancellationToken);

        var normalizedName = command.Request.Name.ToLower().Trim();

        // 4. Проверка уникальности имени (если изменилось)
        var nameConflict = role != null
            && !string.Equals(role.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
            && await dbContext.Roles.AnyAsync(r =>
                r.Id != command.Request.Id && r.Name == normalizedName, cancellationToken);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when role is null
                => ("Роль не найдена.", ResponseStatusCode.NotFound),

            _ when nameConflict
                => ("Роль с таким названием уже существует.", ResponseStatusCode.ValidationError),

            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }

        // 5. Обновляем поля — UpdatedAt/By проставятся через ApplyAudit
        role!.Name = normalizedName;
        role.Description = command.Request.Description.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.RoleUpdated,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            Message: $"Обновлена роль '{role.Name}'",
            DetailsJson: $"{{\"roleId\":\"{role.Id}\",\"roleName\":\"{role.Name}\"}}"
        ), cancellationToken);

        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно обновлена.",
            StatusCode = ResponseStatusCode.Ok,
            Data = _mapper.Map<RoleResponseDTO>(role)
        };

    }
    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteRoleAsync(DeleteRoleCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Инвалидация
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllRolesAsync(default);
            _ = _queries.GetRoleByIdAsync(command.RoleId, default);
            _ = _queries.GetAllUsersAsync(default);  // список юзеров тоже затронут
            return default!;
        }

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var roleToDelete = await dbContext.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        var isSystemRole = roleToDelete != null
            && (roleToDelete.Name == "superadmin" || roleToDelete.Name == "admin");

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when roleToDelete is null
                => ("Роль не найдена.", ResponseStatusCode.NotFound),

            _ when isSystemRole
                => ("Нельзя удалить системную роль.", ResponseStatusCode.ValidationError),

            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }

        dbContext.UserRoles.RemoveRange(roleToDelete!.UserRoles);
        dbContext.Roles.Remove(roleToDelete);

        await dbContext.SaveChangesAsync(cancellationToken);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.RoleDeleted,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            Message: $"Удалена роль '{roleToDelete.Name}'",
            DetailsJson: $"{{\"roleId\":\"{roleToDelete.Id}\",\"roleName\":\"{roleToDelete.Name}\"}}"
        ), cancellationToken);

        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно удалена.",
            StatusCode = ResponseStatusCode.Ok,
            Data = roleToDelete.Name
        };

    }

    // UserRoles
    [CommandHandler]
    public virtual async Task<ResponseDTO<UserRoleResponseDTO>> CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Инвалидация
        if (Invalidation.IsActive)
        {
            _ = _queries.GetUserByIdAsync(command.Request.UserId, default);
            _ = _queries.GetUserByIdWithRolesAsync(command.Request.UserId, default);
            _ = _queries.GetAllUsersAsync(default);
            return default!;
        }

        var startDate = command.Request.StartDate ?? DateTime.UtcNow;
        var endDate = command.Request.EndDate;

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var userExists = await dbContext.Users
            .AnyAsync(u => u.Id == command.Request.UserId, cancellationToken);

        var role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == command.Request.RoleId, cancellationToken);

        var duplicateExists = await dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == command.Request.UserId
                         && ur.RoleId == command.Request.RoleId
                         && (ur.EndDate == null || ur.EndDate > DateTime.UtcNow),
                      cancellationToken);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when !userExists
                => ("Пользователь не найден.", ResponseStatusCode.NotFound),

            _ when role is null
                => ("Роль не найдена.", ResponseStatusCode.NotFound),

            _ when endDate.HasValue && endDate.Value <= startDate
                => ("Дата окончания должна быть позже даты начала.", ResponseStatusCode.ValidationError),

            _ when duplicateExists
                => ("У пользователя уже есть эта роль.", ResponseStatusCode.ValidationError),

            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }

        // 6. Создаём связь — аудит-поля проставит ApplyAudit в SaveChangesAsync
        var userRole = new UserRolesEntity
        {
            UserId = command.Request.UserId,
            RoleId = command.Request.RoleId,
            StartDate = startDate,
            EndDate = endDate
        };

        await dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 7. Подгружаем данные пользователя для ответа
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.Request.UserId, cancellationToken);

        // Изменение состава ролей — выкидываем CallerContext пользователя, чтобы
        // перечитал permission'ы при следующем входе. (Хотя реальный пересчёт
        // permissions для уже-онлайн-сессии пока не делаем — это TODO.)
        _users.Remove(command.Request.UserId);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserRoleAssigned,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            TargetUserId: command.Request.UserId,
            TargetLogin: user?.Login,
            Message: $"Назначена роль '{role!.Name}'",
            DetailsJson: $"{{\"roleId\":\"{role.Id}\",\"roleName\":\"{role.Name}\"}}"
        ), cancellationToken);

        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно назначена.",
            StatusCode = ResponseStatusCode.Ok,
            Data = _mapper.Map<UserRoleResponseDTO>(userRole)
        };

    }
    [CommandHandler]
    public virtual async Task<ResponseDTO<UserRoleResponseDTO>> UpdateUserRoleAsync(UpdateUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Инвалидация
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByIdAsync(command.Request.UserId, default);
            _ = _queries.GetUserByIdWithRolesAsync(command.Request.UserId, default);
            return default!;
        }
      
        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var userRole = await dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.Id == command.Request.Id, cancellationToken);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when userRole is null
                => ("Назначение роли не найдено.", ResponseStatusCode.NotFound),

            _ when command.Request.EndDate.HasValue
                && command.Request.EndDate.Value <= command.Request.StartDate
                => ("Дата окончания должна быть позже даты начала.", ResponseStatusCode.ValidationError),

            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }

        userRole!.StartDate = command.Request.StartDate;
        userRole.EndDate = command.Request.EndDate;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Изменение периода действия роли → пересмотр прав при следующем входе.
        _users.Remove(userRole.UserId);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserRoleUpdated,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            TargetUserId: userRole.UserId,
            TargetLogin: userRole.User?.Login,
            Message: $"Изменён период роли '{userRole.Role?.Name}'",
            DetailsJson: $"{{\"roleId\":\"{userRole.RoleId}\",\"roleName\":\"{userRole.Role?.Name}\",\"startDate\":\"{userRole.StartDate:O}\",\"endDate\":\"{userRole.EndDate:O}\"}}"
        ), cancellationToken);

        return new()
        {
            Status = true,
            StatusMessage = "Период действия роли обновлён.",
            StatusCode = ResponseStatusCode.Ok,
            Data = _mapper.Map<UserRoleResponseDTO>(userRole)
        };

    }
    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteUserRoleAsync(DeleteUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByIdAsync(command.UserId, default);
            _ = _queries.GetUserByIdWithRolesAsync(command.UserId, default);
            return default!;
        }

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var userRole = await dbContext.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.Id == command.UserRoleId, cancellationToken);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when userRole is null
                => ("Назначение роли не найдено.", ResponseStatusCode.NotFound),

            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
        {
            return new()
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };
        }

        dbContext.UserRoles.Remove(userRole!);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Отзыв роли — выкидываем CallerContext пользователя.
        _users.Remove(command.UserId);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserRoleRevoked,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            TargetUserId: command.UserId,
            Message: $"Отозвана роль '{userRole!.Role?.Name}'",
            DetailsJson: $"{{\"roleId\":\"{userRole.RoleId}\",\"roleName\":\"{userRole.Role?.Name}\"}}"
        ), cancellationToken);

        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно отозвана.",
            StatusCode = ResponseStatusCode.Ok,
            Data = userRole!.Role?.Name ?? ""
        };
    }

    // Привилегии ролей
    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> UpdateRolePermissionsAsync(
        UpdateRolePermissionsCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetPermissionIdsByRoleAsync(command.RoleId, default);
            _ = _queries.GetAllRolesAsync(default);
            return default!;
        }

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        if (role is null)
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = "Роль не найдена.",
                StatusCode = ResponseStatusCode.NotFound,
            };

        // admin-роль защищаем: ей всегда даём все permission'ы.
        var isAdmin = string.Equals(role.Name, "admin", StringComparison.OrdinalIgnoreCase);

        Guid[] targetIds;
        if (isAdmin)
        {
            targetIds = await dbContext.Permissions
                .Select(p => p.Id)
                .ToArrayAsync(cancellationToken);
        }
        else
        {
            // Отфильтруем мусор: оставим только реально существующие PermissionId.
            var validIds = await dbContext.Permissions
                .Where(p => command.PermissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToArrayAsync(cancellationToken);
            targetIds = validIds;
        }

        var existing = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();
        var targetSet = targetIds.ToHashSet();

        var toAdd = targetSet.Except(existingIds)
            .Select(pid => new RolePermissionEntity { RoleId = role.Id, PermissionId = pid })
            .ToList();
        var toRemove = existing.Where(rp => !targetSet.Contains(rp.PermissionId)).ToList();

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            return new ResponseDTO<string>
            {
                Status = true,
                StatusMessage = "Изменений нет.",
                StatusCode = ResponseStatusCode.Ok,
                Data = role.Name,
            };
        }

        if (toAdd.Count > 0)
            await dbContext.RolePermissions.AddRangeAsync(toAdd, cancellationToken);
        if (toRemove.Count > 0)
            dbContext.RolePermissions.RemoveRange(toRemove);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Сбрасываем CallerContext всех пользователей этой роли — пусть перечитают
        // permission'ы при следующем входе.
        var affectedUserIds = await dbContext.UserRoles
            .Where(ur => ur.RoleId == role.Id && !ur.IsDeleted)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var uid in affectedUserIds)
            _users.Remove(uid);

        var actor = await TryGetCallerAsync(command.Session, cancellationToken);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.RoleUpdated,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            Message: $"Изменён набор привилегий роли '{role.Name}' (+{toAdd.Count}/-{toRemove.Count})",
            DetailsJson: $"{{\"roleId\":\"{role.Id}\",\"roleName\":\"{role.Name}\",\"added\":{toAdd.Count},\"removed\":{toRemove.Count},\"total\":{targetSet.Count}}}"
        ), cancellationToken);

        return new ResponseDTO<string>
        {
            Status = true,
            StatusMessage = $"Привилегии роли «{role.Name}» обновлены.",
            StatusCode = ResponseStatusCode.Ok,
            Data = role.Name,
        };
    }
}
