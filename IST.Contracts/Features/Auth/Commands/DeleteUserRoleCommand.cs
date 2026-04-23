
namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record DeleteUserRoleCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Guid UserRoleId
) : ICommand<ResponseDTO<string>>;