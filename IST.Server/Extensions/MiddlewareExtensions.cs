namespace IST.Server.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseEnhancedSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

            // Strict-Transport-Security (HSTS) - Enforce HTTPS for 1 year, include subdomains, allow preload
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

            // Content-Security-Policy (CSP) - Tightened
            // Note: Blazor Server requires 'unsafe-inline' and 'unsafe-eval' for core functionality
            var csp = "default-src 'self'; " +
                      "base-uri 'self'; " +
                      "frame-src 'self' https://www.google.com https://maps.google.com; " +
                      "frame-ancestors 'none'; " +
                      "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://unpkg.com https://cdn.jsdelivr.net; " +
                      "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                      "font-src 'self' https://fonts.gstatic.com data:; " +
                      "img-src 'self' data: https://isc.mtaxi.kg blob:; " +
                      "connect-src 'self' ws: wss: https://cdn.jsdelivr.net;";

            context.Response.Headers["Content-Security-Policy"] = csp;

            await next();
        });

        return app;
    }
}
