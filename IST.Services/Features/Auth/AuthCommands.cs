using ActualLab.CommandR.Configuration;
using ActualLab.Fusion.Authentication;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Auth;
using IST.Infrastructure.Security;
using IST.Shared.DTOs.Auth;
using IST.Shared.Enums;

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
    public virtual async Task<ResponseDTO<UserResponseDTO>> UpdateUserAsync(
      UpdateUserCommand command,
      CancellationToken cancellationToken = default)
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
    public virtual async Task<ResponseDTO<string>> DeleteUserAsync(DeleteUserCommand command, Session session,CancellationToken cancellationToken = default)
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
        var currentUser = await _auth.GetUser(session, cancellationToken);

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
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByLoginAsync(command.Request.Login, default);
            return default!;
        }
        // Сразу деконструируем результат в переменные с понятными именами
        (string message, ResponseStatusCode statusCode) = true switch
        {
            // Проверка на совпадение со старым паролем
            _ when command.Request.CurrentPassword == command.Request.NewPassword
                => ("Новый пароль не должен совпадать с текущим.", ResponseStatusCode.ValidationError),

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

        var passValid = PasswordUtils.VerifyPassword(command.Request.CurrentPassword, user?.Password ?? null);

        (message, statusCode) = true switch
        {
            _ when user is null
                => ("Неверный логин", ResponseStatusCode.NotFound),

            _ when !user.IsActive
                => ("Ваша учетная запись отключена. Пожалуйста, свяжитесь с администратором.", ResponseStatusCode.Unauthorized),

            _ when !passValid
                => ("Неверный пароль", ResponseStatusCode.Unauthorized),

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
            user.PasswordExpiryDate = DateTime.UtcNow.AddMonths(3);

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
    public virtual async Task<RoleEntity> CreateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        await dbContext.Roles.AddAsync(role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateRoleCache(role);

        return role;
    }

    [CommandHandler]
    public virtual async Task<RoleEntity> UpdateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        dbContext.Roles.Update(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateRoleCache(role);

        return role;
    }

    [CommandHandler]
    public virtual async Task<bool> DeleteRoleAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        dbContext.Roles.Remove(role);
        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        if (changes > 0)
            InvalidateRoleCache(role);

        return changes > 0;
    }    

    // UserRoles
    [CommandHandler]
    public virtual async Task<UserRolesEntity> CreateUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        await dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateUserRoleCache(userRole);

        return userRole;
    }

    [CommandHandler]
    public virtual async Task<bool> DeleteUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        dbContext.UserRoles.Remove(userRole);
        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        if (changes > 0)
            InvalidateUserRoleCache(userRole);

        return changes > 0;
    }

    // --- Вспомогательные методы для инвалидации кэша ---
    private void InvalidateUserCache(UserEntity user)
    {
        using (Invalidation.Begin())
        {
            _ = _queries.GetAllUsersAsync(default);
            _ = _queries.GetUserByLoginAsync(user.Login, default);
            _ = _queries.GetUserByIdAsync(user.Id, default);
        }
    }
    private void InvalidateRoleCache(RoleEntity role)
    {
        using (Invalidation.Begin())
        {
            _ = _queries.GetAllRolesAsync(default);
            _ = _queries.GetRoleByIdAsync(role.Id, default);
        }
    }
    private void InvalidateUserRoleCache(UserRolesEntity userRole)
    {
        using (Invalidation.Begin())
        {
            _ = _queries.GetUserByIdAsync(userRole.UserId, default);
            _ = _queries.GetAllUsersAsync(default);
        }
    }
}
