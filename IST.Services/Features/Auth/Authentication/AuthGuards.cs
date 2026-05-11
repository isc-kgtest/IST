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
            throw AuthorizationException.Unauthenticated(session);
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
            throw AuthorizationException.Forbidden(codes.FirstOrDefault());
        return caller;
    }

    /// <summary>Требует все переданные permission'ы (AND).</summary>
    public static CallerContext RequireAllPermissions(
        this ICurrentUserStore store, Session session, params string[] codes)
    {
        var caller = store.RequireAuthenticated(session);
        var missing = codes.FirstOrDefault(c => !caller.HasPermission(c));
        if (missing is not null)
            throw AuthorizationException.Forbidden(missing);
        return caller;
    }

    public static CallerContext RequireAdmin(this ICurrentUserStore store, Session session)
    {
        var caller = store.RequireAuthenticated(session);
        if (!caller.IsAdmin)
            throw AuthorizationException.Forbidden("admin");
        return caller;
    }

    public static CallerContext RequireRole(this CallerContext caller, params string[] roles)
    {
        if (!roles.Any(caller.IsInRole))
            throw AuthorizationException.Forbidden(roles.FirstOrDefault());
        return caller;
    }
}
