namespace IST.Services.Features.Auth.Authentication;

public sealed class AuthorizationException : Exception
{
    public AuthorizationStatus Status { get; }

    private AuthorizationException(AuthorizationStatus status, string message)
        : base(message)
    {
        Status = status;
    }

    public static AuthorizationException Unauthenticated()
        => new(AuthorizationStatus.Unauthenticated, "Не выполнен вход в систему.");

    public static AuthorizationException Forbidden()
        => new(AuthorizationStatus.Forbidden, "Недостаточно прав для выполнения действия.");
}

public enum AuthorizationStatus
{
    Unauthenticated,
    Forbidden,
}
