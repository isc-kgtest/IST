namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record CreateUserCommand(
    [property: DataMember] Session Session,
    [property: DataMember] CreateUserRequest Request
) : ICommand<ResponseDTO<UserResponseDTO>>;
