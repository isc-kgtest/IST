using ActualLab.Fusion;
using ActualLab.Rpc;
using IST.Admin.Auth;
using IST.Contracts.Features.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Threading.RateLimiting;

namespace IST.Admin.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.AddCascadingAuthenticationState();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/api/auth/logout";
                options.AccessDeniedPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.Name = "ISC.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;

                options.Cookie.SecurePolicy = env.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
            options.AddPolicy("ContentEditor", policy => policy.RequireRole("admin", "editor"));
        });

        // HttpContext доступ для AuthenticationStateProvider
        services.AddHttpContextAccessor();

        // Кастомный AuthenticationStateProvider (читает claims из cookie)
        services.AddScoped<AuthenticationStateProvider, FusionAuthenticationStateProvider>();

        return services;
    }

    // Вынесли Rate Limiting в отдельный метод для чистоты
    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Auth endpoints: 10 requests per minute per IP
            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        var fusion = services.AddFusion();
        var rpc = services.AddRpc();

        var rpcUrl = config["RpcServer:Url"] ?? "ws://localhost:5000";

        rpc.AddWebSocketClient(rpcUrl);

        // RPC-прокси к IST.Server
        rpc.AddClient<IAuthQueries>();
        rpc.AddClient<IAuthCommands>();

        fusion.AddClient<IAuthQueries>();
        fusion.AddClient<IAuthCommands>();

        // Commander для диспатча команд
        services.AddCommander();

        return services;
    }
}