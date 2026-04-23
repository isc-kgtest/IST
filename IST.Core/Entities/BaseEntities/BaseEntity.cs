namespace IST.Core.Entities.BaseEntities;

/// <summary>
/// Базовая сущность системы. Все доменные сущности наследуются от неё
/// и получают автоматически: Id, аудит (CreatedAt/By, UpdatedAt/By), soft delete.
/// </summary>
public class BaseEntity : IAuditableEntity, ISoftDeletable
{
    /// <summary>Уникальный идентификатор записи.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // Аудит
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
