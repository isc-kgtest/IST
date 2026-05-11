using System.Security.Claims;
using ActualLab.Fusion;
using Microsoft.AspNetCore.Components.Authorization;

namespace IST.Admin.Auth;

/// <summary>
/// Scoped-сервис для Blazor-компонентов: читает claim <see cref="ClaimTypes.PrimarySid"/>
/// из cookie через встроенный <see cref="AuthenticationStateProvider"/> и возвращает
/// соответствующую Fusion <see cref="Session"/>.
/// <para>
/// Используется так:
/// <code>
/// var session = await SessionAccessor.GetAsync();
/// await AuthCommands.CreateUserAsync(new CreateUserCommand(session, request));
/// </code>
/// </para>
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
