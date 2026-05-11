using ActualLab.Rpc.Server;
using IST.Server.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES ---
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddAppServices();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IST.Infrastructure.Data.AppDbContext>();
    // Ensure database is created/migrated (optional but good practice for dev)
    context.Database.Migrate();

    // Seed permissions + admin role bindings (идемпотентно).
    await IST.Infrastructure.Data.SecuritySeeder.SeedAsync(context);

    // Seed NSI
    await IST.Infrastructure.Data.DictionarySeeder.SeedNsiDictionariesAsync(context);
}

// --- PIPELINE ---
app.UseWebSockets();
app.UseRouting();


// Добавляем эти слои, чтобы контекст пользователя был доступен
app.UseAuthentication();
app.UseAuthorization();

app.MapRpcWebSocketServer();

app.Run();