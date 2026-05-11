using MemoryPack;

namespace IST.Shared.DTOs.Auth;

/// <summary>
/// Диагностический срез: что текущая Fusion-сессия знает о пользователе.
/// Используется, чтобы помочь отладить отказ доступа (виден ли пользователь
/// серверу и какие у него permission'ы).
/// </summary>
[MemoryPackable]
public partial class WhoAmIDto
{
    [MemoryPackOrder(0)] public bool IsAuthenticated { get; set; }
    [MemoryPackOrder(1)] public Guid? UserId { get; set; }
    [MemoryPackOrder(2)] public string? Login { get; set; }
    [MemoryPackOrder(3)] public string? FullName { get; set; }
    [MemoryPackOrder(4)] public List<string> Roles { get; set; } = new();
    [MemoryPackOrder(5)] public List<string> Permissions { get; set; } = new();
}
