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

    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<Guid>> GetPermissionIdsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Диагностический срез сессии: какой пользователь, его роли и permission'ы
    /// как их видит сервер. Помогает отладить отказы доступа.
    /// </summary>
    [ComputeMethod(MinCacheDuration = 5)]
    Task<WhoAmIDto> WhoAmIAsync(Session session, CancellationToken cancellationToken = default);
}
