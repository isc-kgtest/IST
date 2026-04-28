using ActualLab.Fusion;
using IST.Shared.DTOs.Common;

namespace IST.Contracts.Features.Nsi;

public interface INsiCommands : ICommandService, IComputeService
{
    Task<ResponseDTO<Guid>> UpsertNsiItemAsync(Commands.UpsertNsiItemCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<bool>> DeleteNsiItemAsync(Commands.DeleteNsiItemCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<bool>> ImportNsiItemsAsync(Commands.ImportNsiItemsCommand command, CancellationToken cancellationToken = default);
}
