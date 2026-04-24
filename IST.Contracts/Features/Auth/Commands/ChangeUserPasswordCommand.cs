using ActualLab.Fusion;
using IST.Shared.DTOs.Auth;
using IST.Shared.DTOs.Common;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
[MemoryPackable]
public partial record ChangeUserPasswordCommand(
    [property: DataMember] Session Session,
    [property: DataMember] ChangeUserPasswordRequest Request
) : ICommand<ResponseDTO<string>>;
