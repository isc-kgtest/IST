using ActualLab.Fusion;
using IST.Shared.DTOs.Dictionaries;

namespace IST.Contracts.Features.Dictionaries;

/// <summary>
/// Queries (read-side) для модуля динамических Справочников.
/// </summary>
public interface IDictionaryQueries : IComputeService
{
    // ── Список справочников ───────────────────────────────────────────────────

    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<DictionaryDto>> GetAllDictionariesAsync(CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 60)]
    Task<DictionaryDto?> GetDictionaryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 60)]
    Task<DictionaryDto?> GetDictionaryBySlugAsync(string slug, CancellationToken cancellationToken = default);

    // ── Поля справочника ──────────────────────────────────────────────────────

    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<DictionaryFieldDto>> GetFieldsByDictionaryIdAsync(Guid dictionaryId, CancellationToken cancellationToken = default);

    // ── Записи справочника ────────────────────────────────────────────────────

    [ComputeMethod(MinCacheDuration = 30)]
    Task<List<DictionaryRecordDto>> GetRecordsByDictionaryIdAsync(Guid dictionaryId, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 30)]
    Task<DictionaryRecordDto?> GetRecordByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // ── Детальный DTO (метаданные + поля + записи) ────────────────────────────

    [ComputeMethod(MinCacheDuration = 30)]
    Task<DictionaryDetailDto?> GetDictionaryDetailAsync(Guid id, CancellationToken cancellationToken = default);
}
