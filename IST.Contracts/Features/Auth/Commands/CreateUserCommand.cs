using ActualLab.Fusion;
using IST.Shared.DTOs.Auth;
using IST.Shared.DTOs.Common;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
[MemoryPackable]
public partial record CreateUserCommand(
    [property: DataMember][property: MemoryPackOrder(0)] Session Session,
    [property: DataMember][property: MemoryPackOrder(1)] CreateUserRequest Request
) : ICommand<ResponseDTO<UserResponseDTO>>;
