using ActualLab.Fusion;
using IST.Shared.DTOs.Auth;
using IST.Shared.DTOs.Common;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
public record CreateUserCommand(
    [property: DataMember] Session Session,
    [property: DataMember] CreateUserRequest Request
) : ICommand<ResponseDTO<UserResponseDTO>>;
