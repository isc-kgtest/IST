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
    /// <summary>
    /// Таймаут на резолв сессии через Fusion <c>IAuth</c>. На сервере IAuth
    /// в текущей конфигурации иногда зависает (роутится через RPC сам на себя).
    /// Лучше быстро отвалиться и считать актора неизвестным, чем висеть на каждой
    /// команде.
    /// </summary>
    private static readonly TimeSpan AuthLookupTimeout = TimeSpan.FromMilliseconds(1500);

    public static async ValueTask<CallerContext> RequireAuthenticatedAsync(
        this ICurrentUserStore store, IAuth auth, Session session, CancellationToken ct = default)
    {
        var userId = await TryResolveUserIdAsync(auth, session, ct).ConfigureAwait(false);
        if (userId is null)
            throw AuthorizationException.Unauthenticated();

        var caller = store.Find(userId.Value);
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
    /// «AccessDenied»-DTO вместо ошибки, и для аудита (актор неизвестен → null).
    /// </summary>
    public static async ValueTask<CallerContext?> TryFindCallerAsync(
        this ICurrentUserStore store, IAuth auth, Session session, CancellationToken ct = default)
    {
        var userId = await TryResolveUserIdAsync(auth, session, ct).ConfigureAwait(false);
        return userId is null ? null : store.Find(userId.Value);
    }

    /// <summary>
    /// Безопасный резолв userId из Fusion-сессии. Никогда не висит дольше
    /// <see cref="AuthLookupTimeout"/>, никогда не бросает исключений — возвращает
    /// null при таймауте, ошибке, отсутствии пользователя или невалидном Guid.
    /// </summary>
    private static async ValueTask<Guid?> TryResolveUserIdAsync(IAuth auth, Session session, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(AuthLookupTimeout);
            var user = await auth.GetUser(session, cts.Token).ConfigureAwait(false);
            if (user is null || !Guid.TryParse(user.Id, out var userId))
                return null;
            return userId;
        }
        catch
        {
            return null;
        }
    }
}
