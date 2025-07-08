using Serilog.Context;

namespace AuthenticationApi.Common.Middleware;

/// <summary>
/// Middleware that generates and manages correlation IDs for request tracking and audit trails.
/// Enables full request tracing across all system components for debugging and compliance.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    /// <summary>
    /// Initializes a new instance of the CorrelationIdMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and establishes correlation ID context.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = GetOrGenerateCorrelationId(context);

        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Store correlation ID in HttpContext for access by other components
        context.Items["CorrelationId"] = correlationId;

        // Add correlation ID to Serilog logging context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Gets the correlation ID from request headers or generates a new one.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The correlation ID for this request</returns>
    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers (for forwarded requests)
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID if not provided
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension methods for adding CorrelationIdMiddleware to the application pipeline.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the CorrelationIdMiddleware to the application's request pipeline.
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for method chaining</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Gets the correlation ID for the current request from HttpContext.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The correlation ID, or null if not available</returns>
    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items["CorrelationId"] as string;
    }
}