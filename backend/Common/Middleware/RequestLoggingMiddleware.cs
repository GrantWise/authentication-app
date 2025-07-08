using Serilog.Context;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using AuthenticationApi.Common.Services;

namespace AuthenticationApi.Common.Middleware;

/// <summary>
/// Middleware for comprehensive request logging with business context and performance tracking.
/// Captures user context, request/response details, and performance metrics for monitoring and audit purposes.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly MetricsService _metricsService;
    private readonly IConfiguration _configuration;
    private readonly bool _logRequestBodies;
    private readonly bool _logResponseBodies;
    private readonly long _slowRequestThresholdMs;

    /// <summary>
    /// Initializes a new instance of the RequestLoggingMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">Logger for request logging</param>
    /// <param name="metricsService">Service for recording metrics</param>
    /// <param name="configuration">Application configuration</param>
    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        MetricsService metricsService,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _metricsService = metricsService;
        _configuration = configuration;
        
        // Configuration options
        _logRequestBodies = configuration.GetValue<bool>("Logging:RequestBodies", false);
        _logResponseBodies = configuration.GetValue<bool>("Logging:ResponseBodies", false);
        _slowRequestThresholdMs = configuration.GetValue<long>("Logging:SlowRequestThresholdMs", 1000);
    }

    /// <summary>
    /// Processes the HTTP request and logs comprehensive request/response information.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.GetCorrelationId() ?? Guid.NewGuid().ToString();
        
        // Capture request details
        var requestInfo = await CaptureRequestInfo(context);
        
        // Log request start
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("UserId", requestInfo.UserId))
        using (LogContext.PushProperty("Username", requestInfo.Username))
        using (LogContext.PushProperty("SessionId", requestInfo.SessionId))
        using (LogContext.PushProperty("ClientIP", requestInfo.ClientIP))
        using (LogContext.PushProperty("UserAgent", requestInfo.UserAgent))
        {
            _logger.LogInformation("Request started: {Method} {Path} from {ClientIP}",
                requestInfo.Method, requestInfo.Path, requestInfo.ClientIP);

            // Capture original response stream
            var originalResponseStream = context.Response.Body;
            using var responseStreamCapture = new MemoryStream();
            context.Response.Body = responseStreamCapture;

            Exception? exception = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                // Restore original response stream
                context.Response.Body = originalResponseStream;
                
                // Copy captured response back to original stream
                if (responseStreamCapture.Length > 0)
                {
                    responseStreamCapture.Seek(0, SeekOrigin.Begin);
                    await responseStreamCapture.CopyToAsync(originalResponseStream);
                }

                // Log request completion
                await LogRequestCompletion(context, requestInfo, stopwatch.Elapsed, exception, responseStreamCapture);
                
                // Record metrics
                RecordMetrics(requestInfo, context.Response.StatusCode, stopwatch.Elapsed);
            }
        }
    }

    /// <summary>
    /// Captures detailed request information including user context.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>Request information object</returns>
    private async Task<RequestInfo> CaptureRequestInfo(HttpContext context)
    {
        var requestInfo = new RequestInfo
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            ClientIP = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            ContentType = context.Request.ContentType,
            ContentLength = context.Request.ContentLength,
            Timestamp = DateTime.UtcNow
        };

        // Extract user context from claims
        if (context.User.Identity?.IsAuthenticated == true)
        {
            requestInfo.UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            requestInfo.Username = context.User.FindFirst(ClaimTypes.Name)?.Value;
            requestInfo.SessionId = context.User.FindFirst("SessionId")?.Value;
        }

        // Capture request body if enabled (be careful with sensitive data)
        if (_logRequestBodies && context.Request.ContentLength > 0 && 
            ShouldLogBody(context.Request.ContentType))
        {
            context.Request.EnableBuffering();
            var requestBody = await ReadRequestBody(context.Request);
            requestInfo.RequestBody = requestBody;
            context.Request.Body.Position = 0;
        }

        return requestInfo;
    }

    /// <summary>
    /// Logs the completion of the request with performance and business context.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="requestInfo">Request information</param>
    /// <param name="duration">Request duration</param>
    /// <param name="exception">Exception if one occurred</param>
    /// <param name="responseStream">Response stream for capturing response body</param>
    private async Task LogRequestCompletion(HttpContext context, RequestInfo requestInfo, 
        TimeSpan duration, Exception? exception, MemoryStream responseStream)
    {
        var statusCode = context.Response.StatusCode;
        var responseSize = responseStream.Length;
        
        // Determine log level based on status code and duration
        var logLevel = DetermineLogLevel(statusCode, duration, exception);
        
        // Capture response body if enabled
        string? responseBody = null;
        if (_logResponseBodies && responseSize > 0 && 
            ShouldLogBody(context.Response.ContentType))
        {
            responseBody = await ReadResponseBody(responseStream);
        }

        // Create structured log entry
        var logData = new
        {
            RequestId = context.GetCorrelationId(),
            Method = requestInfo.Method,
            Path = requestInfo.Path,
            QueryString = requestInfo.QueryString,
            StatusCode = statusCode,
            Duration = duration.TotalMilliseconds,
            RequestSize = requestInfo.ContentLength,
            ResponseSize = responseSize,
            ClientIP = requestInfo.ClientIP,
            UserAgent = requestInfo.UserAgent,
            UserId = requestInfo.UserId,
            Username = requestInfo.Username,
            SessionId = requestInfo.SessionId,
            Exception = exception?.ToString(),
            RequestBody = requestInfo.RequestBody,
            ResponseBody = responseBody,
            IsSlowRequest = duration.TotalMilliseconds > _slowRequestThresholdMs
        };

        // Log with appropriate level
        switch (logLevel)
        {
            case LogLevel.Information:
                _logger.LogInformation("Request completed: {Method} {Path} {StatusCode} in {Duration}ms",
                    requestInfo.Method, requestInfo.Path, statusCode, duration.TotalMilliseconds);
                break;
            case LogLevel.Warning:
                _logger.LogWarning("Slow request: {Method} {Path} {StatusCode} in {Duration}ms",
                    requestInfo.Method, requestInfo.Path, statusCode, duration.TotalMilliseconds);
                break;
            case LogLevel.Error:
                _logger.LogError(exception, "Request failed: {Method} {Path} {StatusCode} in {Duration}ms",
                    requestInfo.Method, requestInfo.Path, statusCode, duration.TotalMilliseconds);
                break;
        }

        // Log detailed request data at debug level
        _logger.LogDebug("Request details: {@RequestData}", logData);
    }

    /// <summary>
    /// Records metrics for the request.
    /// </summary>
    /// <param name="requestInfo">Request information</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="duration">Request duration</param>
    private void RecordMetrics(RequestInfo requestInfo, int statusCode, TimeSpan duration)
    {
        try
        {
            // Record API request metrics
            _metricsService.RecordApiRequest(requestInfo.Method, requestInfo.Path, statusCode, duration);
            
            // Record authentication-specific metrics
            if (requestInfo.Path.StartsWith("/api/auth"))
            {
                var success = statusCode < 400;
                var endpoint = ExtractAuthEndpoint(requestInfo.Path);
                _metricsService.RecordAuthenticationDuration(endpoint, success, duration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record metrics for request {Method} {Path}",
                requestInfo.Method, requestInfo.Path);
        }
    }

    /// <summary>
    /// Determines the appropriate log level based on response status and duration.
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="duration">Request duration</param>
    /// <param name="exception">Exception if one occurred</param>
    /// <returns>Log level</returns>
    private LogLevel DetermineLogLevel(int statusCode, TimeSpan duration, Exception? exception)
    {
        if (exception != null || statusCode >= 500)
            return LogLevel.Error;
        
        if (duration.TotalMilliseconds > _slowRequestThresholdMs)
            return LogLevel.Warning;
        
        return LogLevel.Information;
    }

    /// <summary>
    /// Determines if the request/response body should be logged based on content type.
    /// </summary>
    /// <param name="contentType">Content type header</param>
    /// <returns>True if body should be logged</returns>
    private bool ShouldLogBody(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;
        
        // Only log text-based content types
        return contentType.StartsWith("application/json") ||
               contentType.StartsWith("application/xml") ||
               contentType.StartsWith("text/");
    }

    /// <summary>
    /// Reads the request body as a string.
    /// </summary>
    /// <param name="request">HTTP request</param>
    /// <returns>Request body as string</returns>
    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        return body;
    }

    /// <summary>
    /// Reads the response body from the captured stream.
    /// </summary>
    /// <param name="responseStream">Response stream</param>
    /// <returns>Response body as string</returns>
    private async Task<string> ReadResponseBody(MemoryStream responseStream)
    {
        responseStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseStream, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        return body;
    }

    /// <summary>
    /// Extracts the authentication endpoint name from the path.
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Authentication endpoint name</returns>
    private string ExtractAuthEndpoint(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 2 ? segments[2] : "unknown";
    }
}

/// <summary>
/// Request information captured for logging.
/// </summary>
public class RequestInfo
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string? ClientIP { get; set; }
    public string? UserAgent { get; set; }
    public string? ContentType { get; set; }
    public long? ContentLength { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? SessionId { get; set; }
    public string? RequestBody { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Extension methods for RequestLoggingMiddleware.
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds the RequestLoggingMiddleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for method chaining</returns>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}