using ActualLab.Fusion;
using ActualLab.Fusion.Authentication;

namespace IST.Services.Features.Auth.Authentication;

/// <summary>
/// Гарды авторизации. UserId резолвится через Fusion <see cref="IAuth"/>
/// (Fusion-сессия → claims из cookie), затем по нему достаём <see cref="CallerContext"/>
/// из <see cref="ICurrentUserStore"/>.
/// </summary>
public static class AuthGuards
{
    public static async ValueTask<CallerContext> RequireAuthenticatedAsync(
        this ICurrentUserStore store, IAuth auth, Session session, CancellationToken ct = default)
    {
        var user = await auth.GetUser(session, ct).ConfigureAwait(false);
        if (user is null || !Guid.TryParse(user.Id, out var userId))
            throw AuthorizationException.Unauthenticated();

        var caller = store.Find(userId);
        if (caller is null)
            throw AuthorizationException.Unauthenticated();
        return caller;
    }

    /// <summary>Требует хотя бы один из переданных permission'ов (OR).</summary>
    public static async ValueTask<CallerContext> RequirePermissionAsync(
        this ICurrentUserStore store, IAuth auth, Session session,
        CancellationToken ct, params string[] codes)
    {
        var caller = await store.RequireAuthenticatedAsync(auth, session, ct).ConfigureAwait(false);
        if (codes.Length == 0)
            return caller;
        if (!codes.Any(caller.HasPermission))
            throw AuthorizationException.Forbidden();
        return caller;
    }

    /// <summary>Требует все переданные permission'ы (AND).</summary>
    public static async ValueTask<CallerContext> RequireAllPermissionsAsync(
        this ICurrentUserStore store, IAuth auth, Session session,
        CancellationToken ct, params string[] codes)
    {
        var caller = await store.RequireAuthenticatedAsync(auth, session, ct).ConfigureAwait(false);
        if (codes.Any(c => !caller.HasPermission(c)))
            throw AuthorizationException.Forbidden();
        return caller;
    }

    public static async ValueTask<CallerContext> RequireAdminAsync(
        this ICurrentUserStore store, IAuth auth, Session session, CancellationToken ct = default)
    {
        var caller = await store.RequireAuthenticatedAsync(auth, session, ct).ConfigureAwait(false);
        if (!caller.IsAdmin)
            throw AuthorizationException.Forbidden();
        return caller;
    }

    public static CallerContext RequireRole(this CallerContext caller, params string[] roles)
    {
        if (!roles.Any(caller.IsInRole))
            throw AuthorizationException.Forbidden();
        return caller;
    }

    /// <summary>
    /// Soft-вариант: возвращает <see cref="CallerContext"/> если он есть в реестре,
    /// либо null. Не бросает исключений — удобно для query-методов, отдающих
    /// «AccessDenied»-DTO вместо ошибки.
    /// </summary>
    public static async ValueTask<CallerContext?> TryFindCallerAsync(
        this ICurrentUserStore store, IAuth auth, Session session, CancellationToken ct = default)
    {
        var user = await auth.GetUser(session, ct).ConfigureAwait(false);
        if (user is null || !Guid.TryParse(user.Id, out var userId))
            return null;
        return store.Find(userId);
    }
}
