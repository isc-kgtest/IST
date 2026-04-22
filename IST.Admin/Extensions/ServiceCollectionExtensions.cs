using ActualLab.Fusion;
using ActualLab.Rpc;
using IST.Contracts.Features.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
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
                options.LogoutPath = "/logout";
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
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("ContentEditor", policy => policy.RequireRole("Admin", "Editor"));
        });

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

        // 🔥 только прокси
        rpc.AddClient<IAuthQueries>();

        // 🔥 команды
        services.AddCommander();

        return services;
    }
}