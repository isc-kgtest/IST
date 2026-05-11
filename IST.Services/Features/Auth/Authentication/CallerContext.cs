namespace IST.Services.Features.Auth.Authentication;

/// <summary>
/// Контекст вызывающего пользователя — собирается при логине и кладётся в
/// <see cref="ICurrentUserStore"/> по ключу Fusion <c>Session</c>.
/// Внутри сервера используется для проверок прав через <see cref="AuthGuards"/>.
/// </summary>
public sealed record CallerContext(
    Guid UserId,
    string Login,
    string FullName,
    IReadOnlyCollection<string> Roles,
    IReadOnlySet<string> Permissions)
{
    private static readonly StringComparer RoleComparer = StringComparer.OrdinalIgnoreCase;

    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public bool IsInRole(string role) => Roles.Contains(role, RoleComparer);
    public bool IsAdmin => IsInRole("admin") || IsInRole("superadmin");
    public bool HasPermission(string code) => Permissions.Contains(code);
}
