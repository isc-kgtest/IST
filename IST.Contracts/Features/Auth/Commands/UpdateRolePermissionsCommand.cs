using ActualLab.Fusion;
using IST.Shared.DTOs.Common;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

/// <summary>
/// Заменяет полный набор привилегий у роли. Передаётся плоский массив PermissionId.
/// На сервере вычисляется diff (add/remove), для admin-роли всегда выставляются все.
/// </summary>
[DataContract]
[MemoryPackable]
public partial record UpdateRolePermissionsCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Guid RoleId,
    [property: DataMember] Guid[] PermissionIds
) : ICommand<ResponseDTO<string>>;
