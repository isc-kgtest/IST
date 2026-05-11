using ActualLab.Fusion;
using ActualLab.Fusion.EntityFramework;
using IST.Contracts.Features.Audit;
using IST.Core.Entities.Audit;
using IST.Core.Entities.Auth;
using IST.Infrastructure.Data;
using IST.Services.Features.Auth.Authentication;
using IST.Shared.DTOs.Audit;
using Microsoft.EntityFrameworkCore;

namespace IST.Services.Features.Audit;

public class AuditQueries : IAuditQueries
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly ICurrentUserStore _users;

    public AuditQueries(DbHub<AppDbContext> dbHub, ICurrentUserStore users)
    {
        _dbHub = dbHub;
        _users = users;
    }

    [ComputeMethod(MinCacheDuration = 30)]
    public virtual async Task<AuditLogPageDto> GetAuditPageAsync(
        Session session, AuditFilter filter, int skip, int take,
        CancellationToken cancellationToken = default)
    {
        // Синхронный lookup — никаких IAuth.GetUser.
        var caller = _users.Find(session);
        if (caller is null || !caller.HasPermission(Permissions.AuditView))
            return new AuditLogPageDto { AccessDenied = true };

        if (take <= 0) take = 50;
        if (take > 500) take = 500;
        if (skip < 0) skip = 0;

        await using var db = await _dbHub.CreateDbContext(cancellationToken);

        IQueryable<SecurityAuditLogEntity> q = db.SecurityAuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.EventType))
            q = q.Where(x => x.EventType == filter.EventType);

        if (!string.IsNullOrWhiteSpace(filter.ActorLoginContains))
        {
            var needle = filter.ActorLoginContains;
            q = q.Where(x => x.ActorLogin != null && EF.Functions.ILike(x.ActorLogin, $"%{needle}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetLoginContains))
        {
            var needle = filter.TargetLoginContains;
            q = q.Where(x => x.TargetLogin != null && EF.Functions.ILike(x.TargetLogin, $"%{needle}%"));
        }

        if (filter.FromUtc.HasValue)
            q = q.Where(x => x.Timestamp >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            q = q.Where(x => x.Timestamp <= filter.ToUtc.Value);

        if (filter.OnlyFailures == true)
            q = q.Where(x => !x.Success);

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(x => x.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(x => new AuditLogEntryDto
            {
                Id = x.Id,
                Timestamp = x.Timestamp,
                EventType = x.EventType,
                Success = x.Success,
                ActorUserId = x.ActorUserId,
                ActorLogin = x.ActorLogin,
                TargetUserId = x.TargetUserId,
                TargetLogin = x.TargetLogin,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                Message = x.Message,
                DetailsJson = x.DetailsJson,
            })
            .ToListAsync(cancellationToken);

        return new AuditLogPageDto { Items = items, Total = total };
    }

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual async Task<List<string>> GetEventTypesAsync(Session session, CancellationToken cancellationToken = default)
    {
        var caller = _users.Find(session);
        if (caller is null || !caller.HasPermission(Permissions.AuditView))
            return new List<string>();

        await using var db = await _dbHub.CreateDbContext(cancellationToken);
        return await db.SecurityAuditLogs.AsNoTracking()
            .Select(x => x.EventType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
    }
}
