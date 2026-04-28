using ActualLab.Fusion;
using IST.Shared.DTOs.Common;

namespace IST.Contracts.Features.Dictionaries;

public interface IDictionaryCommands : ICommandService, IComputeService
{
    Task<ResponseDTO<Guid>> UpsertDictionaryAsync(Commands.UpsertDictionaryCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<bool>> DeleteDictionaryAsync(Commands.DeleteDictionaryCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<Guid>> UpsertDictionaryRecordAsync(Commands.UpsertDictionaryRecordCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<bool>> DeleteDictionaryRecordAsync(Commands.DeleteDictionaryRecordCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<bool>> ImportDictionaryRecordsAsync(Commands.ImportDictionaryRecordsCommand command, CancellationToken cancellationToken = default);
}
