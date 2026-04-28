using IST.Core.Entities.BaseEntities;
using MemoryPack;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace IST.Core.Entities.Dictionaries;

[MemoryPackable]
public partial class DictionaryEntity : BaseEntity
{
    [MemoryPackOrder(8)]
    public string Name { get; set; } = string.Empty;

    [MemoryPackOrder(9)]
    public string? Description { get; set; }

    /// <summary>
    /// Структура полей (будет храниться как JSONB в БД).
    /// </summary>
    [Column(TypeName = "jsonb")]
    [MemoryPackOrder(10)]
    public string FieldStructure { get; set; } = "[]";
}

[MemoryPackable]
public partial class DictionaryRecordEntity : BaseEntity
{
    [MemoryPackOrder(8)]
    public Guid DictionaryId { get; set; }

    /// <summary>
    /// Динамические данные записи (будет храниться как JSONB в БД).
    /// </summary>
    [Column(TypeName = "jsonb")]
    [MemoryPackOrder(9)]
    public string Data { get; set; } = "{}";
}
