using MemoryPack;
using Mapster;
using IST.Core.Entities.Dictionaries;
using IST.Core.Entities.Dictionaries.Enums;

namespace IST.Shared.DTOs.Dictionaries;

// ──────────────────────────────────────────────────────────
//  DTO метаданных справочника
// ──────────────────────────────────────────────────────────

[MemoryPackable]
public partial class DictionaryDto : IRegister
{
    public void Register(TypeAdapterConfig config)
        => config.NewConfig<DictionaryEntity, DictionaryDto>()
                 .Map(d => d.FieldCount, src => src.Fields.Count(f => !f.IsDeleted));

    [MemoryPackOrder(0)] public Guid Id              { get; set; }
    [MemoryPackOrder(1)] public string Name          { get; set; } = string.Empty;
    [MemoryPackOrder(2)] public string? Description  { get; set; }
    [MemoryPackOrder(3)] public string Slug          { get; set; } = string.Empty;
    [MemoryPackOrder(4)] public int FieldCount       { get; set; }
    [MemoryPackOrder(5)] public bool IsDeleted       { get; set; }
    [MemoryPackOrder(6)] public DateTime CreatedAt   { get; set; }
    [MemoryPackOrder(7)] public DateTime? UpdatedAt  { get; set; }
    [MemoryPackOrder(8), MemoryPackAllowSerialize] public DictionaryType Type  { get; set; } = DictionaryType.General;
}

// ──────────────────────────────────────────────────────────
//  DTO поля справочника
// ──────────────────────────────────────────────────────────

[MemoryPackable]
public partial class DictionaryFieldDto : IRegister
{
    public void Register(TypeAdapterConfig config)
        => config.NewConfig<DictionaryFieldEntity, DictionaryFieldDto>();

    [MemoryPackOrder(0)] public Guid Id                      { get; set; }
    [MemoryPackOrder(1)] public Guid DictionaryId            { get; set; }
    [MemoryPackOrder(2)] public string FieldKey              { get; set; } = string.Empty;
    [MemoryPackOrder(3)] public string DisplayName           { get; set; } = string.Empty;
    [MemoryPackOrder(4)] public DictionaryFieldType FieldType { get; set; }
    [MemoryPackOrder(5)] public bool IsRequired              { get; set; }
    [MemoryPackOrder(6)] public int SortOrder                { get; set; }
    [MemoryPackOrder(7)] public bool IsDeleted               { get; set; }
}

// ──────────────────────────────────────────────────────────
//  DTO записи справочника
// ──────────────────────────────────────────────────────────

[MemoryPackable]
public partial class DictionaryRecordDto : IRegister
{
    public void Register(TypeAdapterConfig config)
        => config.NewConfig<DictionaryRecordEntity, DictionaryRecordDto>();

    [MemoryPackOrder(0)] public Guid Id              { get; set; }
    [MemoryPackOrder(1)] public Guid DictionaryId    { get; set; }
    /// <summary>JSON-строка с данными записи.</summary>
    [MemoryPackOrder(2)] public string Data          { get; set; } = "{}";
    [MemoryPackOrder(3)] public bool IsDeleted       { get; set; }
    [MemoryPackOrder(4)] public DateTime CreatedAt   { get; set; }
    [MemoryPackOrder(5)] public DateTime? UpdatedAt  { get; set; }
}

// ──────────────────────────────────────────────────────────
//  Детальный DTO — справочник со структурой полей и записями
// ──────────────────────────────────────────────────────────

[MemoryPackable]
public partial class DictionaryDetailDto
{
    [MemoryPackOrder(0)] public DictionaryDto Metadata     { get; set; } = new();
    [MemoryPackOrder(1)] public List<DictionaryFieldDto> Fields  { get; set; } = new();
    [MemoryPackOrder(2)] public List<DictionaryRecordDto> Records { get; set; } = new();
}

// ──────────────────────────────────────────────────────────
//  Request-модели
// ──────────────────────────────────────────────────────────

[MemoryPackable]
public partial class CreateDictionaryRequest
{
    public string Name          { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string Slug          { get; set; } = string.Empty;
    [MemoryPackAllowSerialize] public DictionaryType Type  { get; set; } = DictionaryType.General;
    public List<DictionaryFieldRequest> Fields { get; set; } = new();
}

[MemoryPackable]
public partial class UpdateDictionaryRequest
{
    public Guid Id              { get; set; }
    public string Name          { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string Slug          { get; set; } = string.Empty;
    [MemoryPackAllowSerialize] public DictionaryType Type  { get; set; } = DictionaryType.General;
}

[MemoryPackable]
public partial class DictionaryFieldRequest
{
    public Guid? Id                          { get; set; }
    public string FieldKey                   { get; set; } = string.Empty;
    public string DisplayName                { get; set; } = string.Empty;
    public DictionaryFieldType FieldType     { get; set; } = DictionaryFieldType.Text;
    public bool IsRequired                   { get; set; }
    public int SortOrder                     { get; set; }
}

[MemoryPackable]
public partial class SaveDictionaryRecordRequest
{
    public Guid? Id           { get; set; }
    public Guid DictionaryId  { get; set; }
    /// <summary>JSON-строка с данными записи.</summary>
    public string Data        { get; set; } = "{}";
}
