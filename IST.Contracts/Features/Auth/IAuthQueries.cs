using ActualLab.Fusion;
using IST.Core.Entities.Auth;
using IST.Shared.DTOs.Auth;

namespace IST.Contracts.Features.Auth;

public interface IAuthQueries : IComputeService
{
    [ComputeMethod(MinCacheDuration = 60)]
    Task<UserDto?> GetUserByLoginAsync(string login, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<RoleDto?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 60)]
    Task<UserDto?> GetUserByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
}
