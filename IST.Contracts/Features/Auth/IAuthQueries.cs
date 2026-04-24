using ActualLab.Fusion;
using IST.Core.Entities.Auth;

namespace IST.Contracts.Features.Auth;

public interface IAuthQueries : IComputeService
{
    [ComputeMethod]
    Task<UserEntity?> GetUserByLoginAsync(string login, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<UserEntity?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<List<UserEntity>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<RoleEntity?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<List<RoleEntity>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<UserEntity?> GetUserByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
}
