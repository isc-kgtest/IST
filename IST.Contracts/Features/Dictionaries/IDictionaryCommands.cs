using IST.Contracts.Features.Dictionaries.Commands;
using IST.Shared.DTOs.Common;
using IST.Shared.DTOs.Dictionaries;

namespace IST.Contracts.Features.Dictionaries;

/// <summary>
/// Commands (write-side) для модуля динамических Справочников.
/// </summary>
public interface IDictionaryCommands : ICommandService, IComputeService
{
    // ── Метаданные справочника ────────────────────────────────────────────────
    Task<ResponseDTO<DictionaryDto>> CreateDictionaryAsync(CreateDictionaryCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<DictionaryDto>> UpdateDictionaryAsync(UpdateDictionaryCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> DeleteDictionaryAsync(DeleteDictionaryCommand command, CancellationToken cancellationToken = default);

    // ── Поля справочника ──────────────────────────────────────────────────────
    Task<ResponseDTO<DictionaryFieldDto>> SaveFieldAsync(SaveDictionaryFieldCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> DeleteFieldAsync(DeleteDictionaryFieldCommand command, CancellationToken cancellationToken = default);

    // ── Записи справочника ────────────────────────────────────────────────────
    Task<ResponseDTO<DictionaryRecordDto>> SaveRecordAsync(SaveDictionaryRecordCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> DeleteRecordAsync(DeleteDictionaryRecordCommand command, CancellationToken cancellationToken = default);

    // ── Импорт / Экспорт ──────────────────────────────────────────────────────
    /// <summary>Импорт записей из XLSX/CSV файла в указанный справочник.</summary>
    Task<ResponseDTO<int>> ImportRecordsAsync(ImportDictionaryRecordsCommand command, CancellationToken cancellationToken = default);

    /// <summary>Экспорт записей справочника. Возвращает байты файла и MIME-тип.</summary>
    Task<ResponseDTO<ExportResult>> ExportRecordsAsync(ExportDictionaryRecordsCommand command, CancellationToken cancellationToken = default);
}
