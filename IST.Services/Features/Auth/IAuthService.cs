using ActualLab.Fusion;
using IST.Core.Entities.Auth;

namespace IST.Services.Features.Auth;

public interface IAuthService
{
    //Queries
    [ComputeMethod]
    Task<UserEntity?> GetUserByLoginAsync(string login, string password, CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<UserEntity?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<List<UserEntity>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<RoleEntity?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<List<RoleEntity>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    //Commands
    //Users
    Task<string> CreateUserAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task<UserEntity> UpdateUserAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task<string> ChangeUserPasswordAsync(Guid userId, string hashedNewPassword, DateTime passwordExpiryDate, CancellationToken cancellationToken = default);
    Task<string> ResetUserPasswordAsync(Guid userId, string hashedNewPassword, DateTime passwordExpiryDate, CancellationToken cancellationToken = default);

    //Roles
    Task<RoleEntity> CreateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default);
    Task<RoleEntity> UpdateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(RoleEntity role, CancellationToken cancellationToken = default);

    //UserRoles
    Task<UserRolesEntity> CreateUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default)
}
