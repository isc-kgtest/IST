using System.Collections.Concurrent;
using ActualLab.Fusion;
using IST.Contracts.Features.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace IST.Services.Features.Auth.Authentication;

/// <summary>
/// In-memory реализация <see cref="ICurrentUserStore"/>.
/// При Set/Remove инвалидирует <see cref="IUserPresence.IsAuthenticatedAsync"/>,
/// чтобы Blazor-клиент, подписанный на presence через Fusion, среагировал
/// мгновенно (redirect на /login при удалении/деактивации пользователя).
/// </summary>
public sealed class InMemoryCurrentUserStore : ICurrentUserStore
{
    private readonly ConcurrentDictionary<Session, CallerContext> _bySession = new();
    private readonly IServiceProvider _services;
    private IUserPresence? _presence;

    public InMemoryCurrentUserStore(IServiceProvider services)
        => _services = services;

    public void Set(Session session, CallerContext caller)
    {
        if (IsDefault(session))
            throw new ArgumentException("Cannot bind a default Session to a user.", nameof(session));
        _bySession[session] = caller;
        InvalidatePresence(session);
    }

    public void Remove(Session session)
    {
        if (_bySession.TryRemove(session, out _))
            InvalidatePresence(session);
    }

    public IReadOnlyList<Session> RemoveByUserId(Guid userId)
    {
        var killed = _bySession
            .Where(kv => kv.Value.UserId == userId)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var s in killed)
            _bySession.TryRemove(s, out _);

        if (killed.Count > 0)
        {
            var presence = ResolvePresence();
            if (presence is not null)
            {
                using (Invalidation.Begin())
                {
                    foreach (var s in killed)
                        _ = presence.IsAuthenticatedAsync(s, default);
                }
            }
        }

        return killed;
    }

    public CallerContext? Find(Session session)
    {
        if (IsDefault(session))
            return null;
        return _bySession.TryGetValue(session, out var caller) ? caller : null;
    }

    private void InvalidatePresence(Session session)
    {
        var presence = ResolvePresence();
        if (presence is null)
            return;
        using (Invalidation.Begin())
            _ = presence.IsAuthenticatedAsync(session, default);
    }

    private IUserPresence? ResolvePresence()
        => _presence ??= _services.GetService<IUserPresence>();

    private static bool IsDefault(Session session)
        => session == Session.Default || string.IsNullOrEmpty(session.Id);
}
