namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record UpdateUserCommand(
    [property: DataMember] Session Session,
    [property: DataMember] UpdateUserRequest Request
) : ICommand<ResponseDTO<UserResponseDTO>>;