using ActualLab.Fusion;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Rpc;
using IST.Admin.Auth;
using IST.Contracts.Features.Audit;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Dictionaries;
using IST.Contracts.Features.Dictionaries.Commands;
using IST.Contracts.Features.Organization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;

namespace IST.Admin.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationAndAuthorization(
        this IServiceCollection services, IWebHostEnvironment env)
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
                options.Cookie.SameSite = SameSiteMode.Lax;

                options.Cookie.SecurePolicy = env.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
            options.AddPolicy("ContentEditor", policy => policy.RequireRole("admin", "editor"));
        });

        services.AddHttpContextAccessor();

        // Встроенного Blazor-провайдера достаточно — он сам читает claims из cookie
        // в circuit. Кастомный, читающий IHttpContextAccessor, ломается в circuit'е
        // (HttpContext там null).

        return services;
    }

    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, IConfiguration config)
    {
        var fusion = services.AddFusion();

        var rpcUrl = config["RpcServer:Url"] ?? "ws://localhost:5000";
        fusion.Rpc.AddWebSocketClient(rpcUrl);

        // КРИТИЧНО: AddAuthClient регистрирует Fusion ISessionResolver. Без него
        // RPC-pipeline подмешивает в каждый вызов Session.Default или случайную
        // "~"-сессию, и сервер не находит CallerContext в store по той сессии,
        // которую мы кладём в cookie при логине.
        fusion.AddAuthClient();

        // Fusion-клиенты для всех RPC-сервисов.
        fusion.AddClient<IUserPresence>();

        fusion.AddClient<IAuthQueries>();
        fusion.AddClient<IAuthCommands>();
        fusion.AddClient<IAuditQueries>();
        fusion.AddClient<IDictionaryQueries>();
        fusion.AddClient<IDictionaryCommands>();
        fusion.AddClient<IOrganizationQueries>();
        fusion.AddClient<IOrganizationCommands>();

        // Blazor-интеграция Fusion (CircuitHub, ComputedState и т.п.) — БЕЗ AddAuthentication.
        fusion.AddBlazor();

        services.AddCommander();

        // Поставщик Session для компонентов: читает PrimarySid из cookie.
        services.AddScoped<SessionAccessor>();

        return services;
    }
}
