using ActualLab.Fusion;
using IST.Core.Entities.Auth;

namespace IST.Contracts.Features.Auth;

public interface IAuthQueries : IComputeService
{
    [ComputeMethod(MinCacheDuration = 60)]
    Task<UserEntity?> GetUserByLoginAsync(string login, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<UserEntity?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<UserEntity>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<RoleEntity?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<RoleEntity>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 60)]
    Task<UserEntity?> GetUserByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
}
