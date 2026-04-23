
namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record UpdateUserRoleCommand(
    [property: DataMember] Session Session,
    [property: DataMember] UpdateUserRoleRequest Request
) : ICommand<ResponseDTO<UserRoleResponseDTO>>;