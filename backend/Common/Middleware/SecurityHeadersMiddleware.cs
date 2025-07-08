namespace AuthenticationApi.Common.Middleware;

/// <summary>
/// Middleware that adds security headers to all HTTP responses for ISO 27001 compliance.
/// Implements headers required by the technical specification including HSTS, CSP, X-Frame-Options, etc.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the SecurityHeadersMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Remove server header for security
        context.Response.Headers.Remove("Server");

        // Add security headers
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy - different for Swagger UI vs API
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            // More permissive CSP for Swagger UI functionality
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'";
        }
        else
        {
            // Strict CSP for API endpoints
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for adding SecurityHeadersMiddleware to the application pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds the SecurityHeadersMiddleware to the application's request pipeline.
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for method chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}