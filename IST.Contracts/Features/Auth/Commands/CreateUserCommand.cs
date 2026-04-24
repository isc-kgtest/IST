
using MemoryPack;


namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public partial record CreateUserCommand(
    [property: DataMember][property: MemoryPackOrder(0)] Session Session,
    [property: DataMember][property: MemoryPackOrder(1)] CreateUserRequest Request
) : ICommand<ResponseDTO<UserResponseDTO>>;
