namespace IST.Core.Entities.BaseEntities;

/// <summary>
/// Ambient-контекст текущего актора для заполнения CreatedBy/UpdatedBy/DeletedBy
/// в <c>AppDbContext.SaveChangesAsync</c>. Командные обработчики оборачивают свою
/// логику в <see cref="Begin"/>, передавая UserId; внутри SaveChangesAsync значение
/// читается из <see cref="CurrentUserId"/>.
/// </summary>
public static class AuditContext
{
    private static readonly AsyncLocal<Guid?> _currentUserId = new();

    public static Guid? CurrentUserId => _currentUserId.Value;

    /// <summary>
    /// Активирует ambient-актора в текущем async-потоке. Возвращает scope —
    /// диспоуз восстанавливает предыдущее значение.
    /// </summary>
    public static IDisposable Begin(Guid? userId)
    {
        var previous = _currentUserId.Value;
        _currentUserId.Value = userId;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly Guid? _previous;
        public Scope(Guid? previous) => _previous = previous;
        public void Dispose() => _currentUserId.Value = _previous;
    }
}
