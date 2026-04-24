using ActualLab.CommandR.Configuration;
using ActualLab.Fusion.Authentication;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Auth;
using IST.Infrastructure.Security;

namespace IST.Services.Features.Auth;

public class AuthCommands : IAuthCommands
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IAuthQueries _queries;
    private readonly IAuth _auth;

    public AuthCommands(DbHub<AppDbContext> dbHub, IAuthQueries queries, IAuth auth)
    {
        _dbHub = dbHub;
        _queries = queries;
        _auth = auth;
    }
    // Auth
    [CommandHandler]
    public virtual async Task<ResponseDTO<SessionUserDto>> LoginAsync(
        LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
            return default!;

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var normalizedLogin = command.Login.ToLower().Trim();
        var user = await dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles.Where(ur => !ur.IsDeleted))
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Login == normalizedLogin, cancellationToken);

        if (user is null || !PasswordUtils.VerifyPassword(command.Password, user.Password))
        {
            return new()
            {
                Status = false,
                StatusMessage = "Неверный логин или пароль.",
                StatusCode = ResponseStatusCode.Unauthorized
            };
        }

        if (!user.IsActive)
        {
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

        // Обновляем дату последнего входа
        var writableUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        if (writableUser != null)
        {
            writableUser.LastDateLogin = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var activeRoles = user.UserRoles
            .Where(ur => ur.StartDate <= DateTime.UtcNow
                      && (ur.EndDate == null || ur.EndDate > DateTime.UtcNow))
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

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
                Roles = activeRoles
            }
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

        // 8. Формируем ответ
        return new ResponseDTO<UserResponseDTO>
        {
            Status = true,
            StatusMessage = "Пользователь успешно создан",
            Data = new UserResponseDTO
            {
                Id = userEntity.Id,
                Login = userEntity.Login,
                FullName = userEntity.FullName
            }
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
            return default!;
        }

        // 2. Открываем транзакционный контекст
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

        // 7. Ответ
        return new ResponseDTO<UserResponseDTO>
        {
            Status = true,
            StatusMessage = "Данные пользователя успешно обновлены",
            Data = new UserResponseDTO
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName
            }
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
            _ = _queries.GetUserByLoginAsync(string.Empty, default);
            return default!;
        }

        // 2. ПОЛУЧАЕМ ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ ИЗ СЕССИИ
        var currentUser = await _auth.GetUser(command.Session, cancellationToken);

        // Предполагаю, что command.UserId у тебя Guid или число, поэтому приводим к строке:
        bool isSelfDelete = currentUser?.Id == command.UserId.ToString();

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

        // 6. Формируем ответ
        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно создана.",
            StatusCode = ResponseStatusCode.Ok,
            Data = new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            }
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
            return default!;
        }


        // 3. Находим роль
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

        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно обновлена.",
            StatusCode = ResponseStatusCode.Ok,
            Data = new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            }
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
            _ = _queries.GetAllUsersAsync(default);
            return default!;
        }

        var startDate = command.Request.StartDate ?? DateTime.UtcNow;
        var endDate = command.Request.EndDate;

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

        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно назначена.",
            StatusCode = ResponseStatusCode.Ok,
            Data = new UserRoleResponseDTO
            {
                Id = userRole.Id,
                UserId = userRole.UserId,
                UserFullName = user?.FullName ?? "",
                RoleId = userRole.RoleId,
                RoleName = role!.Name,
                StartDate = userRole.StartDate,
                EndDate = userRole.EndDate
            }
        };

    }
    [CommandHandler]
    public virtual async Task<ResponseDTO<UserRoleResponseDTO>> UpdateUserRoleAsync(UpdateUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Инвалидация
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            return default!;
        }
      
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

        return new()
        {
            Status = true,
            StatusMessage = "Период действия роли обновлён.",
            StatusCode = ResponseStatusCode.Ok,
            Data = new UserRoleResponseDTO
            {
                Id = userRole.Id,
                UserId = userRole.UserId,
                UserFullName = userRole.User?.FullName ?? "",
                RoleId = userRole.RoleId,
                RoleName = userRole.Role?.Name ?? "",
                StartDate = userRole.StartDate,
                EndDate = userRole.EndDate
            }
        };

    }
    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteUserRoleAsync(DeleteUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            return default!;
        }

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

        return new()
        {
            Status = true,
            StatusMessage = "Роль успешно отозвана.",
            StatusCode = ResponseStatusCode.Ok,
            Data = userRole!.Role?.Name ?? ""
        };

    }

}
