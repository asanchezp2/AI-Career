namespace FraudDetection.Api.Middleware;

/// <summary>
/// Adds basic security headers to all responses.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            // Swashbuckle serves inline bootstrap script and styles. Keep this
            // exception scoped to Swagger; the API retains its strict CSP.
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:";
        }
        else
        {
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
        }
        await _next(context);
    }
}
