
namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record CreateRoleCommand(
    [property: DataMember] Session Session,
    [property: DataMember] CreateRoleRequest Request
) : ICommand<ResponseDTO<RoleResponseDTO>>;