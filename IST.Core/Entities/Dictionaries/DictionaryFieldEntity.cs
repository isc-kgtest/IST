using MemoryPack;
using IST.Core.Entities.BaseEntities;
using IST.Core.Entities.Dictionaries.Enums;

namespace IST.Core.Entities.Dictionaries;

/// <summary>
/// Описание одного поля (колонки) динамического справочника.
/// Связь 1-ко-многим с DictionaryEntity.
/// </summary>
[MemoryPackable]
public partial class DictionaryFieldEntity : BaseEntity
{
    [MemoryPackOrder(8)]
    public Guid DictionaryId { get; set; }

    /// <summary>Системное имя поля (camelCase, латиница).</summary>
    [MemoryPackOrder(9)]
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>Отображаемое название поля.</summary>
    [MemoryPackOrder(10)]
    public string DisplayName { get; set; } = string.Empty;

    [MemoryPackOrder(11)]
    public DictionaryFieldType FieldType { get; set; } = DictionaryFieldType.Text;

    [MemoryPackOrder(12)]
    public bool IsRequired { get; set; }

    [MemoryPackOrder(13)]
    public int SortOrder { get; set; }

    // Навигация
    public DictionaryEntity? Dictionary { get; set; }
}
