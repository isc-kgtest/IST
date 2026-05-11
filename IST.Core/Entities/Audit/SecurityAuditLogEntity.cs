namespace IST.Core.Entities.Audit;

/// <summary>
/// Неизменяемая запись журнала событий безопасности (вход/выход, CRUD пользователей и ролей,
/// сброс пароля, отказы доступа). Не наследует BaseEntity — у журнала своя семантика
/// (нет soft-delete, нет UpdatedAt), чтобы исключить случайное редактирование.
/// </summary>
public sealed class SecurityAuditLogEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; }

    /// <summary>Код события — см. <see cref="SecurityAuditEventType"/>.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Пользователь, инициировавший действие (если есть).</summary>
    public Guid? ActorUserId { get; set; }
    public string? ActorLogin { get; set; }

    /// <summary>Над кем совершено действие (если применимо).</summary>
    public Guid? TargetUserId { get; set; }
    public string? TargetLogin { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool Success { get; set; }
    public string? Message { get; set; }

    /// <summary>Произвольные дополнительные детали в формате JSON (jsonb в PostgreSQL).</summary>
    public string? DetailsJson { get; set; }
}
