using MemoryPack;

namespace IST.Shared.DTOs.Organization;

[MemoryPackable]
public partial class OrganizationNodeDto
{
    [MemoryPackOrder(0)] public Guid Id { get; set; }
    [MemoryPackOrder(1)] public Guid NodeTypeId { get; set; }
    [MemoryPackOrder(2)] public string NodeTypeName { get; set; } = string.Empty;
    [MemoryPackOrder(3)] public string? NodeTypeIcon { get; set; }
    [MemoryPackOrder(4)] public Guid? ParentNodeId { get; set; }
    [MemoryPackOrder(5)] public string Name { get; set; } = string.Empty;
    [MemoryPackOrder(6)] public string? Code { get; set; }
    [MemoryPackOrder(7)] public string Path { get; set; } = "/";
    [MemoryPackOrder(8)] public int Depth { get; set; }
    [MemoryPackOrder(9)] public int SortOrder { get; set; }
    [MemoryPackOrder(10)] public bool IsActive { get; set; }
    [MemoryPackOrder(11)] public string? Description { get; set; }
    /// <summary>Кол-во прямых детей (для UI: показывать ли expand-стрелочку).</summary>
    [MemoryPackOrder(12)] public int ChildrenCount { get; set; }
}
