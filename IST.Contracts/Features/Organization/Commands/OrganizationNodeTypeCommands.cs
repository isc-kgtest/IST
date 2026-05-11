using ActualLab.Fusion;
using IST.Shared.DTOs.Common;
using IST.Shared.DTOs.Organization;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Organization.Commands;

[DataContract]
[MemoryPackable]
public partial record SaveNodeTypeCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Guid? Id,
    [property: DataMember] string Code,
    [property: DataMember] string Name,
    [property: DataMember] string? Description,
    [property: DataMember] int Level,
    [property: DataMember] int SortOrder,
    [property: DataMember] string? Icon
) : ICommand<ResponseDTO<OrganizationNodeTypeDto>>;

[DataContract]
[MemoryPackable]
public partial record DeleteNodeTypeCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Guid Id
) : ICommand<ResponseDTO<string>>;
