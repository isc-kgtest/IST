using MemoryPack;

namespace IST.Shared.DTOs.Auth;

[MemoryPackable]
public partial class PermissionDto
{
    [MemoryPackOrder(0)] public Guid Id { get; set; }
    [MemoryPackOrder(1)] public string Code { get; set; } = string.Empty;
    [MemoryPackOrder(2)] public string Description { get; set; } = string.Empty;
    [MemoryPackOrder(3)] public string? Category { get; set; }
}
