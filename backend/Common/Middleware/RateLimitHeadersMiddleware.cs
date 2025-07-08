using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace AuthenticationApi.Common.Middleware;

/// <summary>
/// Middleware to add rate limiting headers to all HTTP responses.
/// Provides X-RateLimit-* headers for API clients to understand rate limiting status.
/// </summary>
public class RateLimitHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitHeadersMiddleware> _logger;

    public RateLimitHeadersMiddleware(RequestDelegate next, ILogger<RateLimitHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Call the next middleware in the pipeline
        await _next(context);

        // Add rate limit headers to the response
        AddRateLimitHeaders(context);
    }

    private void AddRateLimitHeaders(HttpContext context)
    {
        try
        {
            // In .NET 8, rate limiting doesn't expose state through features
            // Instead, we'll add headers based on the endpoint and response status
            var rateLimitPolicy = GetRateLimitPolicyForEndpoint(context.Request.Path);

            if (rateLimitPolicy != null)
            {
                // Add standard rate limit headers
                context.Response.Headers.Add("X-RateLimit-Policy", rateLimitPolicy.PolicyName);
                context.Response.Headers.Add("X-RateLimit-Limit", rateLimitPolicy.Limit.ToString());
                
                if (rateLimitPolicy.Window.HasValue)
                {
                    var resetTime = DateTimeOffset.UtcNow.Add(rateLimitPolicy.Window.Value);
                    context.Response.Headers.Add("X-RateLimit-Reset", resetTime.ToUnixTimeSeconds().ToString());
                    context.Response.Headers.Add("X-RateLimit-Window", ((int)rateLimitPolicy.Window.Value.TotalSeconds).ToString());
                }

                // Add remaining attempts (this is estimated based on policy)
                var remaining = EstimateRemainingAttempts(context, rateLimitPolicy);
                context.Response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());
            }

            // Always add correlation ID header if available
            if (context.Items.TryGetValue("CorrelationId", out var correlationId))
            {
                context.Response.Headers.Add("X-Correlation-Id", correlationId?.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add rate limit headers");
            // Don't fail the request if we can't add headers
        }
    }

    private RateLimitPolicyInfo? GetRateLimitPolicyForEndpoint(string path)
    {
        return path.ToLowerInvariant() switch
        {
            "/api/auth/login" => new RateLimitPolicyInfo("LoginPolicy", 5, TimeSpan.FromMinutes(15)),
            "/api/auth/refresh" => new RateLimitPolicyInfo("RefreshPolicy", 10, TimeSpan.FromMinutes(1)),
            "/api/auth/mfa/verify" => new RateLimitPolicyInfo("MfaPolicy", 5, TimeSpan.FromMinutes(5)),
            _ when path.StartsWith("/api/") => new RateLimitPolicyInfo("GeneralPolicy", 100, TimeSpan.FromMinutes(1)),
            _ => null
        };
    }

    private int EstimateRemainingAttempts(HttpContext context, RateLimitPolicyInfo policy)
    {
        // This is a simplified estimation since we don't have direct access to the rate limiter state
        // In a production environment, you might want to integrate more closely with the rate limiter
        // or maintain your own tracking for more accurate remaining counts
        
        if (context.Response.StatusCode == 429)
        {
            return 0; // Rate limit exceeded
        }

        // For non-rate-limited requests, return a reasonable estimate
        // This could be enhanced with actual rate limiter integration
        return Math.Max(0, policy.Limit - 1);
    }

    private class RateLimitPolicyInfo
    {
        public string PolicyName { get; }
        public int Limit { get; }
        public TimeSpan? Window { get; }

        public RateLimitPolicyInfo(string policyName, int limit, TimeSpan? window = null)
        {
            PolicyName = policyName;
            Limit = limit;
            Window = window;
        }
    }
}

/// <summary>
/// Extension methods for configuring the rate limit headers middleware.
/// </summary>
public static class RateLimitHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds the rate limit headers middleware to the application pipeline.
    /// Should be added after UseRateLimiter() but before UseAuthentication().
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseRateLimitHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitHeadersMiddleware>();
    }
}