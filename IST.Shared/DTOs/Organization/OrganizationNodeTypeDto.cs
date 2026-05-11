using MemoryPack;

namespace IST.Shared.DTOs.Organization;

[MemoryPackable]
public partial class OrganizationNodeTypeDto
{
    [MemoryPackOrder(0)] public Guid Id { get; set; }
    [MemoryPackOrder(1)] public string Code { get; set; } = string.Empty;
    [MemoryPackOrder(2)] public string Name { get; set; } = string.Empty;
    [MemoryPackOrder(3)] public string? Description { get; set; }
    [MemoryPackOrder(4)] public int Level { get; set; }
    [MemoryPackOrder(5)] public int SortOrder { get; set; }
    [MemoryPackOrder(6)] public string? Icon { get; set; }
}
