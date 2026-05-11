using MemoryPack;

namespace IST.Shared.DTOs.Organization;

/// <summary>
/// Плоское представление узла с готовой строкой пути для удобной отрисовки
/// в выпадашке или поле выбора. FullPath: "Кыргызстан → Чуй → Бишкек".
/// </summary>
[MemoryPackable]
public partial class OrganizationNodeFlatDto
{
    [MemoryPackOrder(0)] public Guid Id { get; set; }
    [MemoryPackOrder(1)] public string Name { get; set; } = string.Empty;
    [MemoryPackOrder(2)] public string FullPath { get; set; } = string.Empty;
    [MemoryPackOrder(3)] public string NodeTypeName { get; set; } = string.Empty;
    [MemoryPackOrder(4)] public int Depth { get; set; }
    [MemoryPackOrder(5)] public bool IsActive { get; set; }
}
