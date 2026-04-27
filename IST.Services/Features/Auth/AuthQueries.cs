using IST.Contracts.Features.Auth;
using IST.Core.Entities.Auth;
using IST.Shared.DTOs.Auth;

namespace IST.Services.Features.Auth;

public class AuthQueries : IAuthQueries
{
    private readonly DbHub<AppDbContext> _dbHub;

    public AuthQueries(DbHub<AppDbContext> dbHub) => _dbHub = dbHub;

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<UserDto?> GetUserByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .Include(ur => ur.UserRoles)
            .ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
        return user == null ? null : MapToUserDto(user);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user == null ? null : MapToUserDto(user);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var users = await dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync(cancellationToken);
        return users.Select(MapToUserDto).ToList();
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<RoleDto?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var role = await dbContext.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return role == null ? null : MapToRoleDto(role);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var roles = await dbContext.Roles.AsNoTracking()
            .ToListAsync(cancellationToken);
        return roles.Select(MapToRoleDto).ToList();
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<UserDto?> GetUserByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user == null ? null : MapToUserDto(user);
    }

    private static UserDto MapToUserDto(UserEntity user)
    {
        return new UserDto
        {
            Id = user.Id,
            Surname = user.Surname,
            Name = user.Name,
            Patronymic = user.Patronymic,
            FullName = user.FullName,
            Position = user.Position,
            Department = user.Department,
            OrganizationId = user.OrganizationId,
            EMail = user.EMail,
            PhoneNumber = user.PhoneNumber,
            Login = user.Login,
            PasswordExpiryDate = user.PasswordExpiryDate,
            LastDateLogin = user.LastDateLogin,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            IsDeleted = user.IsDeleted,
            UserRoles = user.UserRoles
                .Where(ur => !ur.IsDeleted)
                .Select(ur => new UserRoleResponseDTO
                {
                    Id = ur.Id,
                    UserId = ur.UserId,
                    UserFullName = user.FullName,
                    RoleId = ur.RoleId,
                    RoleName = ur.Role?.Name ?? "",
                    StartDate = ur.StartDate,
                    EndDate = ur.EndDate
                })
                .ToList()
        };
    }

    private static RoleDto MapToRoleDto(RoleEntity role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            IsDeleted = role.IsDeleted
        };
    }
}
