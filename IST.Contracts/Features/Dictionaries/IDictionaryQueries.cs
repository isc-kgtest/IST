using ActualLab.Fusion;
using IST.Shared.DTOs.Dictionaries;

namespace IST.Contracts.Features.Dictionaries;

public interface IDictionaryQueries : IComputeService
{
    [ComputeMethod]
    Task<List<DictionaryDto>> GetAllDictionariesAsync(CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<DictionaryDto?> GetDictionaryByIdAsync(Guid dictionaryId, CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<List<DictionaryRecordDto>> GetDictionaryRecordsAsync(Guid dictionaryId, CancellationToken cancellationToken = default);
}
