namespace IST.Services.Features.Auth.Authentication;

/// <summary>
/// Серверный singleton-реестр <c>UserId → CallerContext</c>.
/// Заполняется при логине и при перезагрузке прав; очищается при логауте,
/// удалении пользователя или изменении его ролей/прав.
/// </summary>
public interface ICurrentUserStore
{
    void Set(Guid userId, CallerContext caller);
    void Remove(Guid userId);
    CallerContext? Find(Guid userId);
}
