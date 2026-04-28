using ActualLab.Fusion;
using IST.Shared.DTOs.Nsi;

namespace IST.Contracts.Features.Nsi.Commands;

public record UpsertNsiItemCommand(Session Session, string TypeName, UpsertNsiItemRequest Request) : ISessionCommand<IST.Shared.DTOs.Common.ResponseDTO<Guid>>;

public record DeleteNsiItemCommand(Session Session, string TypeName, Guid Id) : ISessionCommand<IST.Shared.DTOs.Common.ResponseDTO<bool>>;

// FileData - base64 или другой формат файла (зависит от реализации)
public record ImportNsiItemsCommand(Session Session, string TypeName, string FileName, byte[] FileData) : ISessionCommand<IST.Shared.DTOs.Common.ResponseDTO<bool>>;
