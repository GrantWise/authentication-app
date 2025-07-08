using AuthenticationApi.Common.Data;
using AuthenticationApi.Common.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace AuthenticationApi.Common.Services;

/// <summary>
/// Service for collecting and providing business metrics related to authentication and user management.
/// Provides real-time and historical metrics for monitoring business performance and security.
/// </summary>
public class BusinessMetricsService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MetricsService _metricsService;
    private readonly ILogger<BusinessMetricsService> _logger;
    private readonly Timer _metricsUpdateTimer;
    private readonly ConcurrentDictionary<string, BusinessMetric> _cachedMetrics;
    private readonly TimeSpan _updateInterval;

    /// <summary>
    /// Initializes a new instance of the BusinessMetricsService class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services</param>
    /// <param name="metricsService">Prometheus metrics service</param>
    /// <param name="logger">Logger for service operations</param>
    /// <param name="configuration">Application configuration</param>
    public BusinessMetricsService(
        IServiceProvider serviceProvider,
        MetricsService metricsService,
        ILogger<BusinessMetricsService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _metricsService = metricsService;
        _logger = logger;
        _cachedMetrics = new ConcurrentDictionary<string, BusinessMetric>();
        
        // Configure update interval (default 5 minutes)
        _updateInterval = TimeSpan.FromMinutes(configuration.GetValue<int>("BusinessMetrics:UpdateIntervalMinutes", 5));
        
        // Start periodic metrics updates
        _metricsUpdateTimer = new Timer(UpdateMetrics, null, TimeSpan.Zero, _updateInterval);
    }

    /// <summary>
    /// Gets authentication-related business metrics.
    /// </summary>
    /// <returns>Authentication metrics</returns>
    public async Task<AuthenticationMetrics> GetAuthenticationMetricsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        try
        {
            // Get audit logs for authentication events
            var authLogs = await context.AuditLogs
                .Where(log => log.Timestamp >= last30Days)
                .Where(log => log.EventType.StartsWith("LOGIN") || 
                             log.EventType.StartsWith("REGISTRATION") || 
                             log.EventType.StartsWith("PASSWORD_RESET"))
                .ToListAsync();

            var metrics = new AuthenticationMetrics
            {
                LoginAttempts = new PeriodMetrics
                {
                    Last24Hours = authLogs.Count(log => log.EventType == "LOGIN_ATTEMPT" && log.Timestamp >= last24Hours),
                    Last7Days = authLogs.Count(log => log.EventType == "LOGIN_ATTEMPT" && log.Timestamp >= last7Days),
                    Last30Days = authLogs.Count(log => log.EventType == "LOGIN_ATTEMPT" && log.Timestamp >= last30Days)
                },
                SuccessfulLogins = new PeriodMetrics
                {
                    Last24Hours = authLogs.Count(log => log.EventType == "LOGIN_SUCCESS" && log.Timestamp >= last24Hours),
                    Last7Days = authLogs.Count(log => log.EventType == "LOGIN_SUCCESS" && log.Timestamp >= last7Days),
                    Last30Days = authLogs.Count(log => log.EventType == "LOGIN_SUCCESS" && log.Timestamp >= last30Days)
                },
                FailedLogins = new PeriodMetrics
                {
                    Last24Hours = authLogs.Count(log => log.EventType == "LOGIN_FAILED" && log.Timestamp >= last24Hours),
                    Last7Days = authLogs.Count(log => log.EventType == "LOGIN_FAILED" && log.Timestamp >= last7Days),
                    Last30Days = authLogs.Count(log => log.EventType == "LOGIN_FAILED" && log.Timestamp >= last30Days)
                },
                AccountLockouts = new PeriodMetrics
                {
                    Last24Hours = authLogs.Count(log => log.EventType == "ACCOUNT_LOCKED" && log.Timestamp >= last24Hours),
                    Last7Days = authLogs.Count(log => log.EventType == "ACCOUNT_LOCKED" && log.Timestamp >= last7Days),
                    Last30Days = authLogs.Count(log => log.EventType == "ACCOUNT_LOCKED" && log.Timestamp >= last30Days)
                },
                PasswordResets = new PeriodMetrics
                {
                    Last24Hours = authLogs.Count(log => log.EventType == "PASSWORD_RESET_COMPLETED" && log.Timestamp >= last24Hours),
                    Last7Days = authLogs.Count(log => log.EventType == "PASSWORD_RESET_COMPLETED" && log.Timestamp >= last7Days),
                    Last30Days = authLogs.Count(log => log.EventType == "PASSWORD_RESET_COMPLETED" && log.Timestamp >= last30Days)
                }
            };

            // Calculate success rates
            metrics.LoginSuccessRate = CalculateSuccessRate(metrics.SuccessfulLogins.Last24Hours, metrics.LoginAttempts.Last24Hours);
            metrics.Timestamp = now;

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authentication metrics");
            return new AuthenticationMetrics { Timestamp = now };
        }
    }

    /// <summary>
    /// Gets user-related business metrics.
    /// </summary>
    /// <returns>User metrics</returns>
    public async Task<UserMetrics> GetUserMetricsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        try
        {
            var totalUsers = await context.Users.CountAsync();
            var activeUsers = await context.Users.CountAsync(u => !u.IsLocked);
            var lockedUsers = await context.Users.CountAsync(u => u.IsLocked);
            var mfaEnabledUsers = await context.Users.CountAsync(u => u.MfaEnabled);

            var newRegistrations = new PeriodMetrics
            {
                Last24Hours = await context.Users.CountAsync(u => u.CreatedAt >= last24Hours),
                Last7Days = await context.Users.CountAsync(u => u.CreatedAt >= last7Days),
                Last30Days = await context.Users.CountAsync(u => u.CreatedAt >= last30Days)
            };

            var metrics = new UserMetrics
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                LockedUsers = lockedUsers,
                MfaEnabledUsers = mfaEnabledUsers,
                NewRegistrations = newRegistrations,
                MfaAdoptionRate = totalUsers > 0 ? (double)mfaEnabledUsers / totalUsers * 100 : 0,
                Timestamp = now
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user metrics");
            return new UserMetrics { Timestamp = now };
        }
    }

    /// <summary>
    /// Gets session-related business metrics.
    /// </summary>
    /// <returns>Session metrics</returns>
    public async Task<SessionMetrics> GetSessionMetricsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);

        try
        {
            var activeSessions = await context.ActiveSessions.CountAsync();
            var expiredSessions = await context.ActiveSessions.CountAsync(s => s.ExpiresAt <= now);
            
            var sessionLogs = await context.AuditLogs
                .Where(log => log.Timestamp >= last24Hours)
                .Where(log => log.EventType.StartsWith("SESSION"))
                .ToListAsync();

            var metrics = new SessionMetrics
            {
                ActiveSessions = activeSessions,
                ExpiredSessions = expiredSessions,
                SessionsCreated24h = sessionLogs.Count(log => log.EventType == "SESSION_CREATED"),
                SessionsExpired24h = sessionLogs.Count(log => log.EventType == "SESSION_EXPIRED"),
                AverageSessionDuration = await CalculateAverageSessionDuration(context),
                Timestamp = now
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session metrics");
            return new SessionMetrics { Timestamp = now };
        }
    }

    /// <summary>
    /// Gets system performance metrics.
    /// </summary>
    /// <returns>System metrics</returns>
    public async Task<SystemMetrics> GetSystemMetricsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        
        var now = DateTime.UtcNow;

        try
        {
            // Database performance
            var dbMetrics = await GetDatabaseMetrics(context);
            
            // API performance (from internal counters)
            var apiMetrics = GetApiMetrics();
            
            var metrics = new SystemMetrics
            {
                DatabaseMetrics = dbMetrics,
                ApiMetrics = apiMetrics,
                Timestamp = now
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system metrics");
            return new SystemMetrics { Timestamp = now };
        }
    }

    /// <summary>
    /// Gets comprehensive business metrics summary.
    /// </summary>
    /// <returns>Business metrics summary</returns>
    public async Task<BusinessMetricsSummary> GetBusinessMetricsSummaryAsync()
    {
        try
        {
            var authMetrics = await GetAuthenticationMetricsAsync();
            var userMetrics = await GetUserMetricsAsync();
            var sessionMetrics = await GetSessionMetricsAsync();
            var systemMetrics = await GetSystemMetricsAsync();

            return new BusinessMetricsSummary
            {
                Authentication = authMetrics,
                Users = userMetrics,
                Sessions = sessionMetrics,
                System = systemMetrics,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business metrics summary");
            return new BusinessMetricsSummary { Timestamp = DateTime.UtcNow };
        }
    }

    /// <summary>
    /// Periodic update of cached metrics and Prometheus gauges.
    /// </summary>
    /// <param name="state">Timer state (unused)</param>
    private async void UpdateMetrics(object? state)
    {
        try
        {
            _logger.LogDebug("Updating business metrics");
            
            var authMetrics = await GetAuthenticationMetricsAsync();
            var userMetrics = await GetUserMetricsAsync();
            var sessionMetrics = await GetSessionMetricsAsync();
            
            // Update Prometheus metrics
            _metricsService.UpdateTotalUserCount(userMetrics.TotalUsers);
            _metricsService.UpdateLockedUserCount(userMetrics.LockedUsers);
            _metricsService.UpdateActiveSessionCount(sessionMetrics.ActiveSessions);
            
            // Cache metrics
            _cachedMetrics.AddOrUpdate("authentication", new BusinessMetric { Value = authMetrics, Timestamp = DateTime.UtcNow }, (key, old) => new BusinessMetric { Value = authMetrics, Timestamp = DateTime.UtcNow });
            _cachedMetrics.AddOrUpdate("users", new BusinessMetric { Value = userMetrics, Timestamp = DateTime.UtcNow }, (key, old) => new BusinessMetric { Value = userMetrics, Timestamp = DateTime.UtcNow });
            _cachedMetrics.AddOrUpdate("sessions", new BusinessMetric { Value = sessionMetrics, Timestamp = DateTime.UtcNow }, (key, old) => new BusinessMetric { Value = sessionMetrics, Timestamp = DateTime.UtcNow });
            
            _logger.LogDebug("Business metrics updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business metrics");
        }
    }

    private double CalculateSuccessRate(int successes, int total)
    {
        return total > 0 ? (double)successes / total * 100 : 0;
    }

    private async Task<TimeSpan> CalculateAverageSessionDuration(AuthenticationDbContext context)
    {
        var completedSessions = await context.AuditLogs
            .Where(log => log.EventType == "SESSION_ENDED" || log.EventType == "SESSION_EXPIRED")
            .Where(log => log.Timestamp >= DateTime.UtcNow.AddDays(-7))
            .Select(log => new { log.Timestamp, log.Details })
            .ToListAsync();

        if (!completedSessions.Any())
            return TimeSpan.Zero;

        // This is a simplified calculation - in production, you'd want to track session start/end times
        // For now, we'll use a default average based on token expiry times
        return TimeSpan.FromMinutes(45); // Average between 15min access token and 60min refresh token
    }

    private async Task<DatabaseMetrics> GetDatabaseMetrics(AuthenticationDbContext context)
    {
        var startTime = DateTime.UtcNow;
        var canConnect = await context.Database.CanConnectAsync();
        var connectionTime = DateTime.UtcNow - startTime;

        return new DatabaseMetrics
        {
            ConnectionHealthy = canConnect,
            ConnectionTime = connectionTime,
            TotalTables = 4, // Users, ActiveSessions, AuditLogs, etc.
            TotalRecords = await context.Users.CountAsync() + await context.ActiveSessions.CountAsync() + await context.AuditLogs.CountAsync()
        };
    }

    private ApiMetrics GetApiMetrics()
    {
        // Get metrics from internal counters
        var internalCounters = _metricsService.GetAllInternalCounters();
        
        return new ApiMetrics
        {
            TotalRequests = internalCounters.GetValueOrDefault("total_requests", 0),
            AverageResponseTime = 0, // Would need to calculate from collected data
            ErrorRate = 0 // Would need to calculate from success/failure ratios
        };
    }

    public void Dispose()
    {
        _metricsUpdateTimer?.Dispose();
    }
}

#region Metric Data Models

public class BusinessMetric
{
    public object Value { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class BusinessMetricsSummary
{
    public AuthenticationMetrics Authentication { get; set; } = new();
    public UserMetrics Users { get; set; } = new();
    public SessionMetrics Sessions { get; set; } = new();
    public SystemMetrics System { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class AuthenticationMetrics
{
    public PeriodMetrics LoginAttempts { get; set; } = new();
    public PeriodMetrics SuccessfulLogins { get; set; } = new();
    public PeriodMetrics FailedLogins { get; set; } = new();
    public PeriodMetrics AccountLockouts { get; set; } = new();
    public PeriodMetrics PasswordResets { get; set; } = new();
    public double LoginSuccessRate { get; set; }
    public DateTime Timestamp { get; set; }
}

public class UserMetrics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int MfaEnabledUsers { get; set; }
    public PeriodMetrics NewRegistrations { get; set; } = new();
    public double MfaAdoptionRate { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SessionMetrics
{
    public int ActiveSessions { get; set; }
    public int ExpiredSessions { get; set; }
    public int SessionsCreated24h { get; set; }
    public int SessionsExpired24h { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SystemMetrics
{
    public DatabaseMetrics DatabaseMetrics { get; set; } = new();
    public ApiMetrics ApiMetrics { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class PeriodMetrics
{
    public int Last24Hours { get; set; }
    public int Last7Days { get; set; }
    public int Last30Days { get; set; }
}

public class DatabaseMetrics
{
    public bool ConnectionHealthy { get; set; }
    public TimeSpan ConnectionTime { get; set; }
    public int TotalTables { get; set; }
    public int TotalRecords { get; set; }
}

public class ApiMetrics
{
    public long TotalRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
}

#endregion