using ActualLab.CommandR;
using ActualLab.Fusion;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
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
                // ConfigureOperationLogReader — fallback-читатель для _Operations.
                // NpgsqlWatcher ловит события мгновенно через NOTIFY;
                // ридер нужен только как резервный механизм.
                operations.ConfigureOperationLogReader(_ => new()
                {
                    CheckPeriod = TimeSpan.FromMinutes(5),
                });

                // Корень проблемы: DbEventLogReader по умолчанию опрашивает "_Events" каждую секунду.
                // Если в таблице есть необработанные записи (State=0) — оставшиеся от старого
                // PresenceReporter или других сессий — Fusion видит их и инвалидирует
                // весь граф computed, что запускает SELECT users+roles каждую секунду.
                // Переводим его в режим редкой проверки — инвалидация будет приходить
                // через NpgsqlWatcher (мгновенно при реальном изменении ДБ).
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

        // 🔹 Регистрируем сервисы авторизации
        services.AddAuthorization();

        // 🔹 Core builders — ОДИН РАЗ
        var fusion = services.AddFusion();
        var commander = services.AddCommander();
        var rpc = services.AddRpc();       

        // Серверный реестр активных сессий: UserId -> CallerContext (роли + permissions).
        // Заполняется в LoginAsync, чистится в LogoutAsync / удалении пользователя.
        services.AddSingleton<ICurrentUserStore, InMemoryCurrentUserStore>();

        // Журнал событий безопасности.
        services.AddSingleton<IAuditService, AuditService>();

        // Fusion Blazor + Auth
        // ВАЖНО: AddPresenceReporter() НЕ вызываем на сервере!
        // PresenceReporter пишет в _Operations каждую секунду для каждой
        // подключённой сессии. NpgsqlWatcher ловит эти записи и инвалидирует
        // весь граф Computed — именно это вызывало цикличный SELECT каждую секунду.
        // PresenceReporter нужен только на клиенте (IST.Admin).
        fusion.AddBlazor()
            .AddAuthentication();

        fusion.AddClient<IAuth>(); // IAuth = a client of backend's IAuth
        fusion.Configure<IAuth>().IsServer(typeof(IAuth)).HasClient(); // Expose IAuth (a client) via RPC
        // RPC transport
        rpc.AddWebSocketServer();

        // Queries (Compute)
        fusion.AddService<IAuthQueries, AuthQueries>(RpcServiceMode.Server);
        fusion.AddService<IDictionaryQueries, DictionaryQueries>(RpcServiceMode.Server);
        fusion.AddService<IAuditQueries, AuditQueries>(RpcServiceMode.Server);
        fusion.AddService<IOrganizationQueries, OrganizationQueries>(RpcServiceMode.Server);

        // Commands
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