using ActualLab.Fusion;
using IST.Shared.DTOs.Common;
using IST.Shared.DTOs.Organization;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Organization.Commands;

[DataContract]
[MemoryPackable]
public partial record SaveNodeCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Guid? Id,
    [property: DataMember] Guid NodeTypeId,
    [property: DataMember] Guid? ParentNodeId,
    [property: DataMember] string Name,
    [property: DataMember] string? Code,
    [property: DataMember] string? Description,
    [property: DataMember] int SortOrder,
    [property: DataMember] bool IsActive
) : ICommand<ResponseDTO<OrganizationNodeDto>>;

[DataContract]
[MemoryPackable]
public partial record DeleteNodeCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Guid Id
) : ICommand<ResponseDTO<string>>;
