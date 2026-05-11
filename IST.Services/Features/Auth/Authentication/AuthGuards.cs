using ActualLab.Fusion;

namespace IST.Services.Features.Auth.Authentication;

/// <summary>
/// Синхронные проверки доступа поверх <see cref="ICurrentUserStore"/>.
/// Никаких БД-запросов и Fusion RPC — просто lookup в памяти по
/// <see cref="Session"/>. Используются в начале каждого <c>[CommandHandler]</c>
/// для быстрого отказа неавторизованных запросов.
/// </summary>
public static class AuthGuards
{
    public static CallerContext RequireAuthenticated(this ICurrentUserStore store, Session session)
    {
        var caller = store.Find(session);
        if (caller is null)
            throw AuthorizationException.Unauthenticated();
        return caller;
    }

    /// <summary>Требует хотя бы один из переданных permission'ов (OR).</summary>
    public static CallerContext RequirePermission(
        this ICurrentUserStore store, Session session, params string[] codes)
    {
        var caller = store.RequireAuthenticated(session);
        if (codes.Length == 0)
            return caller;
        if (!codes.Any(caller.HasPermission))
            throw AuthorizationException.Forbidden();
        return caller;
    }

    /// <summary>Требует все переданные permission'ы (AND).</summary>
    public static CallerContext RequireAllPermissions(
        this ICurrentUserStore store, Session session, params string[] codes)
    {
        var caller = store.RequireAuthenticated(session);
        if (codes.Any(c => !caller.HasPermission(c)))
            throw AuthorizationException.Forbidden();
        return caller;
    }

    public static CallerContext RequireAdmin(this ICurrentUserStore store, Session session)
    {
        var caller = store.RequireAuthenticated(session);
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
}
