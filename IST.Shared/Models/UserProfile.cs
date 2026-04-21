using IST.Shared.Enums;

namespace IST.Shared.Models;

public class UserProfile
{
    public UserSession UserSession { get; set; }

    public LanguageCode CurrentLang { get; set; } = LanguageCode.Ru;
}
