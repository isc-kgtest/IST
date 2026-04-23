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
    Task<ResponseDTO<string>> DeleteUserAsync(DeleteUserCommand command,  CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> ChangeUserPasswordAsync(ChangeUserPasswordCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> ResetUserPasswordAsync(ResetUserPasswordCommand command, CancellationToken cancellationToken = default);

    //Roles
    Task<ResponseDTO<RoleResponseDTO>> CreateRoleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<RoleResponseDTO>> UpdateRoleAsync(UpdateRoleCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> DeleteRoleAsync(DeleteRoleCommand command, CancellationToken cancellationToken = default);

    // UserRoles
    Task<ResponseDTO<UserRoleResponseDTO>> CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<UserRoleResponseDTO>> UpdateUserRoleAsync(UpdateUserRoleCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<string>> DeleteUserRoleAsync(DeleteUserRoleCommand command, CancellationToken cancellationToken = default);
}
