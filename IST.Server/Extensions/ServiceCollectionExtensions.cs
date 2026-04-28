using ActualLab.CommandR;
using ActualLab.Fusion;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Fusion.EntityFramework.Npgsql;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Dictionaries;
using IST.Contracts.Features.Nsi;
using IST.Infrastructure.Data;
using IST.Services.Features.Auth;
using IST.Services.Features.Dictionaries;
using IST.Services.Features.Nsi;
using Mapster;
using MapsterMapper;
using IST.Shared.DTOs;

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

        // EF Core интеграция
        services.AddDbContextServices<AppDbContext>(db =>
        {
            db.AddOperations(operations =>
            {
                operations.ConfigureOperationLogReader(_ => new()
                {
                    CheckPeriod = TimeSpan.FromMinutes(5),
                });

                operations.ConfigureEventLogReader(_ => new()
                {
                    CheckPeriod = TimeSpan.FromMinutes(5),
                });

                operations.AddNpgsqlOperationLogWatcher();
            });
        });

        return services;
    }

    // ЕДИНАЯ ТОЧКА СБОРКИ ВСЕГО
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {

        services.AddAuthentication();
        services.AddAuthorization();

        var fusion = services.AddFusion();
        var commander = services.AddCommander();
        var rpc = services.AddRpc();       

        fusion.AddBlazor()
            .AddAuthentication();

        fusion.AddClient<IAuth>(); // IAuth = a client of backend's IAuth
        fusion.Configure<IAuth>().IsServer(typeof(IAuth)).HasClient(); // Expose IAuth (a client) via RPC
        rpc.AddWebSocketServer();

        // Auth
        fusion.AddService<IAuthQueries, AuthQueries>(RpcServiceMode.Server);
        fusion.AddService<IAuthCommands, AuthCommands>(RpcServiceMode.Server);
        commander.AddHandlers<AuthCommands>();

        // NSI
        fusion.AddService<INsiQueries, NsiQueries>(RpcServiceMode.Server);
        fusion.AddService<INsiCommands, NsiCommands>(RpcServiceMode.Server);
        commander.AddHandlers<NsiCommands>();

        // Dictionaries
        fusion.AddService<IDictionaryQueries, DictionaryQueries>(RpcServiceMode.Server);
        fusion.AddService<IDictionaryCommands, DictionaryCommands>(RpcServiceMode.Server);
        commander.AddHandlers<DictionaryCommands>();

        services.AddMapster();

        return services;
    }

    public static IServiceCollection AddMapster(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(IMappingMarker).Assembly);
        services.AddSingleton(config);
        services.AddSingleton<IMapper, ServiceMapper>();
        return services;
    }
}
