using ActualLab.Fusion;
using IST.Contracts.Features.Auth;

namespace IST.Services.Features.Auth.Authentication;

public class UserPresence : IUserPresence
{
    private readonly ICurrentUserStore _store;

    public UserPresence(ICurrentUserStore store) => _store = store;

    [ComputeMethod(MinCacheDuration = 60)]
    public virtual Task<bool> IsAuthenticatedAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session == Session.Default || string.IsNullOrEmpty(session.Id))
            return Task.FromResult(false);
        return Task.FromResult(_store.Find(session) is not null);
    }
}
