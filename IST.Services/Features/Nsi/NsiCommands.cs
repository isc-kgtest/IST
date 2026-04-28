using ActualLab.Fusion;
using IST.Contracts.Features.Nsi;
using IST.Contracts.Features.Nsi.Commands;
using IST.Shared.DTOs.Common;

namespace IST.Services.Features.Nsi;

public class NsiCommands : INsiCommands
{
    public virtual Task<ResponseDTO<Guid>> UpsertNsiItemAsync(UpsertNsiItemCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseDTO<Guid> { Status = true, Data = Guid.NewGuid() });
    }

    public virtual Task<ResponseDTO<bool>> DeleteNsiItemAsync(DeleteNsiItemCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseDTO<bool> { Status = true, Data = true });
    }

    public virtual Task<ResponseDTO<bool>> ImportNsiItemsAsync(ImportNsiItemsCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseDTO<bool> { Status = true, Data = true });
    }
}
