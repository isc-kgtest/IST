namespace IST.Contracts.Features.Auth.Commands;


[DataContract]
public record UpdateRoleCommand(
    [property: DataMember] Session Session,
    [property: DataMember] UpdateRoleRequest Request
) : ICommand<ResponseDTO<RoleResponseDTO>>;
