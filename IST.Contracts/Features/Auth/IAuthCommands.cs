using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Auth;
using IST.Shared.DTOs.Auth;
using IST.Shared.DTOs.Common;

namespace IST.Contracts.Features.Auth;

public interface IAuthCommands : ICommandService
{
    //Users
    Task<ResponseDTO<UserResponseDTO>> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<UserResponseDTO>> UpdateUserAsync(UpdateUserCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> DeleteUserAsync(DeleteUserCommand command, Session session, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> ChangeUserPasswordAsync(ChangeUserPasswordCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> ResetUserPasswordAsync(ResetUserPasswordCommand command, CancellationToken cancellationToken = default);

    //Roles
    Task<RoleEntity> CreateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default);
    Task<RoleEntity> UpdateRoleAsync(RoleEntity role, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(RoleEntity role, CancellationToken cancellationToken = default);

    //UserRoles
    Task<UserRolesEntity> CreateUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserRoleAsync(UserRolesEntity userRole, CancellationToken cancellationToken = default);
}
