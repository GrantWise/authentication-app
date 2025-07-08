using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Entities;
using AuthenticationApi.Common.Middleware;
using Serilog.Context;
using System.Text.Json;
using System.Security.Claims;

namespace AuthenticationApi.Common.Services;

/// <summary>
/// Enhanced audit service that provides rich business context logging and compliance tracking.
/// Extends the base audit service with correlation IDs, user context, and performance metrics.
/// </summary>
public class EnhancedAuditService : IAuditService
{
    private readonly IAuditService _baseAuditService;
    private readonly MetricsService _metricsService;
    private readonly ILogger<EnhancedAuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the EnhancedAuditService class.
    /// </summary>
    /// <param name="baseAuditService">Base audit service for database operations</param>
    /// <param name="metricsService">Metrics service for recording audit events</param>
    /// <param name="logger">Logger for service operations</param>
    /// <param name="httpContextAccessor">HTTP context accessor for request context</param>
    public EnhancedAuditService(
        IAuditService baseAuditService,
        MetricsService metricsService,
        ILogger<EnhancedAuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _baseAuditService = baseAuditService;
        _metricsService = metricsService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Logs an audit event with enhanced context including correlation ID, user information, and performance metrics.
    /// </summary>
    /// <param name="eventType">Type of event being logged</param>
    /// <param name="userId">User ID (optional)</param>
    /// <param name="username">Username (optional)</param>
    /// <param name="ipAddress">IP address (optional)</param>
    /// <param name="userAgent">User agent (optional)</param>
    /// <param name="details">Additional event details (optional)</param>
    /// <returns>Task representing the async operation</returns>
    public async Task LogEventAsync(string eventType, Guid? userId = null, string? username = null, 
        string? ipAddress = null, string? userAgent = null, string? details = null)
    {
        try
        {
            // Get enhanced context from HTTP request
            var enhancedContext = GetEnhancedContext(userId, username, ipAddress);
            
            // Create enriched details with all context
            var enrichedDetails = CreateEnrichedDetails(details, enhancedContext);
            
            // Log to database using base service
            await _baseAuditService.LogEventAsync(
                eventType, 
                enhancedContext.UserId, 
                enhancedContext.Username, 
                enhancedContext.IpAddress, 
                userAgent ?? enhancedContext.UserAgent,
                enrichedDetails);

            // Log to structured logging with rich context
            LogToStructuredLogging(eventType, enhancedContext, enrichedDetails);
            
            // Record metrics based on event type
            RecordEventMetrics(eventType, enhancedContext);
            
            _logger.LogDebug("Enhanced audit event logged: {EventType} for user {Username}", 
                eventType, enhancedContext.Username ?? "anonymous");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging enhanced audit event: {EventType}", eventType);
            
            // Still attempt to log with base service as fallback
            try
            {
                await _baseAuditService.LogEventAsync(eventType, userId, username, ipAddress, userAgent, details);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback audit logging also failed for event: {EventType}", eventType);
            }
        }
    }

    /// <summary>
    /// Logs a security event with additional security context and alerting.
    /// </summary>
    /// <param name="eventType">Type of security event</param>
    /// <param name="severity">Security event severity (Low, Medium, High, Critical)</param>
    /// <param name="userId">User ID (optional)</param>
    /// <param name="username">Username (optional)</param>
    /// <param name="ipAddress">IP address (optional)</param>
    /// <param name="details">Additional event details (optional)</param>
    /// <param name="sessionId">Session ID (optional)</param>
    /// <returns>Task representing the async operation</returns>
    public async Task LogSecurityEventAsync(string eventType, string severity, Guid? userId = null, 
        string? username = null, string? ipAddress = null, string? details = null, string? sessionId = null)
    {
        try
        {
            // Get enhanced context
            var enhancedContext = GetEnhancedContext(userId, username, ipAddress, sessionId);
            
            // Create security-specific details
            var securityDetails = CreateSecurityDetails(details, severity, enhancedContext);
            
            // Log as regular audit event with security prefix
            await LogEventAsync($"SECURITY_{eventType}", userId, username, ipAddress, null, securityDetails);
            
            // Additional security logging
            LogSecurityEvent(eventType, severity, enhancedContext, securityDetails);
            
            _logger.LogInformation("Security event logged: {EventType} with severity {Severity}", 
                eventType, severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event: {EventType}", eventType);
        }
    }

    /// <summary>
    /// Logs a performance event with timing and resource usage information.
    /// </summary>
    /// <param name="operation">Operation name</param>
    /// <param name="duration">Operation duration</param>
    /// <param name="success">Whether the operation was successful</param>
    /// <param name="resourcesUsed">Resources used during operation (optional)</param>
    /// <param name="userId">User ID (optional)</param>
    /// <param name="additionalContext">Additional context information (optional)</param>
    /// <returns>Task representing the async operation</returns>
    public async Task LogPerformanceEventAsync(string operation, TimeSpan duration, bool success, 
        Dictionary<string, object>? resourcesUsed = null, Guid? userId = null, 
        Dictionary<string, object>? additionalContext = null)
    {
        try
        {
            // Get enhanced context
            var enhancedContext = GetEnhancedContext(userId, duration: duration);
            
            // Create performance details
            var performanceDetails = CreatePerformanceDetails(operation, duration, success, resourcesUsed, additionalContext);
            
            // Log as performance event
            await LogEventAsync($"PERFORMANCE_{operation}", userId, null, null, null, performanceDetails);
            
            // Record performance metrics
            RecordPerformanceMetrics(operation, duration, success);
            
            _logger.LogDebug("Performance event logged: {Operation} took {Duration}ms, Success: {Success}", 
                operation, duration.TotalMilliseconds, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging performance event: {Operation}", operation);
        }
    }

    /// <summary>
    /// Gets enhanced context from HTTP request and claims.
    /// </summary>
    /// <param name="userId">Provided user ID</param>
    /// <param name="username">Provided username</param>
    /// <param name="ipAddress">Provided IP address</param>
    /// <param name="sessionId">Provided session ID</param>
    /// <param name="duration">Operation duration</param>
    /// <returns>Enhanced context information</returns>
    private EnhancedAuditContext GetEnhancedContext(Guid? userId = null, string? username = null, 
        string? ipAddress = null, string? sessionId = null, TimeSpan? duration = null)
    {
        var context = new EnhancedAuditContext
        {
            Timestamp = DateTime.UtcNow,
            Duration = duration
        };

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            if (httpContext != null)
            {
                // Get correlation ID
                context.CorrelationId = httpContext.GetCorrelationId();
                
                // Get request information
                context.RequestPath = httpContext.Request.Path;
                context.RequestMethod = httpContext.Request.Method;
                context.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
                context.IpAddress = ipAddress ?? httpContext.Connection.RemoteIpAddress?.ToString();
                
                // Get user information from claims if authenticated
                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    context.UserId = userId ?? GetUserIdFromClaims(httpContext.User);
                    context.Username = username ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                    context.SessionId = sessionId ?? httpContext.User.FindFirst("SessionId")?.Value;
                    context.Roles = httpContext.User.FindFirst(ClaimTypes.Role)?.Value?.Split(',') ?? Array.Empty<string>();
                }
                else
                {
                    context.UserId = userId;
                    context.Username = username;
                    context.SessionId = sessionId;
                }
            }
            else
            {
                // No HTTP context (background service, etc.)
                context.UserId = userId;
                context.Username = username;
                context.IpAddress = ipAddress;
                context.SessionId = sessionId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting enhanced audit context");
            
            // Fallback to provided values
            context.UserId = userId;
            context.Username = username;
            context.IpAddress = ipAddress;
            context.SessionId = sessionId;
        }

        return context;
    }

    /// <summary>
    /// Creates enriched details JSON with all context information.
    /// </summary>
    /// <param name="originalDetails">Original details string</param>
    /// <param name="context">Enhanced context</param>
    /// <returns>Enriched details JSON string</returns>
    private string CreateEnrichedDetails(string? originalDetails, EnhancedAuditContext context)
    {
        var enrichedDetails = new Dictionary<string, object>
        {
            ["timestamp"] = context.Timestamp,
            ["correlationId"] = context.CorrelationId ?? string.Empty,
            ["requestPath"] = context.RequestPath ?? string.Empty,
            ["requestMethod"] = context.RequestMethod ?? string.Empty,
            ["userAgent"] = context.UserAgent ?? string.Empty,
            ["roles"] = context.Roles ?? Array.Empty<string>()
        };

        if (context.Duration.HasValue)
        {
            enrichedDetails["duration"] = context.Duration.Value.TotalMilliseconds;
        }

        if (!string.IsNullOrEmpty(originalDetails))
        {
            enrichedDetails["details"] = originalDetails;
        }

        return JsonSerializer.Serialize(enrichedDetails);
    }

    /// <summary>
    /// Creates security-specific details with security context.
    /// </summary>
    /// <param name="originalDetails">Original details</param>
    /// <param name="severity">Security severity</param>
    /// <param name="context">Enhanced context</param>
    /// <returns>Security details JSON string</returns>
    private string CreateSecurityDetails(string? originalDetails, string severity, EnhancedAuditContext context)
    {
        var securityDetails = new Dictionary<string, object>
        {
            ["severity"] = severity,
            ["timestamp"] = context.Timestamp,
            ["correlationId"] = context.CorrelationId ?? string.Empty,
            ["ipAddress"] = context.IpAddress ?? string.Empty,
            ["userAgent"] = context.UserAgent ?? string.Empty,
            ["requestPath"] = context.RequestPath ?? string.Empty,
            ["requestMethod"] = context.RequestMethod ?? string.Empty,
            ["authenticated"] = context.UserId.HasValue,
            ["roles"] = context.Roles ?? Array.Empty<string>()
        };

        if (!string.IsNullOrEmpty(originalDetails))
        {
            securityDetails["details"] = originalDetails;
        }

        return JsonSerializer.Serialize(securityDetails);
    }

    /// <summary>
    /// Creates performance-specific details with timing and resource information.
    /// </summary>
    /// <param name="operation">Operation name</param>
    /// <param name="duration">Operation duration</param>
    /// <param name="success">Success status</param>
    /// <param name="resourcesUsed">Resources used</param>
    /// <param name="additionalContext">Additional context</param>
    /// <returns>Performance details JSON string</returns>
    private string CreatePerformanceDetails(string operation, TimeSpan duration, bool success,
        Dictionary<string, object>? resourcesUsed, Dictionary<string, object>? additionalContext)
    {
        var performanceDetails = new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["duration"] = duration.TotalMilliseconds,
            ["success"] = success,
            ["timestamp"] = DateTime.UtcNow
        };

        if (resourcesUsed != null)
        {
            performanceDetails["resourcesUsed"] = resourcesUsed;
        }

        if (additionalContext != null)
        {
            performanceDetails["additionalContext"] = additionalContext;
        }

        return JsonSerializer.Serialize(performanceDetails);
    }

    /// <summary>
    /// Logs to structured logging with rich context properties.
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <param name="context">Enhanced context</param>
    /// <param name="details">Event details</param>
    private void LogToStructuredLogging(string eventType, EnhancedAuditContext context, string details)
    {
        using (LogContext.PushProperty("EventType", eventType))
        using (LogContext.PushProperty("UserId", context.UserId))
        using (LogContext.PushProperty("Username", context.Username))
        using (LogContext.PushProperty("SessionId", context.SessionId))
        using (LogContext.PushProperty("IpAddress", context.IpAddress))
        using (LogContext.PushProperty("UserAgent", context.UserAgent))
        using (LogContext.PushProperty("RequestPath", context.RequestPath))
        using (LogContext.PushProperty("RequestMethod", context.RequestMethod))
        using (LogContext.PushProperty("Duration", context.Duration?.TotalMilliseconds))
        {
            _logger.LogInformation("Audit event: {EventType} for user {Username} from {IpAddress}", 
                eventType, context.Username ?? "anonymous", context.IpAddress ?? "unknown");
        }
    }

    /// <summary>
    /// Logs security events with additional security context.
    /// </summary>
    /// <param name="eventType">Security event type</param>
    /// <param name="severity">Security severity</param>
    /// <param name="context">Enhanced context</param>
    /// <param name="details">Security details</param>
    private void LogSecurityEvent(string eventType, string severity, EnhancedAuditContext context, string details)
    {
        using (LogContext.PushProperty("SecurityEvent", eventType))
        using (LogContext.PushProperty("SecuritySeverity", severity))
        using (LogContext.PushProperty("SecurityDetails", details))
        {
            var logLevel = severity switch
            {
                "Critical" => LogLevel.Critical,
                "High" => LogLevel.Error,
                "Medium" => LogLevel.Warning,
                "Low" => LogLevel.Information,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "Security event: {EventType} with severity {Severity} from {IpAddress}", 
                eventType, severity, context.IpAddress ?? "unknown");
        }
    }

    /// <summary>
    /// Records metrics based on event type.
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <param name="context">Enhanced context</param>
    private void RecordEventMetrics(string eventType, EnhancedAuditContext context)
    {
        try
        {
            switch (eventType)
            {
                case "LOGIN_SUCCESS":
                    _metricsService.RecordLoginAttempt(true);
                    break;
                case "LOGIN_FAILED":
                    _metricsService.RecordLoginAttempt(false);
                    break;
                case "USER_REGISTERED":
                    _metricsService.RecordRegistrationAttempt(true);
                    break;
                case "REGISTRATION_FAILED":
                    _metricsService.RecordRegistrationAttempt(false);
                    break;
                case "PASSWORD_RESET_REQUESTED":
                    _metricsService.RecordPasswordResetAttempt("initiate", true);
                    break;
                case "PASSWORD_RESET_COMPLETED":
                    _metricsService.RecordPasswordResetAttempt("complete", true);
                    break;
                case "ACCOUNT_LOCKED":
                    _metricsService.RecordAccountLockout();
                    break;
                case "SESSION_CREATED":
                    _metricsService.RecordSessionOperation("create");
                    break;
                case "SESSION_ENDED":
                    _metricsService.RecordSessionOperation("logout");
                    break;
                case "SESSION_EXPIRED":
                    _metricsService.RecordSessionOperation("expired");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording event metrics for event type: {EventType}", eventType);
        }
    }

    /// <summary>
    /// Records performance metrics.
    /// </summary>
    /// <param name="operation">Operation name</param>
    /// <param name="duration">Operation duration</param>
    /// <param name="success">Success status</param>
    private void RecordPerformanceMetrics(string operation, TimeSpan duration, bool success)
    {
        try
        {
            if (operation.StartsWith("Database"))
            {
                _metricsService.RecordDatabaseQueryDuration(operation, duration);
            }
            else if (operation.StartsWith("BackgroundService"))
            {
                _metricsService.RecordBackgroundServiceOperation(operation, success);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording performance metrics for operation: {Operation}", operation);
        }
    }

    /// <summary>
    /// Gets audit logs for a specific user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>User audit logs</returns>
    public async Task<IEnumerable<AuditLog>> GetAuditLogsForUserAsync(Guid userId, int skip = 0, int take = 50)
    {
        return await _baseAuditService.GetAuditLogsForUserAsync(userId, skip, take);
    }

    /// <summary>
    /// Gets audit logs by event type.
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>Audit logs by event type</returns>
    public async Task<IEnumerable<AuditLog>> GetAuditLogsByEventTypeAsync(string eventType, int skip = 0, int take = 50)
    {
        return await _baseAuditService.GetAuditLogsByEventTypeAsync(eventType, skip, take);
    }

    /// <summary>
    /// Extracts user ID from claims.
    /// </summary>
    /// <param name="principal">Claims principal</param>
    /// <returns>User ID if found</returns>
    private Guid? GetUserIdFromClaims(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

/// <summary>
/// Enhanced audit context with rich information.
/// </summary>
public class EnhancedAuditContext
{
    public string? CorrelationId { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public string[]? Roles { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
}