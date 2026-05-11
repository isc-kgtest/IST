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
///
/// При первом успешном <see cref="GetAsync"/> также синхронно прописывает
/// <see cref="ISessionResolver.Session"/> — без этого Fusion-овский RPC-клиент
/// продолжал бы подмешивать в исходящие команды свою анонимную "~"-сессию.
/// </summary>
public sealed class SessionAccessor
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ISessionResolver _sessionResolver;
    private readonly ILogger<SessionAccessor> _log;
    private Session? _cached;

    public SessionAccessor(
        AuthenticationStateProvider authStateProvider,
        ISessionResolver sessionResolver,
        ILogger<SessionAccessor> log)
    {
        _authStateProvider = authStateProvider;
        _sessionResolver = sessionResolver;
        _log = log;
    }

    public async ValueTask<Session> GetAsync(CancellationToken cancellationToken = default)
    {
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

        // КЛЮЧЕВОЕ: сразу прописываем session в Fusion ISessionResolver.
        // Иначе RPC-клиент подсунет в command.Session свою анонимную "~"-сессию,
        // и сервер не найдёт CallerContext в store.
        try
        {
            _sessionResolver.Session = session;
            _log.LogInformation("SessionAccessor: bound ISessionResolver.Session='{Sid}'", session.Id);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SessionAccessor: failed to set ISessionResolver.Session (ignored).");
        }

        return session;
    }
}
