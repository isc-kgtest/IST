using ActualLab.CommandR;
using ActualLab.CommandR.Configuration;
using ActualLab.Fusion;
using ActualLab.Fusion.EntityFramework;
using IST.Contracts.Features.Dictionaries;
using IST.Contracts.Features.Dictionaries.Commands;
using IST.Core.Entities.Dictionaries;
using IST.Infrastructure.Data;
using IST.Shared.DTOs.Common;
using IST.Shared.DTOs.Dictionaries;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace IST.Services.Features.Dictionaries;

public class DictionaryCommands : IDictionaryCommands
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IDictionaryQueries _queries;
    private readonly IMapper _mapper;

    public DictionaryCommands(DbHub<AppDbContext> dbHub, IDictionaryQueries queries, IMapper mapper)
    {
        _dbHub = dbHub;
        _queries = queries;
        _mapper = mapper;
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<DictionaryDto>> CreateDictionaryAsync(CreateDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllDictionariesAsync(default);
            _ = _queries.GetDictionaryBySlugAsync(command.Request.Slug, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        var exists = await dbContext.Dictionaries.AnyAsync(d => d.Slug == command.Request.Slug, cancellationToken);
        if (exists) return new ResponseDTO<DictionaryDto> { Status = false, StatusMessage = "Справочник с таким кодом уже существует" };

        var entity = new DictionaryEntity
        {
            Name = command.Request.Name,
            Description = command.Request.Description,
            Slug = command.Request.Slug
        };

        dbContext.Dictionaries.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<DictionaryDto> { Status = true, Data = _mapper.Map<DictionaryDto>(entity) };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<DictionaryDto>> UpdateDictionaryAsync(UpdateDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllDictionariesAsync(default);
            _ = _queries.GetDictionaryByIdAsync(command.Request.Id, default);
            _ = _queries.GetDictionaryDetailAsync(command.Request.Id, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        var entity = await dbContext.Dictionaries.FirstOrDefaultAsync(d => d.Id == command.Request.Id, cancellationToken);
        if (entity == null) return new ResponseDTO<DictionaryDto> { Status = false, StatusMessage = "Справочник не найден" };

        entity.Name = command.Request.Name;
        entity.Description = command.Request.Description;
        // Slug обычно не меняют, но если нужно:
        // entity.Slug = command.Slug;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<DictionaryDto> { Status = true, Data = _mapper.Map<DictionaryDto>(entity) };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteDictionaryAsync(DeleteDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllDictionariesAsync(default);
            _ = _queries.GetDictionaryByIdAsync(command.DictionaryId, default);
            _ = _queries.GetDictionaryDetailAsync(command.DictionaryId, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        var entity = await dbContext.Dictionaries.FirstOrDefaultAsync(d => d.Id == command.DictionaryId, cancellationToken);
        if (entity == null) return new ResponseDTO<string> { Status = false, StatusMessage = "Справочник не найден" };

        dbContext.Dictionaries.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<string> { Status = true, Data = "Удалено" };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<DictionaryFieldDto>> SaveFieldAsync(SaveDictionaryFieldCommand command, CancellationToken cancellationToken = default)
    {
        var req = command.Request;
        if (Invalidation.IsActive)
        {
            _ = _queries.GetFieldsByDictionaryIdAsync(command.DictionaryId, default);
            _ = _queries.GetDictionaryDetailAsync(command.DictionaryId, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        DictionaryFieldEntity entity;
        if (req.Id.HasValue && req.Id.Value != Guid.Empty)
        {
            entity = await dbContext.DictionaryFields.FirstOrDefaultAsync(f => f.Id == req.Id.Value, cancellationToken);
            if (entity == null) return new ResponseDTO<DictionaryFieldDto> { Status = false, StatusMessage = "Поле не найдено" };
        }
        else
        {
            entity = new DictionaryFieldEntity { DictionaryId = command.DictionaryId };
            dbContext.DictionaryFields.Add(entity);
        }

        entity.FieldKey = req.FieldKey;
        entity.DisplayName = req.DisplayName;
        entity.FieldType = req.FieldType;
        entity.IsRequired = req.IsRequired;
        entity.SortOrder = req.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<DictionaryFieldDto> { Status = true, Data = _mapper.Map<DictionaryFieldDto>(entity) };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteFieldAsync(DeleteDictionaryFieldCommand command, CancellationToken cancellationToken = default)
    {
        var dbContextForFind = await _dbHub.CreateDbContext(cancellationToken);
        var field = await dbContextForFind.DictionaryFields.AsNoTracking().FirstOrDefaultAsync(f => f.Id == command.FieldId, cancellationToken);
        var dictId = field?.DictionaryId ?? Guid.Empty;

        if (Invalidation.IsActive)
        {
            if (dictId != Guid.Empty)
            {
                _ = _queries.GetFieldsByDictionaryIdAsync(dictId, default);
                _ = _queries.GetDictionaryDetailAsync(dictId, default);
            }
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        var entity = await dbContext.DictionaryFields.FirstOrDefaultAsync(f => f.Id == command.FieldId, cancellationToken);
        if (entity == null) return new ResponseDTO<string> { Status = false, StatusMessage = "Поле не найдено" };

        dbContext.DictionaryFields.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<string> { Status = true, Data = "Удалено" };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<DictionaryRecordDto>> SaveRecordAsync(SaveDictionaryRecordCommand command, CancellationToken cancellationToken = default)
    {
        var req = command.Request;
        if (Invalidation.IsActive)
        {
            _ = _queries.GetRecordsByDictionaryIdAsync(req.DictionaryId, default);
            _ = _queries.GetDictionaryDetailAsync(req.DictionaryId, default);
            if (req.Id.HasValue) _ = _queries.GetRecordByIdAsync(req.Id.Value, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        DictionaryRecordEntity entity;
        if (req.Id.HasValue && req.Id.Value != Guid.Empty)
        {
            entity = await dbContext.DictionaryRecords.FirstOrDefaultAsync(r => r.Id == req.Id.Value, cancellationToken);
            if (entity == null) return new ResponseDTO<DictionaryRecordDto> { Status = false, StatusMessage = "Запись не найдена" };
        }
        else
        {
            entity = new DictionaryRecordEntity { DictionaryId = req.DictionaryId };
            dbContext.DictionaryRecords.Add(entity);
        }

        entity.Data = req.Data;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<DictionaryRecordDto> { Status = true, Data = _mapper.Map<DictionaryRecordDto>(entity) };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteRecordAsync(DeleteDictionaryRecordCommand command, CancellationToken cancellationToken = default)
    {
        var dbContextForFind = await _dbHub.CreateDbContext(cancellationToken);
        var record = await dbContextForFind.DictionaryRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == command.RecordId, cancellationToken);
        var dictId = record?.DictionaryId ?? Guid.Empty;

        if (Invalidation.IsActive)
        {
            if (dictId != Guid.Empty)
            {
                _ = _queries.GetRecordsByDictionaryIdAsync(dictId, default);
                _ = _queries.GetDictionaryDetailAsync(dictId, default);
            }
            _ = _queries.GetRecordByIdAsync(command.RecordId, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        var entity = await dbContext.DictionaryRecords.FirstOrDefaultAsync(r => r.Id == command.RecordId, cancellationToken);
        if (entity == null) return new ResponseDTO<string> { Status = false, StatusMessage = "Запись не найдена" };

        dbContext.DictionaryRecords.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<string> { Status = true, Data = "Удалено" };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<int>> ImportRecordsAsync(ImportDictionaryRecordsCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetRecordsByDictionaryIdAsync(command.DictionaryId, default);
            _ = _queries.GetDictionaryDetailAsync(command.DictionaryId, default);
            return default!;
        }

        // Заглушка: реальный парсинг XLSX/CSV будет реализован позже. 
        // Здесь мы должны распарсить command.FileContent и создать записи.
        // Для MVP возвращаем статус OK
        return new ResponseDTO<int> { Status = true, Data = 0, StatusMessage = "Импорт успешно завершен (Mock)" };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<ExportResult>> ExportRecordsAsync(ExportDictionaryRecordsCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive) return default!;

        // Заглушка
        var csvContent = "mock,data\n1,2";
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        
        var result = new ExportResult
        {
            FileName = $"export_{command.DictionaryId}.csv",
            ContentType = "text/csv",
            FileContent = bytes
        };

        return new ResponseDTO<ExportResult> { Status = true, Data = result };
    }
}
