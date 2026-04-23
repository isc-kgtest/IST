namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record ChangeUserPasswordCommand(
    [property: DataMember] Session Session,
    [property: DataMember] ChangeUserPasswordRequest Request
) : ICommand<ResponseDTO<string>>;
