using ActualLab.CommandR;
using ActualLab.Fusion;
using ActualLab.Rpc;
using ActualLab.Fusion.Authentication;
using IST.Services.Data;
using IST.Services.Features.Auth;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DB Context for PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=ist_db;Username=postgres;Password=postgres"));

// Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("Role", "Admin"));
    options.AddPolicy("ClientOnly", policy => policy.RequireClaim("Role", "Client"));
});

var fusion = builder.Services.AddFusion();

// 1. Services
builder.Services.AddScoped<AuthHandlers>();
// TODO: commandR.AddHandlers<AuthHandlers>();

// 2. Rpc WebSocket Server setup
// TODO: builder.Services.AddRpc().AddWebSocketServer();

var app = builder.Build();

app.UseWebSockets();

// Maps Rpc WebSocket Endpoints that allow the WASM client to connect
// TODO: app.MapRpcWebSocketServer();

app.Run();
