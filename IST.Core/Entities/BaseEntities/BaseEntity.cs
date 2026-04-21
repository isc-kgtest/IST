namespace IST.Core.Entities.BaseEntities;

public class BaseEntity : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get ; set; }
    public bool IsDeleted { get; set; }
}
