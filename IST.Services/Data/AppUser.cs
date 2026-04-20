namespace IST.Services.Data;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? TelegramId { get; set; }
    public string Role { get; set; } = "Client";
}
