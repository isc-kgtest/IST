using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Shared.DTOs.Auth;
using IST.Shared.Enums;
using IST.Shared.Models;
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

        var res = await authCommands.LoginAsync(command);

        if (res.Status)
        {
            var user = res.Data;

            var newSessionId = Guid.NewGuid();

            var userSession = new UserSession
            {
                UserId = user.Id,
                SessionId = newSessionId,
                Login = user.Login,
                FullName = user.FullName,
                Roles = user?.Roles
            };

            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                    new Claim(ClaimTypes.Name, userSession.Login),
                    new Claim(ClaimTypes.Surname, userSession.FullName),
                    new Claim(ClaimTypes.PrimarySid, newSessionId.ToString())
                };

            foreach (var userRole in userSession.Roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var claimsIdentity = new ClaimsIdentity(authClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            //// --- ДИАГНОСТИЧЕСКОЕ ЛОГИРОВАНИЕ ---
            //// Проверяем, был ли добавлен заголовок Set-Cookie в ответ сервера.
            //var setCookieHeader = httpContext.Response.Headers.SetCookie.ToString();
            //logger.LogWarning("Set-Cookie header after SignInAsync: '{Header}'", string.IsNullOrEmpty(setCookieHeader) ? "EMPTY" : setCookieHeader);
            //// --- КОНЕЦ ДИАГНОСТИКИ ---

            return Results.Ok(new
            {
                Status = true,
                StatusCode = res.StatusCode,
                StatusMessage = "Вход успешно выполнен."
            });
        }

        return Results.Ok(new
        {
            Status = false,
            StatusCode = res.StatusCode,
            StatusMessage = res.StatusMessage
        });
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
