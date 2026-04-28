using ActualLab.Fusion;
using IST.Contracts.Features.Dictionaries;
using IST.Contracts.Features.Dictionaries.Commands;
using IST.Shared.DTOs.Common;

namespace IST.Services.Features.Dictionaries;

public class DictionaryCommands : IDictionaryCommands
{
    public virtual Task<ResponseDTO<Guid>> UpsertDictionaryAsync(UpsertDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseDTO<Guid> { Status = true, Data = Guid.NewGuid() });
    }

    public virtual Task<ResponseDTO<bool>> DeleteDictionaryAsync(DeleteDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseDTO<bool> { Status = true, Data = true });
    }

    public virtual Task<ResponseDTO<Guid>> UpsertDictionaryRecordAsync(UpsertDictionaryRecordCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseDTO<Guid> { Status = true, Data = Guid.NewGuid() });
    }

    public virtual Task<ResponseDTO<bool>> DeleteDictionaryRecordAsync(DeleteDictionaryRecordCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseDTO<bool> { Status = true, Data = true });
    }

    public virtual Task<ResponseDTO<bool>> ImportDictionaryRecordsAsync(ImportDictionaryRecordsCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseDTO<bool> { Status = true, Data = true });
    }
}
