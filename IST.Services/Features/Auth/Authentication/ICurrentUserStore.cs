using ActualLab.Fusion;

namespace IST.Services.Features.Auth.Authentication;

/// <summary>
/// Серверный singleton-реестр <c>Session → CallerContext</c>.
/// <para>
/// Session создаётся ОДНАЖДЫ при логине (HTTP endpoint), кладётся в claim
/// <c>ClaimTypes.PrimarySid</c> cookie и параллельно передаётся в
/// <c>LoginCommand</c> через RPC. На сервере мы получаем тот же id и
/// сохраняем по нему <see cref="CallerContext"/>.
/// </para>
/// <para>
/// Все последующие RPC-команды от клиента передают <c>Session</c> явно
/// (из cookie через <c>SessionAccessor</c>), и сервер делает прямой
/// <see cref="Find"/> — без обращения к Fusion <c>IAuth</c>.
/// </para>
/// </summary>
public interface ICurrentUserStore
{
    void Set(Session session, CallerContext caller);
    void Remove(Session session);

    /// <summary>
    /// Снимает все сессии конкретного пользователя. Вызывать при удалении,
    /// деактивации, сбросе пароля, изменении ролей/прав.
    /// </summary>
    /// <returns>Сессии, которые были сняты.</returns>
    IReadOnlyList<Session> RemoveByUserId(Guid userId);

    CallerContext? Find(Session session);
}
