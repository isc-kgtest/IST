using IST.Contracts.Features.Organization;
using IST.Core.Entities.Organization;
using IST.Shared.DTOs.Organization;

namespace IST.Services.Features.Organization;

public class OrganizationQueries : IOrganizationQueries
{
    private readonly DbHub<AppDbContext> _dbHub;

    public OrganizationQueries(DbHub<AppDbContext> dbHub) => _dbHub = dbHub;

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<OrganizationNodeTypeDto>> GetNodeTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbHub.CreateDbContext(cancellationToken);
        return await db.OrganizationNodeTypes.AsNoTracking()
            .OrderBy(t => t.Level).ThenBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => new OrganizationNodeTypeDto
            {
                Id = t.Id, Code = t.Code, Name = t.Name, Description = t.Description,
                Level = t.Level, SortOrder = t.SortOrder, Icon = t.Icon,
            })
            .ToListAsync(cancellationToken);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<OrganizationNodeDto>> GetChildrenAsync(Guid? parentId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbHub.CreateDbContext(cancellationToken);

        var nodes = await db.OrganizationNodes.AsNoTracking()
            .Include(n => n.NodeType)
            .Where(n => n.ParentNodeId == parentId)
            .OrderBy(n => n.SortOrder).ThenBy(n => n.Name)
            .ToListAsync(cancellationToken);

        var ids = nodes.Select(n => n.Id).ToList();
        var childCounts = await db.OrganizationNodes.AsNoTracking()
            .Where(n => n.ParentNodeId != null && ids.Contains(n.ParentNodeId.Value))
            .GroupBy(n => n.ParentNodeId!.Value)
            .Select(g => new { Pid = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var countByPid = childCounts.ToDictionary(x => x.Pid, x => x.Count);

        return nodes.Select(n => Map(n, countByPid.GetValueOrDefault(n.Id, 0))).ToList();
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<OrganizationNodeDto?> GetNodeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbHub.CreateDbContext(cancellationToken);
        var node = await db.OrganizationNodes.AsNoTracking()
            .Include(n => n.NodeType)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (node is null) return null;
        var childrenCount = await db.OrganizationNodes.AsNoTracking()
            .CountAsync(n => n.ParentNodeId == id, cancellationToken);
        return Map(node, childrenCount);
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<OrganizationNodeFlatDto>> GetAllNodesFlatAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbHub.CreateDbContext(cancellationToken);

        var raw = await db.OrganizationNodes.AsNoTracking()
            .Include(n => n.NodeType)
            .OrderBy(n => n.Depth).ThenBy(n => n.SortOrder).ThenBy(n => n.Name)
            .Select(n => new
            {
                n.Id, n.Name, n.ParentNodeId, n.Depth, n.IsActive,
                NodeTypeName = n.NodeType != null ? n.NodeType.Name : ""
            })
            .ToListAsync(cancellationToken);

        var nameById = raw.ToDictionary(x => x.Id, x => x.Name);
        var parentById = raw.ToDictionary(x => x.Id, x => x.ParentNodeId);

        string BuildPath(Guid id)
        {
            var parts = new List<string>();
            var current = (Guid?)id;
            while (current.HasValue && nameById.TryGetValue(current.Value, out var name))
            {
                parts.Add(name);
                current = parentById[current.Value];
            }
            parts.Reverse();
            return string.Join(" → ", parts);
        }

        return raw.Select(x => new OrganizationNodeFlatDto
        {
            Id = x.Id,
            Name = x.Name,
            NodeTypeName = x.NodeTypeName,
            Depth = x.Depth,
            IsActive = x.IsActive,
            FullPath = BuildPath(x.Id),
        }).ToList();
    }

    private static OrganizationNodeDto Map(OrganizationNodeEntity n, int childrenCount) => new()
    {
        Id = n.Id,
        NodeTypeId = n.NodeTypeId,
        NodeTypeName = n.NodeType?.Name ?? string.Empty,
        NodeTypeIcon = n.NodeType?.Icon,
        ParentNodeId = n.ParentNodeId,
        Name = n.Name,
        Code = n.Code,
        Path = n.Path,
        Depth = n.Depth,
        SortOrder = n.SortOrder,
        IsActive = n.IsActive,
        Description = n.Description,
        ChildrenCount = childrenCount,
    };
}
