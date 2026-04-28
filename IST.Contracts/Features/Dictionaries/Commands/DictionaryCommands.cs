using ActualLab.Fusion;
using IST.Shared.DTOs.Dictionaries;

namespace IST.Contracts.Features.Dictionaries.Commands;

public record UpsertDictionaryCommand(Session Session, UpsertDictionaryRequest Request) : ISessionCommand<IST.Shared.DTOs.Common.ResponseDTO<Guid>>;
public record DeleteDictionaryCommand(Session Session, Guid Id) : ISessionCommand<IST.Shared.DTOs.Common.ResponseDTO<bool>>;

public record UpsertDictionaryRecordCommand(Session Session, UpsertDictionaryRecordRequest Request) : ISessionCommand<IST.Shared.DTOs.Common.ResponseDTO<Guid>>;
public record DeleteDictionaryRecordCommand(Session Session, Guid DictionaryId, Guid RecordId) : ISessionCommand<IST.Shared.DTOs.Common.ResponseDTO<bool>>;

public record ImportDictionaryRecordsCommand(Session Session, Guid DictionaryId, string FileName, byte[] FileData) : ISessionCommand<IST.Shared.DTOs.Common.ResponseDTO<bool>>;
