using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace IST.Admin.Auth;

/// <summary>
/// AuthenticationStateProvider для Blazor Server.
/// Читает ClaimsPrincipal из HttpContext (cookie middleware).
/// После login/logout обновляется через forceLoad навигацию.
/// </summary>
public class FusionAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FusionAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User
                   ?? new ClaimsPrincipal(new ClaimsIdentity());

        return Task.FromResult(new AuthenticationState(user));
    }
}
