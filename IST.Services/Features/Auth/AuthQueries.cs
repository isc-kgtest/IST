using IST.Contracts.Features.Auth;
using IST.Core.Entities.Auth;

namespace IST.Services.Features.Auth;

public class AuthQueries : IAuthQueries
{
    private readonly DbHub<AppDbContext> _dbHub;

    public AuthQueries(DbHub<AppDbContext> dbHub) => _dbHub = dbHub;

    [ComputeMethod]
    public virtual async Task<UserEntity?> GetUserByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .Include(ur => ur.UserRoles)
            .ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
        return user;
    }

    [ComputeMethod]
    public virtual async Task<UserEntity?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user;
    }

    [ComputeMethod]
    public virtual async Task<List<UserEntity>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var users = await dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync(cancellationToken);
        return users;
    }

    [ComputeMethod]
    public virtual async Task<RoleEntity?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var role = await dbContext.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return role;
    }

    [ComputeMethod]
    public virtual async Task<List<RoleEntity>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var roles = await dbContext.Roles.AsNoTracking()
            .ToListAsync(cancellationToken);
        return roles;
    }

    [ComputeMethod]
    public virtual async Task<UserEntity?> GetUserByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user;
    }
}
