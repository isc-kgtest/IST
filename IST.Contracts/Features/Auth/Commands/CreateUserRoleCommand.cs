
namespace IST.Contracts.Features.Auth.Commands;


[DataContract]
public record CreateUserRoleCommand(
    [property: DataMember] Session Session,
    [property: DataMember] CreateUserRoleRequest Request
) : ICommand<ResponseDTO<UserRoleResponseDTO>>;