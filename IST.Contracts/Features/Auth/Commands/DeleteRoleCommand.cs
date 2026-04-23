
namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record DeleteRoleCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Guid RoleId
) : ICommand<ResponseDTO<string>>;