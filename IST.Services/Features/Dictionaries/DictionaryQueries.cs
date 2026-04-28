using ActualLab.Fusion;
using IST.Contracts.Features.Dictionaries;
using IST.Shared.DTOs.Dictionaries;

namespace IST.Services.Features.Dictionaries;

public class DictionaryQueries : IDictionaryQueries
{
    public virtual Task<List<DictionaryDto>> GetAllDictionariesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<DictionaryDto>());
    }

    public virtual Task<DictionaryDto?> GetDictionaryByIdAsync(Guid dictionaryId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DictionaryDto?>(null);
    }

    public virtual Task<List<DictionaryRecordDto>> GetDictionaryRecordsAsync(Guid dictionaryId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<DictionaryRecordDto>());
    }
}
