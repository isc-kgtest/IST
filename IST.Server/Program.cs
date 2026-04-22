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

app.MapRpcWebSocketServer();

app.Run();