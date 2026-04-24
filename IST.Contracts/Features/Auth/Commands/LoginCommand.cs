namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record LoginCommand(
    [property: DataMember] Session Session,
    [property: DataMember] string Login,
    [property: DataMember] string Password
) : ICommand<ResponseDTO<SessionUserDto>>;
