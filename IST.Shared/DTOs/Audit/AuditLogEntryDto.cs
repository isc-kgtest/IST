using MemoryPack;

namespace IST.Shared.DTOs.Audit;

[MemoryPackable]
public partial class AuditLogEntryDto
{
    [MemoryPackOrder(0)] public Guid Id { get; set; }
    [MemoryPackOrder(1)] public DateTime Timestamp { get; set; }
    [MemoryPackOrder(2)] public string EventType { get; set; } = string.Empty;
    [MemoryPackOrder(3)] public bool Success { get; set; }
    [MemoryPackOrder(4)] public Guid? ActorUserId { get; set; }
    [MemoryPackOrder(5)] public string? ActorLogin { get; set; }
    [MemoryPackOrder(6)] public Guid? TargetUserId { get; set; }
    [MemoryPackOrder(7)] public string? TargetLogin { get; set; }
    [MemoryPackOrder(8)] public string? IpAddress { get; set; }
    [MemoryPackOrder(9)] public string? UserAgent { get; set; }
    [MemoryPackOrder(10)] public string? Message { get; set; }
    [MemoryPackOrder(11)] public string? DetailsJson { get; set; }
}
