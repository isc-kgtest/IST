using ActualLab.CommandR.Configuration;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Auth;
using IST.Infrastructure.Security;
using IST.Shared.DTOs.Auth;

namespace IST.Services.Features.Auth;

public class AuthCommands : IAuthCommands
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IAuthQueries _queries;

    public AuthCommands(DbHub<AppDbContext> dbHub, IAuthQueries queries)
    {
        _dbHub = dbHub;
        _queries = queries;
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
    public virtual async Task<bool> DeleteUserAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        dbContext.Users.Remove(user);
        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        if (changes > 0)
            InvalidateUserCache(user);

        return changes > 0;
    }

    [CommandHandler]
    public virtual async Task<string> ChangeUserPasswordAsync(Guid userId, string hashedNewPassword, DateTime passwordExpiryDate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var user = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user != null)
        {
            user.Password = hashedNewPassword;
            user.PasswordExpiryDate = passwordExpiryDate;

            await dbContext.SaveChangesAsync(cancellationToken);

            InvalidateUserCache(user);

            return user.Login;
        }

        return string.Empty;
    }

    [CommandHandler]
    public virtual async Task<string> ResetUserPasswordAsync(Guid userId, string hashedNewPassword, DateTime passwordExpiryDate, CancellationToken cancellationToken = default)
    {
        return await ChangeUserPasswordAsync(userId, hashedNewPassword, passwordExpiryDate, cancellationToken);
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
