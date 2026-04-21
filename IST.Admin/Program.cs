using ActualLab.Fusion;
using ActualLab.Fusion.Authentication;
using ActualLab.Rpc;
using ISC.Academy.WEB.Extensions;
using IST.Admin.Extensions;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// 2. Custom Configurations
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddAuthenticationAndAuthorization(builder.Environment);
builder.Services.AddRateLimitingConfiguration();
builder.Services.AddApplicationServices();

// 3. Configure Fusion as an RPC Client
var fusion = builder.Services.AddFusion();

// Достаем URL из appsettings.json, а если его нет - используем fallback
var rpcUrl = builder.Configuration["RpcServer:Url"] ?? "ws://localhost:5000";

builder.Services.AddRpc()
    .AddWebSocketClient(rpcUrl);

// Configure ActualLab Auth client
fusion.AddAuthClient();

var app = builder.Build();

// --- HTTP request pipeline ---
app.UseEnhancedSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Хорошей практикой также является добавление HSTS в продакшене
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting(); // Явно добавляем роутинг

app.UseRateLimiter(); // ВАЖНО: Добавлено Middleware для лимитирования запросов!

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<IST.Admin.Shared.App>()
    .AddInteractiveServerRenderMode();

app.Run();