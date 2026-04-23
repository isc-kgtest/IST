namespace IST.Core.Entities.BaseEntities;

/// <summary>
/// Интерфейс для сущностей, требующих аудита создания и изменения.
/// Поля заполняются автоматически в AppDbContext.SaveChangesAsync.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Дата и время создания записи (UTC).</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>Идентификатор пользователя, создавшего запись.</summary>
    Guid? CreatedBy { get; set; }

    /// <summary>Дата и время последнего изменения записи (UTC).</summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>Идентификатор пользователя, последним изменившего запись.</summary>
    Guid? UpdatedBy { get; set; }
}
