using ActualLab.Fusion;
using IST.Shared.DTOs.Common;
using IST.Shared.DTOs.Dictionaries;
using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Dictionaries.Commands;

// ── Метаданные справочника ────────────────────────────────────────────────────

[DataContract, MemoryPackable]
public partial record CreateDictionaryCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] CreateDictionaryRequest Request
) : ICommand<ResponseDTO<DictionaryDto>>;

[DataContract, MemoryPackable]
public partial record UpdateDictionaryCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] UpdateDictionaryRequest Request
) : ICommand<ResponseDTO<DictionaryDto>>;

[DataContract, MemoryPackable]
public partial record DeleteDictionaryCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] Guid DictionaryId
) : ICommand<ResponseDTO<string>>;

// ── Поля справочника ──────────────────────────────────────────────────────────

[DataContract, MemoryPackable]
public partial record SaveDictionaryFieldCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] Guid DictionaryId,
    [property: DataMember, MemoryPackOrder(2)] DictionaryFieldRequest Request
) : ICommand<ResponseDTO<DictionaryFieldDto>>;

[DataContract, MemoryPackable]
public partial record DeleteDictionaryFieldCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] Guid FieldId
) : ICommand<ResponseDTO<string>>;

// ── Записи справочника ────────────────────────────────────────────────────────

[DataContract, MemoryPackable]
public partial record SaveDictionaryRecordCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] SaveDictionaryRecordRequest Request
) : ICommand<ResponseDTO<DictionaryRecordDto>>;

[DataContract, MemoryPackable]
public partial record DeleteDictionaryRecordCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] Guid RecordId
) : ICommand<ResponseDTO<string>>;

// ── Импорт / Экспорт ──────────────────────────────────────────────────────────

[DataContract, MemoryPackable]
public partial record ImportDictionaryRecordsCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] Guid DictionaryId,
    [property: DataMember, MemoryPackOrder(2)] byte[] FileContent,
    [property: DataMember, MemoryPackOrder(3)] string FileName
) : ICommand<ResponseDTO<int>>;

[DataContract, MemoryPackable]
public partial record ExportDictionaryRecordsCommand(
    [property: DataMember, MemoryPackOrder(0)] Session Session,
    [property: DataMember, MemoryPackOrder(1)] Guid DictionaryId,
    /// <summary>"xlsx" | "csv"</summary>
    [property: DataMember, MemoryPackOrder(2)] string Format
) : ICommand<ResponseDTO<ExportResult>>;
