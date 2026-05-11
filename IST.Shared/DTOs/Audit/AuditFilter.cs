using MemoryPack;

namespace IST.Shared.DTOs.Audit;

[MemoryPackable]
public partial class AuditFilter
{
    [MemoryPackOrder(0)] public string? EventType { get; set; }
    [MemoryPackOrder(1)] public string? ActorLoginContains { get; set; }
    [MemoryPackOrder(2)] public string? TargetLoginContains { get; set; }
    [MemoryPackOrder(3)] public DateTime? FromUtc { get; set; }
    [MemoryPackOrder(4)] public DateTime? ToUtc { get; set; }
    [MemoryPackOrder(5)] public bool? OnlyFailures { get; set; }
}
