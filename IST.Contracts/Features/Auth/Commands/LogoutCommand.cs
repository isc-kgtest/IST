using ActualLab.Fusion;
using IST.Shared.DTOs.Common;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
[MemoryPackable]
public partial record LogoutCommand(
    [property: DataMember] Session Session
) : ICommand<ResponseDTO<string>>;
