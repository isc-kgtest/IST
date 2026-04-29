using MemoryPack;
using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Dictionaries;

/// <summary>
/// Одна запись (строка данных) в динамическом справочнике.
/// Данные хранятся в виде JSON-документа, ключи которого соответствуют
/// FieldKey полей DictionaryFieldEntity.
/// Пример: { "name": "Казахстан", "code": "KZ", "isActive": true }
/// </summary>
[MemoryPackable]
public partial class DictionaryRecordEntity : BaseEntity
{
    [MemoryPackOrder(8)]
    public Guid DictionaryId { get; set; }

    /// <summary>
    /// JSON-документ с фактическими данными записи.
    /// Хранится как JSONB (PostgreSQL) / nvarchar(max) (SQL Server).
    /// </summary>
    [MemoryPackOrder(9)]
    public string Data { get; set; } = "{}";

    // Навигация
    public DictionaryEntity? Dictionary { get; set; }
}
