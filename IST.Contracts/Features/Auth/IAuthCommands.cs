using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Auth;
using IST.Shared.DTOs.Auth;
using IST.Shared.DTOs.Common;

namespace IST.Contracts.Features.Auth;

public interface IAuthCommands
{
    //Users
    Task<ResponseDTO<UserResponseDTO>> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);
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
    Task<bool> DeleteUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default);
}
