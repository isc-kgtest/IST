using IST.Admin.Auth;
using IST.Admin.Extensions;
using MudBlazor.Services;
var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<IST.Admin.Services.LanguageService>();

// 2. Custom Configurations
builder.Services.AddAuthenticationAndAuthorization(builder.Environment);
builder.Services.AddRateLimitingConfiguration();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// --- HTTP request pipeline ---
//app.UseEnhancedSecurityHeaders();

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

// Auth API endpoints (login/logout)
app.MapAuthEndpoints();

app.MapRazorComponents<IST.Admin.Shared.App>()
    .AddInteractiveServerRenderMode();

app.Run();







