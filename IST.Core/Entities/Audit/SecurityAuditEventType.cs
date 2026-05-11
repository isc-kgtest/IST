namespace IST.Core.Entities.Audit;

/// <summary>
/// Коды событий, записываемые в <see cref="SecurityAuditLogEntity"/>.
/// Все коды стабильны — на них завязана фильтрация в UI журнала.
/// </summary>
public static class SecurityAuditEventType
{
    // Аутентификация
    public const string LoginSuccess         = "login.success";
    public const string LoginFailed          = "login.failed";
    public const string LoginInactive        = "login.inactive";
    public const string LoginPasswordExpired = "login.password_expired";
    public const string Logout               = "logout";

    // Управление пользователями
    public const string UserCreated          = "user.created";
    public const string UserUpdated          = "user.updated";
    public const string UserDeleted          = "user.deleted";
    public const string UserPasswordChanged  = "user.password_changed";
    public const string UserPasswordReset    = "user.password_reset";

    // Управление ролями
    public const string RoleCreated          = "role.created";
    public const string RoleUpdated          = "role.updated";
    public const string RoleDeleted          = "role.deleted";

    // Назначение ролей
    public const string UserRoleAssigned     = "user.role_assigned";
    public const string UserRoleUpdated      = "user.role_assignment_updated";
    public const string UserRoleRevoked      = "user.role_revoked";

    // Авторизация
    public const string AccessDenied         = "access.denied";
}
