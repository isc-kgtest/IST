using ActualLab.CommandR;
using ActualLab.CommandR.Configuration;
using ActualLab.Fusion.Authentication;
using IST.Contracts.Features.Auth;
using IST.Services.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace IST.Services.Features.Auth;

public class AuthHandlers(
    IAuthBackend authBackend,
    AppDbContext dbContext) // Dependency injection is implicit via primary constructor
{
    private const string TelegramBotToken = "YOUR_BOT_TOKEN"; // Should come from configuration in real app

    [CommandHandler]
    public virtual async Task LoginAdminCmdHandler(LoginAdminCmd command, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == command.Username, cancellationToken);
            
        if (user == null || !VerifyPassword(command.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var identity = new UserIdentity("admin", user.Id.ToString());
        var sessionUser = new User(identity.Id, user.Username ?? "Admin")
            .WithIdentity(identity)
            .WithClaim("Role", "Admin");

        // await authBackend.SignIn(new AuthBackend_SignIn(command.Session, sessionUser, identity));
    }

    [CommandHandler]
    public virtual async Task LoginTelegramCmdHandler(LoginTelegramCmd command, CancellationToken cancellationToken = default)
    {
        var parsedInitData = HttpUtility.ParseQueryString(command.TelegramInitData);
        var hash = parsedInitData["hash"];
        
        if (string.IsNullOrEmpty(hash) || !ValidateTelegramHash(command.TelegramInitData, hash))
            throw new UnauthorizedAccessException("Invalid Telegram Web App signature.");

        // Extract mock/real user info, in production parse JSON from parsedInitData["user"]
        var tgUserId = "extracted_tg_id"; 
        var tgUsername = "extracted_tg_username";

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.TelegramId == tgUserId, cancellationToken);

        if (user == null)
        {
            user = new AppUser { TelegramId = tgUserId, Username = tgUsername, Role = "Client" };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            // Record the DB operation context for Fusion so caches invalidate perfectly
            // CommandContext.GetCurrent().Operation.Items.Set(user); 
        }

        var identity = new UserIdentity("telegram", tgUserId);
        var sessionUser = new User(identity.Id, user.Username ?? tgUsername)
            .WithIdentity(identity)
            .WithClaim("Role", "Client");

        // await authBackend.SignIn(new AuthBackend_SignIn(command.Session, sessionUser, identity));
    }

    private bool VerifyPassword(string input, string? hash) => input == hash; // TODO: Use real hashing algorithm

    private bool ValidateTelegramHash(string initData, string expectedHash)
    {
        var dataCheckString = string.Join("\n", initData.Split('&')
            .Where(x => !x.StartsWith("hash="))
            .OrderBy(x => x));

        using var hmacSha256 = new HMACSHA256(new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData")).ComputeHash(Encoding.UTF8.GetBytes(TelegramBotToken)));
        var computedHash = BitConverter.ToString(hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString))).Replace("-", "").ToLower();
        
        return computedHash == expectedHash;
    }
}
