using ActualLab.Fusion;

namespace IST.Services.Features.Auth.Authentication;

public sealed class AuthorizationException : Exception
{
    public AuthorizationStatus Status { get; }

    private AuthorizationException(AuthorizationStatus status, string message)
        : base(message)
    {
        Status = status;
    }

    public static AuthorizationException Unauthenticated(Session? session = null)
    {
        var sid = session?.Id;
        var hint = string.IsNullOrEmpty(sid)
            ? "сессия не привязана к пользователю на сервере (Session=Default)"
            : $"сессия '{sid[..Math.Min(sid.Length, 8)]}…' не найдена в сервере (возможно, перезапуск Server'а — войдите заново)";
        return new(AuthorizationStatus.Unauthenticated,
            $"Не выполнен вход в систему: {hint}.");
    }

    public static AuthorizationException Forbidden(string? permission = null)
    {
        var hint = string.IsNullOrEmpty(permission)
            ? "недостаточно прав"
            : $"требуется привилегия '{permission}'";
        return new(AuthorizationStatus.Forbidden,
            $"Недостаточно прав для выполнения действия ({hint}).");
    }
}

public enum AuthorizationStatus
{
    Unauthenticated,
    Forbidden,
}
