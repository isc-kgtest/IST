using ActualLab.Fusion;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Auth.Commands;
using IST.Core.Entities.Auth;
using IST.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace IST.Admin.Auth;

/// <summary>
/// Minimal API endpoints для логина/логаута.
/// <para>
/// <c>Session</c> создаётся прямо здесь как UUID. Этот же id мы кладём в claim
/// <see cref="ClaimTypes.PrimarySid"/> в cookie и передаём в <c>LoginCommand</c>
/// на сервер. Сервер через <c>ICurrentUserStore.Set</c> привязывает к этому
/// Session-id <c>CallerContext</c>. Все следующие RPC-вызовы клиента берут
/// Session из cookie (через <c>SessionAccessor</c>) и сервер находит юзера
/// прямым lookup'ом в памяти — без Fusion <c>IAuth.GetUser</c>.
/// </para>
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", (Delegate)HandleLogin)
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .DisableAntiforgery();

        app.MapPost("/api/auth/logout", (Delegate)HandleLogout)
            .AllowAnonymous()
            .DisableAntiforgery();

        // GET для прямой навигации по ссылке-логауту в браузере.
        app.MapGet("/api/auth/logout", (Delegate)HandleLogout)
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleLogin(
        HttpContext httpContext,
        LoginRequest request,
        IAuthCommands authCommands)
    {
        // 1) Новый Session-id = новый UUID. Один и тот же для RPC и для cookie.
        var session = new Session(Guid.NewGuid().ToString("N"));

        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var ua = httpContext.Request.Headers.UserAgent.ToString();

        // 2) RPC-команда: сервер проверяет пароль и сохраняет CallerContext по Session.
        var command = new LoginCommand(session, request.Login, request.Password)
        {
            IpAddress = ip,
            UserAgent = string.IsNullOrEmpty(ua) ? null : ua,
            RequiredPermission = Permissions.AdminAccess,
        };
        var res = await authCommands.LoginAsync(command);

        if (!res.Status || res.Data is null)
        {
            return Results.Ok(new
            {
                Status = false,
                StatusCode = res.StatusCode,
                StatusMessage = res.StatusMessage
            });
        }

        // 3) Выписываем cookie с PrimarySid = session.Id (привязка cookie → Session).
        var user = res.Data;
        var authClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Surname, user.FullName),
            new(ClaimTypes.PrimarySid, session.Id),
        };
        if (user.Roles is { Count: > 0 })
            foreach (var role in user.Roles)
                authClaims.Add(new Claim(ClaimTypes.Role, role));

        var claimsIdentity = new ClaimsIdentity(
            authClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = true };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Results.Ok(new
        {
            Status = true,
            StatusCode = res.StatusCode,
            StatusMessage = "Вход успешно выполнен."
        });
    }

    private static async Task<IResult> HandleLogout(
        HttpContext httpContext,
        IAuthCommands authCommands)
    {
        // Достаём Session-id из cookie и снимаем CallerContext на сервере.
        var sid = httpContext.User.FindFirst(ClaimTypes.PrimarySid)?.Value;
        if (!string.IsNullOrEmpty(sid))
        {
            try
            {
                await authCommands.LogoutAsync(new LogoutCommand(new Session(sid)));
            }
            catch
            {
                // Логаут идемпотентен — даже если RPC недоступен, куки чистим.
            }
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        httpContext.Response.Cookies.Delete("ISC.Auth", new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
        });

        httpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        httpContext.Response.Headers.Pragma = "no-cache";

        return Results.Redirect("/login");
    }
}

public record LoginRequest(string Login, string Password);
public record LoginResponse(bool Success, string Message, int StatusCode);
