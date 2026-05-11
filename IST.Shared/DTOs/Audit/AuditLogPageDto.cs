using MemoryPack;

namespace IST.Shared.DTOs.Audit;

[MemoryPackable]
public partial class AuditLogPageDto
{
    [MemoryPackOrder(0)] public List<AuditLogEntryDto> Items { get; set; } = new();
    [MemoryPackOrder(1)] public int Total { get; set; }
    [MemoryPackOrder(2)] public bool AccessDenied { get; set; }
}
