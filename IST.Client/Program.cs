using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ActualLab.Fusion;
using ActualLab.Rpc;
using ActualLab.Fusion.Authentication;

using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<IST.Client.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var fusion = builder.Services.AddFusion();

// 1. Connection to the Server via ActualLab RPC WebSockets
builder.Services.AddRpc().AddWebSocketClient("ws://localhost:5000"); // Update with actual IST.Server URL

// MudBlazor
builder.Services.AddMudServices();

// 2. Set up ActualLab Auth Client
fusion.AddAuthClient();

await builder.Build().RunAsync();
