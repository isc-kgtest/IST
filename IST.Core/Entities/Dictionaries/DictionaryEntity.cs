using MemoryPack;
using IST.Core.Entities.BaseEntities;
using IST.Core.Entities.Dictionaries.Enums;

namespace IST.Core.Entities.Dictionaries;

/// <summary>
/// Метаданные динамического справочника.
/// Каждый экземпляр описывает один конструируемый справочник:
/// его имя, назначение и структуру полей.
/// </summary>
[MemoryPackable]
public partial class DictionaryEntity : BaseEntity
{
    [MemoryPackOrder(8)]
    public string Name { get; set; } = string.Empty;

    [MemoryPackOrder(9)]
    public string? Description { get; set; }

    /// <summary>
    /// Код-идентификатор справочника (slug), используется как системный ключ.
    /// Пример: "countries", "product_types".
    /// </summary>
    [MemoryPackOrder(10)]
    public string Slug { get; set; } = string.Empty;

    [MemoryPackOrder(11)]
    public DictionaryType Type { get; set; } = DictionaryType.General;

    // Навигация
    public ICollection<DictionaryFieldEntity> Fields { get; set; } = new List<DictionaryFieldEntity>();
    public ICollection<DictionaryRecordEntity> Records { get; set; } = new List<DictionaryRecordEntity>();
}
