using Prometheus;
using System.Collections.Concurrent;

namespace AuthenticationApi.Common.Services;

/// <summary>
/// Service for collecting and exposing business metrics using Prometheus.
/// Tracks authentication, user management, and system performance metrics.
/// </summary>
public class MetricsService
{
    // Authentication Metrics
    private readonly Counter _loginAttempts = Metrics
        .CreateCounter("auth_login_attempts_total", "Total number of login attempts", new[] { "result", "mfa_required" });
    
    private readonly Counter _registrationAttempts = Metrics
        .CreateCounter("auth_registration_attempts_total", "Total number of registration attempts", new[] { "result" });
    
    private readonly Counter _passwordResetAttempts = Metrics
        .CreateCounter("auth_password_reset_attempts_total", "Total number of password reset attempts", new[] { "type", "result" });
    
    private readonly Counter _accountLockouts = Metrics
        .CreateCounter("auth_account_lockouts_total", "Total number of account lockouts");
    
    private readonly Histogram _authenticationDuration = Metrics
        .CreateHistogram("auth_request_duration_seconds", "Authentication request duration in seconds", new[] { "endpoint", "result" });

    // Session Metrics
    private readonly Gauge _activeSessions = Metrics
        .CreateGauge("auth_active_sessions", "Number of active user sessions");
    
    private readonly Counter _sessionOperations = Metrics
        .CreateCounter("auth_session_operations_total", "Total session operations", new[] { "operation" });
    
    private readonly Counter _sessionCleanupOperations = Metrics
        .CreateCounter("auth_session_cleanup_operations_total", "Session cleanup operations", new[] { "result" });

    // User Metrics
    private readonly Gauge _totalUsers = Metrics
        .CreateGauge("auth_total_users", "Total number of registered users");
    
    private readonly Gauge _lockedUsers = Metrics
        .CreateGauge("auth_locked_users", "Number of locked user accounts");
    
    private readonly Counter _userOperations = Metrics
        .CreateCounter("auth_user_operations_total", "Total user operations", new[] { "operation" });

    // System Metrics
    private readonly Histogram _databaseQueryDuration = Metrics
        .CreateHistogram("auth_database_query_duration_seconds", "Database query duration in seconds", new[] { "operation" });
    
    private readonly Counter _backgroundServiceOperations = Metrics
        .CreateCounter("auth_background_service_operations_total", "Background service operations", new[] { "service", "result" });
    
    private readonly Gauge _keyRotationStatus = Metrics
        .CreateGauge("auth_key_rotation_status", "JWT key rotation status (1 = healthy, 0 = unhealthy)");

    // API Metrics
    private readonly Histogram _apiRequestDuration = Metrics
        .CreateHistogram("auth_api_request_duration_seconds", "API request duration in seconds", new[] { "method", "endpoint", "status_code" });
    
    private readonly Counter _apiRequestsTotal = Metrics
        .CreateCounter("auth_api_requests_total", "Total API requests", new[] { "method", "endpoint", "status_code" });

    private readonly ILogger<MetricsService> _logger;

    // Thread-safe counters for internal tracking
    private readonly ConcurrentDictionary<string, long> _internalCounters = new();

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
    }

    #region Authentication Metrics

    /// <summary>
    /// Records a login attempt with the result and MFA requirement.
    /// </summary>
    /// <param name="success">Whether the login was successful</param>
    /// <param name="mfaRequired">Whether MFA was required for this login</param>
    public void RecordLoginAttempt(bool success, bool mfaRequired = false)
    {
        var result = success ? "success" : "failure";
        var mfaStatus = mfaRequired ? "required" : "not_required";
        _loginAttempts.WithLabels(result, mfaStatus).Inc();
        
        _logger.LogDebug("Login attempt recorded: {Result}, MFA: {MfaRequired}", result, mfaStatus);
    }

    /// <summary>
    /// Records a registration attempt with the result.
    /// </summary>
    /// <param name="success">Whether the registration was successful</param>
    public void RecordRegistrationAttempt(bool success)
    {
        var result = success ? "success" : "failure";
        _registrationAttempts.WithLabels(result).Inc();
        
        _logger.LogDebug("Registration attempt recorded: {Result}", result);
    }

    /// <summary>
    /// Records a password reset attempt.
    /// </summary>
    /// <param name="type">Type of password reset operation (initiate, complete)</param>
    /// <param name="success">Whether the operation was successful</param>
    public void RecordPasswordResetAttempt(string type, bool success)
    {
        var result = success ? "success" : "failure";
        _passwordResetAttempts.WithLabels(type, result).Inc();
        
        _logger.LogDebug("Password reset attempt recorded: {Type}, {Result}", type, result);
    }

    /// <summary>
    /// Records an account lockout event.
    /// </summary>
    public void RecordAccountLockout()
    {
        _accountLockouts.Inc();
        _logger.LogDebug("Account lockout recorded");
    }

    /// <summary>
    /// Records the duration of an authentication request.
    /// </summary>
    /// <param name="endpoint">The authentication endpoint</param>
    /// <param name="success">Whether the request was successful</param>
    /// <param name="duration">The request duration</param>
    public void RecordAuthenticationDuration(string endpoint, bool success, TimeSpan duration)
    {
        var result = success ? "success" : "failure";
        _authenticationDuration.WithLabels(endpoint, result).Observe(duration.TotalSeconds);
        
        _logger.LogDebug("Authentication duration recorded: {Endpoint}, {Result}, {Duration}ms", 
            endpoint, result, duration.TotalMilliseconds);
    }

    #endregion

    #region Session Metrics

    /// <summary>
    /// Updates the active session count.
    /// </summary>
    /// <param name="count">Current number of active sessions</param>
    public void UpdateActiveSessionCount(int count)
    {
        _activeSessions.Set(count);
        _logger.LogDebug("Active session count updated: {Count}", count);
    }

    /// <summary>
    /// Records a session operation.
    /// </summary>
    /// <param name="operation">The session operation (create, refresh, logout, cleanup)</param>
    public void RecordSessionOperation(string operation)
    {
        _sessionOperations.WithLabels(operation).Inc();
        _logger.LogDebug("Session operation recorded: {Operation}", operation);
    }

    /// <summary>
    /// Records a session cleanup operation.
    /// </summary>
    /// <param name="success">Whether the cleanup was successful</param>
    /// <param name="cleanedCount">Number of sessions cleaned up</param>
    public void RecordSessionCleanup(bool success, int cleanedCount = 0)
    {
        var result = success ? "success" : "failure";
        _sessionCleanupOperations.WithLabels(result).Inc();
        
        if (success && cleanedCount > 0)
        {
            _internalCounters.AddOrUpdate("total_sessions_cleaned", cleanedCount, (key, oldValue) => oldValue + cleanedCount);
        }
        
        _logger.LogDebug("Session cleanup recorded: {Result}, Cleaned: {Count}", result, cleanedCount);
    }

    #endregion

    #region User Metrics

    /// <summary>
    /// Updates the total user count.
    /// </summary>
    /// <param name="count">Total number of registered users</param>
    public void UpdateTotalUserCount(int count)
    {
        _totalUsers.Set(count);
        _logger.LogDebug("Total user count updated: {Count}", count);
    }

    /// <summary>
    /// Updates the locked user count.
    /// </summary>
    /// <param name="count">Number of locked user accounts</param>
    public void UpdateLockedUserCount(int count)
    {
        _lockedUsers.Set(count);
        _logger.LogDebug("Locked user count updated: {Count}", count);
    }

    /// <summary>
    /// Records a user operation.
    /// </summary>
    /// <param name="operation">The user operation (create, update, lock, unlock)</param>
    public void RecordUserOperation(string operation)
    {
        _userOperations.WithLabels(operation).Inc();
        _logger.LogDebug("User operation recorded: {Operation}", operation);
    }

    #endregion

    #region System Metrics

    /// <summary>
    /// Records the duration of a database query.
    /// </summary>
    /// <param name="operation">The database operation</param>
    /// <param name="duration">The query duration</param>
    public void RecordDatabaseQueryDuration(string operation, TimeSpan duration)
    {
        _databaseQueryDuration.WithLabels(operation).Observe(duration.TotalSeconds);
        _logger.LogDebug("Database query duration recorded: {Operation}, {Duration}ms", 
            operation, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Records a background service operation.
    /// </summary>
    /// <param name="serviceName">Name of the background service</param>
    /// <param name="success">Whether the operation was successful</param>
    public void RecordBackgroundServiceOperation(string serviceName, bool success)
    {
        var result = success ? "success" : "failure";
        _backgroundServiceOperations.WithLabels(serviceName, result).Inc();
        
        _logger.LogDebug("Background service operation recorded: {Service}, {Result}", serviceName, result);
    }

    /// <summary>
    /// Updates the key rotation status.
    /// </summary>
    /// <param name="healthy">Whether key rotation is healthy</param>
    public void UpdateKeyRotationStatus(bool healthy)
    {
        _keyRotationStatus.Set(healthy ? 1 : 0);
        _logger.LogDebug("Key rotation status updated: {Status}", healthy ? "healthy" : "unhealthy");
    }

    #endregion

    #region API Metrics

    /// <summary>
    /// Records an API request with duration and result.
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="duration">Request duration</param>
    public void RecordApiRequest(string method, string endpoint, int statusCode, TimeSpan duration)
    {
        var statusCodeStr = statusCode.ToString();
        
        _apiRequestsTotal.WithLabels(method, endpoint, statusCodeStr).Inc();
        _apiRequestDuration.WithLabels(method, endpoint, statusCodeStr).Observe(duration.TotalSeconds);
        
        _logger.LogDebug("API request recorded: {Method} {Endpoint} {StatusCode}, {Duration}ms", 
            method, endpoint, statusCode, duration.TotalMilliseconds);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the current value of an internal counter.
    /// </summary>
    /// <param name="counterName">Name of the counter</param>
    /// <returns>Current counter value</returns>
    public long GetInternalCounter(string counterName)
    {
        return _internalCounters.GetValueOrDefault(counterName, 0);
    }

    /// <summary>
    /// Gets all internal counter values.
    /// </summary>
    /// <returns>Dictionary of counter names and values</returns>
    public Dictionary<string, long> GetAllInternalCounters()
    {
        return new Dictionary<string, long>(_internalCounters);
    }

    #endregion
}