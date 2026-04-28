using IST.Contracts.Features.Auth;
using MapsterMapper;

namespace IST.Services.Features.Auth;

public class AuthQueries : IAuthQueries
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IMapper _mapper;

    public AuthQueries(DbHub<AppDbContext> dbHub, IMapper mapper)
    {
        _dbHub = dbHub;
        _mapper = mapper;
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<UserDto?> GetUserByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .Include(ur => ur.UserRoles)
            .ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var users = await dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<UserDto>>(users);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<RoleDto?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var role = await dbContext.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return role == null ? null : _mapper.Map<RoleDto>(role);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var roles = await dbContext.Roles.AsNoTracking()
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<RoleDto>>(roles);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<UserDto?> GetUserByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }
}
