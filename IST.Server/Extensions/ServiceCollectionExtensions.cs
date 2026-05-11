using ActualLab.CommandR;
using ActualLab.Fusion;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Fusion.EntityFramework.Npgsql;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using IST.Contracts.Features.Audit;
using IST.Contracts.Features.Auth;
using IST.Contracts.Features.Dictionaries;
using IST.Contracts.Features.Organization;
using IST.Infrastructure.Data;
using IST.Services.Features.Audit;
using IST.Services.Features.Auth;
using IST.Services.Features.Auth.Authentication;
using IST.Services.Features.Dictionaries;
using IST.Services.Features.Organization;
using IST.Shared.DTOs;
using Mapster;
using MapsterMapper;

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

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Минимальная инфраструктура для UseAuthentication/UseAuthorization (RPC-сервер).
        services.AddAuthentication();
        services.AddAuthorization();

        var fusion = services.AddFusion();
        var commander = services.AddCommander();
        var rpc = services.AddRpc();

        // RPC-транспорт.
        rpc.AddWebSocketServer();

        // Серверный реестр Session → CallerContext (in-memory).
        // Заполняется в LoginAsync, чистится в LogoutAsync / удалении / смене ролей.
        // Сам резолвит IUserPresence лениво через IServiceProvider — никаких циклов в DI.
        services.AddSingleton<ICurrentUserStore, InMemoryCurrentUserStore>();

        // Реактивный фасад для отслеживания сессии (клиенты подписываются;
        // при удалении сессии — инвалидация → клиент мгновенно видит logout).
        fusion.AddService<IUserPresence, UserPresence>(RpcServiceMode.Server);

        // Журнал событий безопасности.
        services.AddSingleton<IAuditService, AuditService>();

        // Queries (Compute).
        fusion.AddService<IAuthQueries, AuthQueries>(RpcServiceMode.Server);
        fusion.AddService<IDictionaryQueries, DictionaryQueries>(RpcServiceMode.Server);
        fusion.AddService<IAuditQueries, AuditQueries>(RpcServiceMode.Server);
        fusion.AddService<IOrganizationQueries, OrganizationQueries>(RpcServiceMode.Server);

        // Commands.
        fusion.AddService<IAuthCommands, AuthCommands>(RpcServiceMode.Server);
        commander.AddHandlers<AuthCommands>();

        fusion.AddService<IDictionaryCommands, DictionaryCommands>(RpcServiceMode.Server);
        commander.AddHandlers<DictionaryCommands>();

        fusion.AddService<IOrganizationCommands, OrganizationCommands>(RpcServiceMode.Server);
        commander.AddHandlers<OrganizationCommands>();

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
