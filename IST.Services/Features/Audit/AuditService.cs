using ActualLab.Fusion.EntityFramework;
using IST.Core.Entities.Audit;
using IST.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace IST.Services.Features.Audit;

public sealed class AuditService : IAuditService
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly ILogger<AuditService> _log;

    public AuditService(DbHub<AppDbContext> dbHub, ILogger<AuditService> log)
    {
        _dbHub = dbHub;
        _log = log;
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            // readWrite=true → пишем через НЕоперационный контекст, чтобы не дёргать
            // Fusion-инвалидацию (NpgsqlWatcher через _Operations).
            await using var db = await _dbHub.CreateDbContext(true, cancellationToken);

            db.SecurityAuditLogs.Add(new SecurityAuditLogEntity
            {
                Timestamp    = DateTime.UtcNow,
                EventType    = entry.EventType,
                Success      = entry.Success,
                ActorUserId  = entry.ActorUserId,
                ActorLogin   = Truncate(entry.ActorLogin, 128),
                TargetUserId = entry.TargetUserId,
                TargetLogin  = Truncate(entry.TargetLogin, 128),
                IpAddress    = Truncate(entry.IpAddress, 64),
                UserAgent    = Truncate(entry.UserAgent, 512),
                Message      = Truncate(entry.Message, 512),
                DetailsJson  = entry.DetailsJson,
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Аудит не падает вместе с командой — только пишем в лог приложения.
            _log.LogError(ex, "Failed to write security audit entry of type {EventType}", entry.EventType);
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value[..max];
    }
}
