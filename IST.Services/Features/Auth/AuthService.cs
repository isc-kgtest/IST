using ActualLab.Fusion;
using ActualLab.Fusion.EntityFramework;
using IST.Core.Entities.Auth;
using IST.Infrastructure.AppDbContext;
using IST.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace IST.Services.Features.Auth;

public class AuthService : IAuthService
{
    private readonly DbHub<AppDbContext> _dbHub;

    public AuthService(DbHub<AppDbContext> dbHub) => _dbHub = dbHub;

    //Queries
    [ComputeMethod]
    public virtual async Task<UserEntity?> GetUserByLoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users
            .Include(ur => ur.UserRoles)
            .ThenInclude(r=> r.Role)
            .FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
        return user;
    }

    [ComputeMethod]
    public virtual async Task<UserEntity?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user;
    }

    [ComputeMethod]
    public virtual async Task<List<UserEntity>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var users = await dbContext.Users.ToListAsync(cancellationToken);
        return users;
    }

    [ComputeMethod]
    public virtual async Task<RoleEntity?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return role;
    }

    [ComputeMethod]
    public virtual async Task<List<RoleEntity>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var roles = await dbContext.Roles.ToListAsync(cancellationToken);
        return roles;
    }

    //Commands
    //Users
    public virtual async Task<string> CreateUserAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
      
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user.Login;
    }
    public virtual async Task<UserEntity> UpdateUserAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }
    public virtual async Task<bool> DeleteUserAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        dbContext.Users.Remove(user);
        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }
    public virtual async Task<string> ChangeUserPasswordAsync(Guid userId, string hashedNewPassword, DateTime passwordExpiryDate, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        // Находим пользователя по ID
        var user = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user != null)
        {
            user.Password = hashedNewPassword; // Сюда передаем уже захешированный пароль из Handler
            user.PasswordExpiryDate = passwordExpiryDate;

            await dbContext.SaveChangesAsync(cancellationToken);
            return user.Login;
        }

        return string.Empty;
    }

    public virtual async Task<string> ResetUserPasswordAsync(Guid userId, string hashedNewPassword, DateTime passwordExpiryDate, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        var user = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user != null)
        {
            user.Password = hashedNewPassword;
            user.PasswordExpiryDate = passwordExpiryDate;

            await dbContext.SaveChangesAsync(cancellationToken);
            return user.Login;
        }

        return string.Empty;
    }

    //Roles
    public virtual async Task<RoleEntity> CreateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        await dbContext.Roles.AddAsync(role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return role;
    }
    public virtual async Task<RoleEntity> UpdateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        dbContext.Roles.Update(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        return role;
    }

    public virtual async Task<bool> DeleteRoleAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        dbContext.Roles.Remove(role);

        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }

    //UserRoles
    public virtual async Task<UserRolesEntity> CreateUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        await dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return userRole;
    }

    public virtual async Task<bool> DeleteUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        dbContext.UserRoles.Remove(userRole);
        var changes = await dbContext.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }
}