namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record ResetUserPasswordCommand(
    [property: DataMember] Session Session,
    [property: DataMember] ResetUserPasswordRequest Request
) : ICommand<ResponseDTO<string>>;
