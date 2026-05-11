namespace IST.Core.Entities.Auth;

/// <summary>
/// Реестр кодов прав (permissions). Используются в БД (PermissionEntity.Code)
/// и в коде через AuthGuards.RequirePermission(...).
/// </summary>
public static class Permissions
{
    // Пользователи
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string UsersPasswordReset = "users.password_reset";

    // Роли
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";
    public const string RolesAssign = "roles.assign";

    // Справочники / НСИ
    public const string DictionariesView = "dictionaries.view";
    public const string DictionariesManage = "dictionaries.manage";

    // Аудит
    public const string AuditView = "audit.view";

    // Доступ к порталам
    public const string AdminAccess = "admin.access";

    public static readonly IReadOnlyList<PermissionDescriptor> All = new[]
    {
        new PermissionDescriptor(UsersView,           "Просмотр пользователей",         "Users"),
        new PermissionDescriptor(UsersCreate,         "Создание пользователей",         "Users"),
        new PermissionDescriptor(UsersUpdate,         "Редактирование пользователей",   "Users"),
        new PermissionDescriptor(UsersDelete,         "Удаление пользователей",         "Users"),
        new PermissionDescriptor(UsersPasswordReset,  "Сброс пароля пользователя",      "Users"),

        new PermissionDescriptor(RolesView,           "Просмотр ролей",                 "Roles"),
        new PermissionDescriptor(RolesManage,         "Управление ролями",              "Roles"),
        new PermissionDescriptor(RolesAssign,         "Назначение ролей пользователям", "Roles"),

        new PermissionDescriptor(DictionariesView,    "Просмотр справочников",          "Dictionaries"),
        new PermissionDescriptor(DictionariesManage,  "Управление справочниками",       "Dictionaries"),

        new PermissionDescriptor(AuditView,           "Просмотр журнала аудита",        "Audit"),

        new PermissionDescriptor(AdminAccess,         "Доступ к административной панели", "Portals"),
    };
}

public sealed record PermissionDescriptor(string Code, string Description, string Category);
