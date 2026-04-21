namespace IST.Core.Entities.BaseEntities;

/// <summary>
/// Описывает сущность, поддерживающую обратимое удаление.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Удалена ли сущность.
    /// </summary>
    bool IsDeleted { get; set; }
}