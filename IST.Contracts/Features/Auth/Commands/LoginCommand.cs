using ActualLab.Fusion;
using IST.Shared.DTOs.Auth;
using IST.Shared.DTOs.Common;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
[MemoryPackable]
public partial record LoginCommand(
    [property: DataMember] Session Session,
    [property: DataMember] string Login,
    [property: DataMember] string Password
) : ICommand<ResponseDTO<SessionUserDto>>;
