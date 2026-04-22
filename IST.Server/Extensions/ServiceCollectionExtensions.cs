using ActualLab.CommandR;
using ActualLab.Fusion;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using IST.Contracts.Features.Auth;
using IST.Infrastructure.AppDbContext;
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

    // ⚙️ ЕДИНАЯ ТОЧКА СБОРКИ ВСЕГО
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
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

        // RPC transport
        rpc.AddWebSocketServer();

        // Queries (Compute)
        fusion.AddComputeService<IAuthQueries, AuthQueries>();

        // Commands
        services.AddScoped<AuthCommands>();
        commander.AddHandlers<AuthCommands>();

        // RPC endpoints
        rpc.AddServer<IAuthQueries>();

        return services;
    }
}