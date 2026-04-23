namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record DeleteUserCommand(
    [property: DataMember] Session Session,
    [Required(ErrorMessage = "Id обязательно.")]
    [property: DataMember] Guid UserId
) : ICommand<ResponseDTO<string>>;
