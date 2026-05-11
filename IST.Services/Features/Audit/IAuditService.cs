namespace IST.Services.Features.Audit;

/// <summary>
/// Сервис записи событий в журнал безопасности.
/// Singleton. Ошибки логирования не пробрасываются — аудит не должен ронять команду.
/// </summary>
public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
