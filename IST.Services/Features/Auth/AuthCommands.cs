using ActualLab.CommandR.Configuration;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Auth;
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
    public virtual async Task<ResponseDTO<UserResponseDTO>> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        var userEntity = new UserEntity
        {
            Surname = request.Surname,
            Name = request.Name,
            Patronymic = request.Patronymic,
            Position = request.Position,
            Organization = request.Organization,
            Department = request.Department,
            EMail = request.EMail,
            PhoneNumber = request.PhoneNumber,
            IsActive = request.IsActive,

            Login = request.Login.ToLower().Trim(),
            Password = PasswordUtils.HashPassword(request.Password),

            PasswordExpiryDate = DateTime.UtcNow.AddMonths(6),
            LastDateLogin = DateTime.UtcNow,
        };

        await dbContext.Users.AddAsync(userEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateUserCache(userEntity);

        // 4. Формируем ответ, который требует сигнатура метода
        var responseData = new UserResponseDTO
        {
            Id = userEntity.Id,
            Login = userEntity.Login,
            FullName = userEntity.FullName
        };

        return new ResponseDTO<UserResponseDTO>
        {
            Status = true,
            StatusMessage = "Пользователь успешно создан",
            Data = responseData
        };
    }

    [CommandHandler]
    public virtual async Task<UserEntity> UpdateUserAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateUserCache(user);

        return user;
    }
    [CommandHandler]
    public virtual async Task<bool> DeleteUserAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        dbContext.Users.Remove(user);
        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        if (changes > 0)
            InvalidateUserCache(user);

        return changes > 0;
    }

    [CommandHandler]
    public virtual async Task<string> ChangeUserPasswordAsync(Guid userId, string hashedNewPassword, DateTime passwordExpiryDate, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

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
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        await dbContext.Roles.AddAsync(role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateRoleCache(role);

        return role;
    }

    [CommandHandler]
    public virtual async Task<RoleEntity> UpdateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        dbContext.Roles.Update(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateRoleCache(role);

        return role;
    }

    [CommandHandler]
    public virtual async Task<bool> DeleteRoleAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

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
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        await dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateUserRoleCache(userRole);

        return userRole;
    }

    [CommandHandler]
    public virtual async Task<bool> DeleteUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

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
