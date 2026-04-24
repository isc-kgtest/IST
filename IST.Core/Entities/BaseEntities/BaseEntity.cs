using MemoryPack;

namespace IST.Core.Entities.BaseEntities;

/// <summary>
/// Базовая сущность системы. Все доменные сущности наследуются от неё
/// и получают автоматически: Id, аудит (CreatedAt/By, UpdatedAt/By), soft delete.
/// </summary>
[MemoryPackable]
public partial class BaseEntity : IAuditableEntity, ISoftDeletable
{
    /// <summary>Уникальный идентификатор записи.</summary>
    [MemoryPackOrder(0)]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Аудит
    [MemoryPackOrder(1)]
    public DateTime CreatedAt { get; set; }
    [MemoryPackOrder(2)]
    public Guid? CreatedBy { get; set; }
    [MemoryPackOrder(3)]
    public DateTime? UpdatedAt { get; set; }
    [MemoryPackOrder(4)]
    public Guid? UpdatedBy { get; set; }

    // Soft delete
    [MemoryPackOrder(5)]
    public bool IsDeleted { get; set; }
    [MemoryPackOrder(6)]
    public DateTime? DeletedAt { get; set; }
    [MemoryPackOrder(7)]
    public Guid? DeletedBy { get; set; }
}
