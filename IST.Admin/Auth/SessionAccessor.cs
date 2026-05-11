using System.Security.Claims;
using ActualLab.Fusion;
using Microsoft.AspNetCore.Components.Authorization;

namespace IST.Admin.Auth;

/// <summary>
/// Scoped-сервис для Blazor-компонентов: читает claim <see cref="ClaimTypes.PrimarySid"/>
/// из cookie через встроенный <see cref="AuthenticationStateProvider"/> и возвращает
/// соответствующую Fusion <see cref="Session"/>.
/// <para>
/// НЕ кэшируем — claim'ы могут поменяться внутри одного circuit'а (logout/login),
/// а accessor легковесен (просто dictionary lookup в ClaimsPrincipal).
/// </para>
/// <para>
/// Состояния:
/// <list type="bullet">
///   <item><c>Authenticated + PrimarySid есть</c> → возвращаем валидную Session.</item>
///   <item><c>Authenticated + PrimarySid нет</c> (старая cookie до новой архитектуры) →
///     <see cref="IsStaleCookie"/> = true, вернём Session.Default; layout
///     должен выкинуть юзера на /api/auth/logout, чтобы cookie почистилась.</item>
///   <item><c>Не authenticated</c> → Session.Default.</item>
/// </list>
/// </para>
/// </summary>
public sealed class SessionAccessor
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public SessionAccessor(AuthenticationStateProvider authStateProvider)
        => _authStateProvider = authStateProvider;

    /// <summary>
    /// True, если cookie идентифицирует юзера, но в ней нет PrimarySid —
    /// это значит, что cookie была выписана до перехода на Session-based архитектуру
    /// и должна быть переустановлена через /api/auth/logout + повторный логин.
    /// Заполняется при последнем вызове <see cref="GetAsync"/>.
    /// </summary>
    public bool IsStaleCookie { get; private set; }

    public async ValueTask<Session> GetAsync(CancellationToken cancellationToken = default)
    {
        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        var isAuthenticated = auth.User.Identity?.IsAuthenticated == true;
        var sid = auth.User.FindFirst(ClaimTypes.PrimarySid)?.Value;

        IsStaleCookie = isAuthenticated && string.IsNullOrEmpty(sid);

        if (string.IsNullOrEmpty(sid))
            return Session.Default;

        return new Session(sid);
    }
}
