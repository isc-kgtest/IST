namespace IST.Core.Entities.BaseEntities;

/// <summary>
/// Интерфейс для сущностей с мягким удалением.
/// Физическое удаление перехватывается и заменяется на установку IsDeleted = true.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Признак удаления записи.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Дата и время удаления записи (UTC).</summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>Идентификатор пользователя, удалившего запись.</summary>
    Guid? DeletedBy { get; set; }
}