using System.Security.Claims;
using ActualLab.Fusion;
using Microsoft.AspNetCore.Components.Authorization;

namespace IST.Admin.Auth;

/// <summary>
/// Поставщик текущей Fusion-сессии для Razor-компонентов.
///
/// Идентификатор сессии — Guid, выписанный при логине и сохранённый в auth-cookie
/// как <see cref="ClaimTypes.PrimarySid"/>. Этот же Guid используется как ключ в
/// серверном <c>ICurrentUserStore</c>. Такой подход надёжно работает в Blazor
/// Server (как при initial SSR, так и в interactive circuit), потому что claims
/// из cookie доступны через <see cref="AuthenticationStateProvider"/> в обоих
/// сценариях.
/// </summary>
public sealed class SessionAccessor
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private Session? _cached;

    public SessionAccessor(AuthenticationStateProvider authStateProvider)
        => _authStateProvider = authStateProvider;

    public async ValueTask<Session> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is { } cached)
            return cached;

        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        var sid = auth.User.FindFirst(ClaimTypes.PrimarySid)?.Value;
        if (string.IsNullOrEmpty(sid))
        {
            _cached = Session.Default;
            return Session.Default;
        }

        var session = new Session(sid);
        _cached = session;
        return session;
    }
}
