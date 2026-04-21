namespace IST.Shared.Models;

public class UserSession
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Login { get; set; }
    public string FullName { get; set; }
    public IList<string>? Roles { get; set; } = new List<string>();
    public DateTime ExDate { get; set; }
}