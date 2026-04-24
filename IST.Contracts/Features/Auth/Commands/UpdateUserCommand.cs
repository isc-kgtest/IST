using ActualLab.Fusion;
using IST.Shared.DTOs.Auth;
using IST.Shared.DTOs.Common;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
[MemoryPackable]
public partial record UpdateUserCommand(
    [property: DataMember] Session Session,
    [property: DataMember] UpdateUserRequest Request
) : ICommand<ResponseDTO<UserResponseDTO>>;
