using ActualLab.Fusion;

namespace IST.Contracts.Features.Auth;

/// <summary>
/// Реактивный фасад для отслеживания валидности Fusion-сессии. Клиент Blazor
/// подписывается на <see cref="IsAuthenticatedAsync"/> через Fusion; когда
/// сервер удаляет сессию (logout, delete user, ресет пароля, ...) —
/// <see cref="IUserPresence"/> инвалидируется и клиент мгновенно получает
/// <c>false</c> → редирект на /login.
/// </summary>
public interface IUserPresence : IComputeService
{
    [ComputeMethod(MinCacheDuration = 60)]
    Task<bool> IsAuthenticatedAsync(Session session, CancellationToken cancellationToken = default);
}
