using System.Collections.Concurrent;

namespace IST.Services.Features.Auth.Authentication;

public sealed class InMemoryCurrentUserStore : ICurrentUserStore
{
    private readonly ConcurrentDictionary<Guid, CallerContext> _byUserId = new();

    public void Set(Guid userId, CallerContext caller)
        => _byUserId[userId] = caller;

    public void Remove(Guid userId)
        => _byUserId.TryRemove(userId, out _);

    public CallerContext? Find(Guid userId)
        => _byUserId.TryGetValue(userId, out var caller) ? caller : null;
}
