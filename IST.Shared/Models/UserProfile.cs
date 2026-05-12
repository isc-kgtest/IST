using IST.Shared.Enums;

namespace IST.Shared.Models;

public class UserProfile
{
    public UserSession UserSession { get; set; } = null!;

    public LanguageCode CurrentLang { get; set; } = LanguageCode.Ru;
}
