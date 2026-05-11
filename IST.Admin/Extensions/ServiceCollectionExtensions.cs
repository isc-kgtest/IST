using ActualLab.Fusion;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Rpc;
using IST.Admin.Auth;
using IST.Contracts.Features.Audit;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Dictionaries;
using IST.Contracts.Features.Dictionaries.Commands;
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

        var rpcUrl = config["RpcServer:Url"] ?? "ws://localhost:5000";

        // WebSocket-клиент поднимаем через Fusion-ную RPC-шину (fusion.Rpc),
        // а не через отдельный services.AddRpc(). Иначе получаются две RPC-
        // инфраструктуры: одна без Fusion-перехватчика, и compute-клиенты
        // падают на fallback AutoInvalidationDelay (~1 с polling).
        fusion.Rpc.AddWebSocketClient(rpcUrl);

        // Клиент IAuth нужен для правильной работы Session/аутентификации
        fusion.AddAuthClient();

        // Клиенты Queries и Commands.
        fusion.AddClient<IAuthQueries>();
        fusion.AddClient<IAuthCommands>();
        
        fusion.AddClient<IDictionaryQueries>();
        fusion.AddClient<IDictionaryCommands>();

        fusion.AddClient<IAuditQueries>();

        // Blazor-интеграция Fusion (CircuitHub и т.п.) + AuthN на Blazor-стороне.
        fusion.AddBlazor();

        // Commander для диспатча команд
        services.AddCommander();

        return services;
    }
}