using ActualLab.CommandR;
using ActualLab.Fusion;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using IST.Contracts.Features.Auth;
using IST.Infrastructure.Data;
using IST.Services.Features.Auth;

namespace IST.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    // ЕДИНАЯ ТОЧКА СБОРКИ ВСЕГО
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {

        services.AddAuthentication();

        // 🔹 Регистрируем сервисы авторизации
        services.AddAuthorization();

        // 🔹 Core builders — ОДИН РАЗ
        var fusion = services.AddFusion();
        var commander = services.AddCommander();
        var rpc = services.AddRpc();

        // ---------------- INFRASTRUCTURE ----------------

        // EF Core интеграция
        services.AddDbContextServices<AppDbContext>();

        // Fusion Blazor + Auth
        fusion.AddBlazor()
            .AddAuthentication()
            .AddPresenceReporter();

        fusion.AddClient<IAuth>(); // IAuth = a client of backend's IAuth
        fusion.Configure<IAuth>().IsServer(typeof(IAuth)).HasClient(); // Expose IAuth (a client) via RPC
        // RPC transport
        rpc.AddWebSocketServer();

        // Queries (Compute)
        fusion.AddComputeService<IAuthQueries, AuthQueries>();

        // Commands
        services.AddScoped<IAuthCommands, AuthCommands>();
        commander.AddHandlers<AuthCommands>();

        // RPC endpoints
        rpc.AddServer<IAuthQueries, AuthQueries>();
        rpc.AddServer<IAuthCommands, AuthCommands>();

        return services;
    }
}