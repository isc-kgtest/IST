using ActualLab.Fusion;
using IST.Shared.DTOs.Common;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
[MemoryPackable]
public partial record DeleteUserRoleCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Guid UserId,
    [property: DataMember] Guid UserRoleId
) : ICommand<ResponseDTO<string>>;
