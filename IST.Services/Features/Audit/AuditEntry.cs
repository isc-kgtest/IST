namespace IST.Services.Features.Audit;

/// <summary>
/// DTO для записи в журнал безопасности. Передаётся в <see cref="IAuditService.LogAsync"/>.
/// </summary>
public sealed record AuditEntry(
    string EventType,
    bool Success,
    Guid? ActorUserId = null,
    string? ActorLogin = null,
    Guid? TargetUserId = null,
    string? TargetLogin = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? Message = null,
    string? DetailsJson = null);
