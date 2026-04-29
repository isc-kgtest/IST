using ActualLab.Fusion;
using ActualLab.Fusion.EntityFramework;
using IST.Contracts.Features.Dictionaries;
using IST.Infrastructure.Data;
using IST.Shared.DTOs.Dictionaries;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace IST.Services.Features.Dictionaries;

public class DictionaryQueries : IDictionaryQueries
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IMapper _mapper;

    public DictionaryQueries(DbHub<AppDbContext> dbHub, IMapper mapper)
    {
        _dbHub = dbHub;
        _mapper = mapper;
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<DictionaryDto>> GetAllDictionariesAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var items = await dbContext.Dictionaries.AsNoTracking()
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<DictionaryDto>>(items);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<DictionaryDto?> GetDictionaryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var item = await dbContext.Dictionaries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return item == null ? null : _mapper.Map<DictionaryDto>(item);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<DictionaryDto?> GetDictionaryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var item = await dbContext.Dictionaries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Slug == slug, cancellationToken);
        return item == null ? null : _mapper.Map<DictionaryDto>(item);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<DictionaryFieldDto>> GetFieldsByDictionaryIdAsync(Guid dictionaryId, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var items = await dbContext.DictionaryFields.AsNoTracking()
            .Where(f => f.DictionaryId == dictionaryId)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<DictionaryFieldDto>>(items);
    }

    [ComputeMethod(MinCacheDuration = 30)]
    public virtual async Task<List<DictionaryRecordDto>> GetRecordsByDictionaryIdAsync(Guid dictionaryId, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var items = await dbContext.DictionaryRecords.AsNoTracking()
            .Where(r => r.DictionaryId == dictionaryId)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<DictionaryRecordDto>>(items);
    }

    [ComputeMethod(MinCacheDuration = 30)]
    public virtual async Task<DictionaryRecordDto?> GetRecordByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var item = await dbContext.DictionaryRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return item == null ? null : _mapper.Map<DictionaryRecordDto>(item);
    }

    [ComputeMethod(MinCacheDuration = 30)]
    public virtual async Task<DictionaryDetailDto?> GetDictionaryDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbHub.CreateDbContext(cancellationToken);
        var item = await dbContext.Dictionaries.AsNoTracking()
            .Include(d => d.Fields)
            .Include(d => d.Records)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        
        if (item == null) return null;
        
        var dto = new DictionaryDetailDto
        {
            Metadata = _mapper.Map<DictionaryDto>(item),
            Fields = _mapper.Map<List<DictionaryFieldDto>>(item.Fields.OrderBy(f => f.SortOrder).ToList()),
            Records = _mapper.Map<List<DictionaryRecordDto>>(item.Records.ToList())
        };
        return dto;
    }
}
