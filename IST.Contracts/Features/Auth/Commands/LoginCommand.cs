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
) : ICommand<ResponseDTO<SessionUserDto>>
{
    [DataMember] public string? IpAddress { get; init; }
    [DataMember] public string? UserAgent { get; init; }
    /// <summary>
    /// Если задано — пользователь должен иметь этот permission, иначе вход
    /// в данный портал отклоняется. Используется для гейтов "admin.access" /
    /// "client.access" — один и тот же пользователь может зайти не во все.
    /// </summary>
    [DataMember] public string? RequiredPermission { get; init; }
}
