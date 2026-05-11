using System.Security.Claims;
using ActualLab.Fusion;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace IST.Admin.Auth;

/// <summary>
/// Поставщик текущей Fusion-сессии для Razor-компонентов.
///
/// Идентификатор сессии — Guid, выписанный при логине и сохранённый в auth-cookie
/// как <see cref="ClaimTypes.PrimarySid"/>. Этот же Guid используется как ключ
/// в серверном <c>ICurrentUserStore</c>.
/// </summary>
public sealed class SessionAccessor
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<SessionAccessor> _log;
    private Session? _cached;

    public SessionAccessor(AuthenticationStateProvider authStateProvider, ILogger<SessionAccessor> log)
    {
        _authStateProvider = authStateProvider;
        _log = log;
    }

    public async ValueTask<Session> GetAsync(CancellationToken cancellationToken = default)
    {
        // Не кэшируем результат, если он Session.Default — claims могут появиться
        // позже в жизни circuit'а (например, после forceLoad с обновлённой cookie).
        if (_cached is { } cached && cached != Session.Default)
            return cached;

        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        var isAuth = auth.User.Identity?.IsAuthenticated == true;
        var sid = auth.User.FindFirst(ClaimTypes.PrimarySid)?.Value;

        _log.LogInformation(
            "SessionAccessor: IsAuthenticated={IsAuth}, PrimarySid='{Sid}'",
            isAuth, sid ?? "(null)");

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
