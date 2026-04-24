using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Shared.DTOs.Auth;
using IST.Shared.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace IST.Admin.Auth;

/// <summary>
/// MinimalAPI endpoints для Login/Logout.
/// Blazor Server InteractiveServer не может вызвать HttpContext.SignInAsync
/// из SignalR-контекста, поэтому login/logout реализованы как HTTP-endpoints.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", (Delegate)HandleLogin)
            .AllowAnonymous();

        app.MapGet("/api/auth/logout", (Delegate)HandleLogout)
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleLogin(
        HttpContext httpContext,
        LoginRequest request,
        IAuthCommands authCommands)
    {
        var command = new LoginCommand(
            ActualLab.Fusion.Session.Default,
            request.Login,
            request.Password);

        var result = await authCommands.LoginAsync(command);

        if (!result.Status)
        {
            return Results.Json(new LoginResponse(false, result.StatusMessage, (int)result.StatusCode));
        }

        var user = result.Data!;

        // Формируем claims для cookie
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new("FullName", user.FullName),
            new(ClaimTypes.Email, user.Email ?? ""),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Results.Json(new LoginResponse(true, result.StatusMessage, (int)result.StatusCode));
    }

    private static async Task<IResult> HandleLogout(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }
}

/// <summary>Тело запроса login-endpoint'а.</summary>
public record LoginRequest(string Login, string Password);

/// <summary>Ответ login-endpoint'а.</summary>
public record LoginResponse(bool Success, string Message, int StatusCode);
