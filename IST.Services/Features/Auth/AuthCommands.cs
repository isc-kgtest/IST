using ActualLab.CommandR.Configuration;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Audit;
using IST.Core.Entities.Auth;
using IST.Core.Entities.BaseEntities;
using IST.Infrastructure.Security;
using IST.Services.Features.Audit;
using IST.Services.Features.Auth.Authentication;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace IST.Services.Features.Auth;

public class AuthCommands : IAuthCommands
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IAuthQueries _queries;
    private readonly IMapper _mapper;
    private readonly ICurrentUserStore _users;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthCommands> _log;

    public AuthCommands(
        DbHub<AppDbContext> dbHub,
        IAuthQueries queries,
        IMapper mapper,
        ICurrentUserStore users,
        IAuditService audit,
        ILogger<AuthCommands> log)
    {
        _dbHub = dbHub;
        _queries = queries;
        _mapper = mapper;
        _users = users;
        _audit = audit;
        _log = log;
    }

    /// <summary>
    /// Активирует ambient <see cref="AuditContext"/> с UserId текущего вызывающего —
    /// чтобы <c>SaveChangesAsync</c> проставил CreatedBy/UpdatedBy/DeletedBy.
    /// Полностью синхронный (lookup в памяти).
    /// </summary>
    private IDisposable BeginAuditScope(Session session)
        => AuditContext.Begin(_users.Find(session)?.UserId);

    // ==================== Auth ====================

    [CommandHandler]
    public virtual async Task<ResponseDTO<SessionUserDto>> LoginAsync(
        LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
            return default!;

        _log.LogInformation("LoginAsync: incoming command.Session.Id='{Sid}', login='{Login}'",
            command.Session.Id, command.Login);

        if (command.Session == Session.Default || string.IsNullOrEmpty(command.Session.Id))
            return new ResponseDTO<SessionUserDto>
            {
                Status = false,
                StatusMessage = "Неверная сессия.",
                StatusCode = ResponseStatusCode.BadRequest,
            };

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

        // Активные роли + объединение их permission'ов.
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

        // Гейт по permission'у портала (если задан).
        if (!string.IsNullOrEmpty(command.RequiredPermission)
            && !permissionsSet.Contains(command.RequiredPermission))
        {
            await _audit.LogAsync(new AuditEntry(
                EventType: SecurityAuditEventType.LoginFailed,
                Success: false,
                ActorUserId: user.Id,
                ActorLogin: user.Login,
                IpAddress: command.IpAddress,
                UserAgent: command.UserAgent,
                Message: $"Отсутствует право '{command.RequiredPermission}' для входа в портал"
            ), cancellationToken);

            return new()
            {
                Status = false,
                StatusMessage = "У вас нет доступа к этому порталу.",
                StatusCode = ResponseStatusCode.Forbidden,
            };
        }

        // Обновляем LastDateLogin через side-effect DbContext (не пишет в _Operations).
        await using var writeCtx = await _dbHub.CreateDbContext(true, cancellationToken);
        var writableUser = await writeCtx.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        if (writableUser != null)
        {
            writableUser.LastDateLogin = DateTime.UtcNow;
            await writeCtx.SaveChangesAsync(cancellationToken);
        }

        _log.LogInformation(
            "LoginAsync: storing CallerContext under Session.Id='{Sid}' for user '{Login}' (UserId={UserId})",
            command.Session.Id, user.Login, user.Id);

        // Связываем Session с CallerContext в памяти. Сессия выписывается HTTP-эндпоинтом
        // и параллельно идёт в cookie (PrimarySid) и в этот RPC-вызов.
        _users.Set(command.Session, new CallerContext(
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

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> LogoutAsync(
        LogoutCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
            return default!;

        var caller = _users.Find(command.Session);
        _users.Remove(command.Session);

        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.Logout,
            Success: true,
            ActorUserId: caller?.UserId,
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

    // ==================== Users ====================

    [CommandHandler]
    public virtual async Task<ResponseDTO<UserResponseDTO>> CreateUserAsync(
        CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            return default!;
        }

        _ = _users.RequirePermission(command.Session, Permissions.UsersCreate);

        var request = command.Request;

        var passwordValidation = PasswordUtils.ValidateStrength(request.Password);
        if (!passwordValidation.IsValid)
            return new ResponseDTO<UserResponseDTO>
            {
                Status = false,
                StatusMessage = passwordValidation.ErrorMessage!,
            };

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var normalizedLogin = request.Login.ToLower().Trim();
        if (await dbContext.Users.AnyAsync(u => u.Login == normalizedLogin, cancellationToken))
            return new ResponseDTO<UserResponseDTO>
            {
                Status = false,
                StatusMessage = "Пользователь с таким логином уже существует",
            };

        var normalizedEmail = request.EMail.ToLower().Trim();
        if (await dbContext.Users.AnyAsync(u => u.EMail.ToLower() == normalizedEmail, cancellationToken))
            return new ResponseDTO<UserResponseDTO>
            {
                Status = false,
                StatusMessage = "Пользователь с таким email уже существует",
            };

        var userEntity = new UserEntity
        {
            Surname = request.Surname.Trim(),
            Name = request.Name.Trim(),
            Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim(),
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

        var caller = _users.Find(command.Session);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserCreated,
            Success: true,
            ActorUserId: caller?.UserId,
            ActorLogin: caller?.Login,
            TargetUserId: userEntity.Id,
            TargetLogin: userEntity.Login,
            IpAddress: caller?.IpAddress,
            UserAgent: caller?.UserAgent
        ), cancellationToken);

        return new ResponseDTO<UserResponseDTO>
        {
            Status = true,
            StatusMessage = "Пользователь успешно создан",
            Data = _mapper.Map<UserResponseDTO>(userEntity)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<UserResponseDTO>> UpdateUserAsync(
        UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByIdAsync(request.Id, default);
            _ = _queries.GetUserByIdWithRolesAsync(request.Id, default);
            return default!;
        }

        _ = _users.RequirePermission(command.Session, Permissions.UsersUpdate);

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null)
            return new ResponseDTO<UserResponseDTO>
            {
                Status = false,
                StatusMessage = $"Пользователь с Id={request.Id} не найден",
            };

        var normalizedEmail = request.EMail.ToLower().Trim();
        if (!string.Equals(user.EMail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await dbContext.Users.AnyAsync(
                u => u.Id != request.Id && u.EMail.ToLower() == normalizedEmail, cancellationToken);
            if (emailExists)
                return new ResponseDTO<UserResponseDTO>
                {
                    Status = false,
                    StatusMessage = "Пользователь с таким email уже существует",
                };
        }

        user.Surname = request.Surname.Trim();
        user.Name = request.Name.Trim();
        user.Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim();
        user.Position = request.Position.Trim();
        user.OrganizationId = request.Organization;
        user.Department = request.Department ?? string.Empty;
        user.EMail = normalizedEmail;
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Если пользователя деактивировали — снимаем все его сессии.
        if (!user.IsActive)
            _users.RemoveByUserId(user.Id);

        var actor = _users.Find(command.Session);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserUpdated,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            TargetUserId: user.Id,
            TargetLogin: user.Login,
            IpAddress: actor?.IpAddress,
            UserAgent: actor?.UserAgent
        ), cancellationToken);

        return new ResponseDTO<UserResponseDTO>
        {
            Status = true,
            StatusMessage = "Данные пользователя успешно обновлены",
            Data = _mapper.Map<UserResponseDTO>(user)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteUserAsync(
        DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByIdAsync(command.UserId, default);
            _ = _queries.GetUserByIdWithRolesAsync(command.UserId, default);
            _ = _queries.GetUserByLoginAsync(string.Empty, default);
            return default!;
        }

        var caller = _users.RequirePermission(command.Session, Permissions.UsersDelete);
        var isSelfDelete = caller.UserId == command.UserId;

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var userToDelete = await dbContext.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when userToDelete is null
                => ("Ресурс не найден", ResponseStatusCode.NotFound),
            _ when isSelfDelete
                => ("Нельзя удалить собственную учётную запись.", ResponseStatusCode.ValidationError),
            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode,
            };

        userToDelete!.IsDeleted = true;
        foreach (var userRole in userToDelete.UserRoles)
            userRole.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Принудительно снимаем все активные сессии удалённого пользователя.
        _users.RemoveByUserId(userToDelete.Id);

        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserDeleted,
            Success: true,
            ActorUserId: caller.UserId,
            ActorLogin: caller.Login,
            TargetUserId: userToDelete.Id,
            TargetLogin: userToDelete.Login,
            IpAddress: caller.IpAddress,
            UserAgent: caller.UserAgent
        ), cancellationToken);

        return new ResponseDTO<string>
        {
            Status = true,
            StatusMessage = "Ok",
            Data = userToDelete.FullName
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> ChangeUserPasswordAsync(
        ChangeUserPasswordCommand command, CancellationToken cancellationToken = default)
    {
        // ChangeUserPassword может вызываться ДО логина (пароль просрочен), поэтому
        // RequirePermission не зовём — защита через проверку текущего пароля.
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByLoginAsync(command.Request.Login, default);
            return default!;
        }

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
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode,
            };

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var user = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Login == command.Request.Login, cancellationToken);

        var passValid = user != null
            && PasswordUtils.VerifyPassword(command.Request.CurrentPassword, user.Password);

        (message, statusCode) = true switch
        {
            _ when user is null || !passValid
                => ("Неверный логин или пароль.", ResponseStatusCode.Unauthorized),
            _ when !user!.IsActive
                => ("Ваша учётная запись отключена. Пожалуйста, свяжитесь с администратором.",
                    ResponseStatusCode.Unauthorized),
            _ when PasswordUtils.VerifyPassword(command.Request.NewPassword, user.Password)
                => ("Новый пароль должен отличаться от текущего.", ResponseStatusCode.ValidationError),
            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode,
            };

        user!.Password = PasswordUtils.HashPassword(command.Request.NewPassword);
        user.PasswordExpiryDate = DateTime.UtcNow.AddMonths(6);

        await dbContext.SaveChangesAsync(cancellationToken);

        // После смены пароля выкидываем все сессии — пользователь зайдёт заново.
        _users.RemoveByUserId(user.Id);

        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserPasswordChanged,
            Success: true,
            ActorUserId: user.Id,
            ActorLogin: user.Login,
            TargetUserId: user.Id,
            TargetLogin: user.Login
        ), cancellationToken);

        return new ResponseDTO<string>
        {
            Data = user.Login,
            Status = true,
            StatusMessage = "Пароль успешно изменён.",
            StatusCode = ResponseStatusCode.Ok
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> ResetUserPasswordAsync(
        ResetUserPasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByLoginAsync(command.Request.Login, default);
            return default!;
        }

        var caller = _users.RequirePermission(command.Session, Permissions.UsersPasswordReset);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when command.Request.ConfirmPassword != command.Request.NewPassword
                => ("Новый пароль и его подтверждение не совпадают.", ResponseStatusCode.ValidationError),
            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode,
            };

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var user = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Login == command.Request.Login, cancellationToken);

        (message, statusCode) = true switch
        {
            _ when user is null
                => ("Неверный логин", ResponseStatusCode.NotFound),
            _ when !user!.IsActive
                => ("Ваша учетная запись отключена. Пожалуйста, свяжитесь с администратором.",
                    ResponseStatusCode.Forbidden),
            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode,
            };

        user!.Password = PasswordUtils.HashPassword(command.Request.NewPassword);
        user.PasswordExpiryDate = command.Request.ResetPassword
            ? DateTime.UtcNow.AddMonths(-1)
            : DateTime.UtcNow.AddMonths(6);

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        // После ресета — kick всех сессий пользователя.
        _users.RemoveByUserId(user.Id);

        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserPasswordReset,
            Success: true,
            ActorUserId: caller.UserId,
            ActorLogin: caller.Login,
            TargetUserId: user.Id,
            TargetLogin: user.Login,
            IpAddress: caller.IpAddress,
            UserAgent: caller.UserAgent
        ), cancellationToken);

        return new ResponseDTO<string>
        {
            Data = user.Login,
            Status = true,
            StatusMessage = "Пароль успешно изменен",
            StatusCode = ResponseStatusCode.Ok
        };
    }

    // ==================== Roles ====================

    [CommandHandler]
    public virtual async Task<ResponseDTO<RoleResponseDTO>> CreateRoleAsync(
        CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllRolesAsync(default);
            return default!;
        }

        _log.LogInformation("CreateRoleAsync: incoming command.Session.Id='{Sid}'", command.Session.Id);

        _ = _users.RequirePermission(command.Session, Permissions.RolesManage);

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var normalizedName = command.Request.Name.ToLower().Trim();

        if (await dbContext.Roles.AnyAsync(r => r.Name == normalizedName, cancellationToken))
            return new ResponseDTO<RoleResponseDTO>
            {
                Status = false,
                StatusMessage = "Роль с таким названием уже существует.",
                StatusCode = ResponseStatusCode.ValidationError
            };

        var role = new RoleEntity
        {
            Name = normalizedName,
            Description = command.Request.Description.Trim()
        };

        await dbContext.Roles.AddAsync(role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var actor = _users.Find(command.Session);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.RoleCreated,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            Message: $"Создана роль '{role.Name}'",
            DetailsJson: $"{{\"roleId\":\"{role.Id}\",\"roleName\":\"{role.Name}\"}}"
        ), cancellationToken);

        return new ResponseDTO<RoleResponseDTO>
        {
            Status = true,
            StatusMessage = "Роль успешно создана.",
            StatusCode = ResponseStatusCode.Ok,
            Data = _mapper.Map<RoleResponseDTO>(role)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<RoleResponseDTO>> UpdateRoleAsync(
        UpdateRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllRolesAsync(default);
            _ = _queries.GetRoleByIdAsync(command.Request.Id, default);
            _ = _queries.GetAllUsersAsync(default);
            return default!;
        }

        _ = _users.RequirePermission(command.Session, Permissions.RolesManage);

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var role = await dbContext.Roles.FirstOrDefaultAsync(
            r => r.Id == command.Request.Id, cancellationToken);

        var normalizedName = command.Request.Name.ToLower().Trim();
        var nameConflict = role != null
            && !string.Equals(role.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
            && await dbContext.Roles.AnyAsync(
                r => r.Id != command.Request.Id && r.Name == normalizedName, cancellationToken);

        (string message, ResponseStatusCode statusCode) = true switch
        {
            _ when role is null
                => ("Роль не найдена.", ResponseStatusCode.NotFound),
            _ when nameConflict
                => ("Роль с таким названием уже существует.", ResponseStatusCode.ValidationError),
            _ => (string.Empty, ResponseStatusCode.Ok)
        };

        if (statusCode != ResponseStatusCode.Ok)
            return new ResponseDTO<RoleResponseDTO>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };

        role!.Name = normalizedName;
        role.Description = command.Request.Description.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        var actor = _users.Find(command.Session);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.RoleUpdated,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            Message: $"Обновлена роль '{role.Name}'",
            DetailsJson: $"{{\"roleId\":\"{role.Id}\",\"roleName\":\"{role.Name}\"}}"
        ), cancellationToken);

        return new ResponseDTO<RoleResponseDTO>
        {
            Status = true,
            StatusMessage = "Роль успешно обновлена.",
            StatusCode = ResponseStatusCode.Ok,
            Data = _mapper.Map<RoleResponseDTO>(role)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteRoleAsync(
        DeleteRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllRolesAsync(default);
            _ = _queries.GetRoleByIdAsync(command.RoleId, default);
            _ = _queries.GetAllUsersAsync(default);
            return default!;
        }

        _ = _users.RequirePermission(command.Session, Permissions.RolesManage);

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var roleToDelete = await dbContext.Roles.Include(r => r.UserRoles)
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
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };

        var affectedUserIds = roleToDelete!.UserRoles.Select(ur => ur.UserId).Distinct().ToList();

        dbContext.UserRoles.RemoveRange(roleToDelete.UserRoles);
        dbContext.Roles.Remove(roleToDelete);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Снимаем сессии всех пользователей, у которых была эта роль.
        foreach (var uid in affectedUserIds)
            _users.RemoveByUserId(uid);

        var actor = _users.Find(command.Session);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.RoleDeleted,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            Message: $"Удалена роль '{roleToDelete.Name}'",
            DetailsJson: $"{{\"roleId\":\"{roleToDelete.Id}\",\"roleName\":\"{roleToDelete.Name}\"}}"
        ), cancellationToken);

        return new ResponseDTO<string>
        {
            Status = true,
            StatusMessage = "Роль успешно удалена.",
            StatusCode = ResponseStatusCode.Ok,
            Data = roleToDelete.Name
        };
    }

    // ==================== UserRoles ====================

    [CommandHandler]
    public virtual async Task<ResponseDTO<UserRoleResponseDTO>> CreateUserRoleAsync(
        CreateUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetUserByIdAsync(command.Request.UserId, default);
            _ = _queries.GetUserByIdWithRolesAsync(command.Request.UserId, default);
            _ = _queries.GetAllUsersAsync(default);
            return default!;
        }

        _ = _users.RequirePermission(command.Session, Permissions.RolesAssign);

        var startDate = command.Request.StartDate ?? DateTime.UtcNow;
        var endDate = command.Request.EndDate;

        using var __auditScope = BeginAuditScope(command.Session);
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
            return new ResponseDTO<UserRoleResponseDTO>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };

        var userRole = new UserRolesEntity
        {
            UserId = command.Request.UserId,
            RoleId = command.Request.RoleId,
            StartDate = startDate,
            EndDate = endDate
        };

        await dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.Request.UserId, cancellationToken);

        // Снимаем сессии пользователя — на следующем входе подтянутся новые роли.
        _users.RemoveByUserId(command.Request.UserId);

        var actor = _users.Find(command.Session);
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

        return new ResponseDTO<UserRoleResponseDTO>
        {
            Status = true,
            StatusMessage = "Роль успешно назначена.",
            StatusCode = ResponseStatusCode.Ok,
            Data = _mapper.Map<UserRoleResponseDTO>(userRole)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<UserRoleResponseDTO>> UpdateUserRoleAsync(
        UpdateUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByIdAsync(command.Request.UserId, default);
            _ = _queries.GetUserByIdWithRolesAsync(command.Request.UserId, default);
            return default!;
        }

        _ = _users.RequirePermission(command.Session, Permissions.RolesAssign);

        using var __auditScope = BeginAuditScope(command.Session);
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
            return new ResponseDTO<UserRoleResponseDTO>
            {
                Status = false,
                StatusMessage = message,
                StatusCode = statusCode
            };

        userRole!.StartDate = command.Request.StartDate;
        userRole.EndDate = command.Request.EndDate;
        await dbContext.SaveChangesAsync(cancellationToken);

        _users.RemoveByUserId(userRole.UserId);

        var actor = _users.Find(command.Session);
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

        return new ResponseDTO<UserRoleResponseDTO>
        {
            Status = true,
            StatusMessage = "Период действия роли обновлён.",
            StatusCode = ResponseStatusCode.Ok,
            Data = _mapper.Map<UserRoleResponseDTO>(userRole)
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteUserRoleAsync(
        DeleteUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByIdAsync(command.UserId, default);
            _ = _queries.GetUserByIdWithRolesAsync(command.UserId, default);
            return default!;
        }

        _ = _users.RequirePermission(command.Session, Permissions.RolesAssign);

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var userRole = await dbContext.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.Id == command.UserRoleId, cancellationToken);

        if (userRole is null)
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = "Назначение роли не найдено.",
                StatusCode = ResponseStatusCode.NotFound
            };

        dbContext.UserRoles.Remove(userRole);
        await dbContext.SaveChangesAsync(cancellationToken);

        _users.RemoveByUserId(command.UserId);

        var actor = _users.Find(command.Session);
        await _audit.LogAsync(new AuditEntry(
            EventType: SecurityAuditEventType.UserRoleRevoked,
            Success: true,
            ActorUserId: actor?.UserId,
            ActorLogin: actor?.Login,
            TargetUserId: command.UserId,
            Message: $"Отозвана роль '{userRole.Role?.Name}'",
            DetailsJson: $"{{\"roleId\":\"{userRole.RoleId}\",\"roleName\":\"{userRole.Role?.Name}\"}}"
        ), cancellationToken);

        return new ResponseDTO<string>
        {
            Status = true,
            StatusMessage = "Роль успешно отозвана.",
            StatusCode = ResponseStatusCode.Ok,
            Data = userRole.Role?.Name ?? ""
        };
    }

    // ==================== Role Permissions ====================

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

        _ = _users.RequirePermission(command.Session, Permissions.RolesManage);

        using var __auditScope = BeginAuditScope(command.Session);
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var role = await dbContext.Roles.FirstOrDefaultAsync(
            r => r.Id == command.RoleId, cancellationToken);

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
            targetIds = await dbContext.Permissions.Select(p => p.Id).ToArrayAsync(cancellationToken);
        }
        else
        {
            targetIds = await dbContext.Permissions
                .Where(p => command.PermissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToArrayAsync(cancellationToken);
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
            return new ResponseDTO<string>
            {
                Status = true,
                StatusMessage = "Изменений нет.",
                StatusCode = ResponseStatusCode.Ok,
                Data = role.Name,
            };

        if (toAdd.Count > 0)
            await dbContext.RolePermissions.AddRangeAsync(toAdd, cancellationToken);
        if (toRemove.Count > 0)
            dbContext.RolePermissions.RemoveRange(toRemove);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Сбрасываем сессии всех пользователей этой роли — новые permission'ы
        // подтянутся при следующем логине.
        var affectedUserIds = await dbContext.UserRoles
            .Where(ur => ur.RoleId == role.Id && !ur.IsDeleted)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var uid in affectedUserIds)
            _users.RemoveByUserId(uid);

        var actor = _users.Find(command.Session);
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
