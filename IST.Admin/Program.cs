using ActualLab.Fusion;
using ActualLab.Rpc;
using ActualLab.Fusion.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices();

// 1. Configure standard Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });

// 2. Configure Fusion as an RPC Client to connect to the Server
var fusion = builder.Services.AddFusion();
// Note: Depending on whether Admin is a connected client or a direct actor, we map Rpc. 
// Assuming it uses Rpc over WebSocket to talk to IST.Server
builder.Services.AddRpc().AddWebSocketClient("ws://localhost:5000"); // Update with actual IST.Server URL

// Configure ActualLab Auth client
fusion.AddAuthClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<IST.Admin.Shared.App>()
    .AddInteractiveServerRenderMode();

app.Run();
