using ActualLab.Rpc.Server;
using IST.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES ---
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddAppServices();

var app = builder.Build();

// --- PIPELINE ---
app.UseWebSockets();
app.UseRouting();


// Добавляем эти слои, чтобы контекст пользователя был доступен
app.UseAuthentication();
app.UseAuthorization();

app.MapRpcWebSocketServer();

app.Run();