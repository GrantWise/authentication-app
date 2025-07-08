using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Concurrent;

namespace AuthenticationApi.Common.HealthChecks;

/// <summary>
/// Health check for monitoring background services status and performance.
/// Tracks execution status, last run times, and failure counts for background services.
/// </summary>
public class BackgroundServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<BackgroundServiceHealthCheck> _logger;
    private readonly ConcurrentDictionary<string, BackgroundServiceStatus> _serviceStatuses;
    private readonly TimeSpan _staleThreshold;

    /// <summary>
    /// Initializes a new instance of the BackgroundServiceHealthCheck class.
    /// </summary>
    /// <param name="logger">Logger for health check operations</param>
    /// <param name="configuration">Application configuration</param>
    public BackgroundServiceHealthCheck(ILogger<BackgroundServiceHealthCheck> logger, IConfiguration configuration)
    {
        _logger = logger;
        _serviceStatuses = new ConcurrentDictionary<string, BackgroundServiceStatus>();
        _staleThreshold = TimeSpan.FromMinutes(configuration.GetValue<int>("HealthChecks:BackgroundServiceStaleThresholdMinutes", 30));
    }

    /// <summary>
    /// Checks the health of all registered background services.
    /// </summary>
    /// <param name="context">Health check context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var healthyServices = new List<string>();
            var degradedServices = new List<string>();
            var unhealthyServices = new List<string>();
            var data = new Dictionary<string, object>();

            foreach (var kvp in _serviceStatuses)
            {
                var serviceName = kvp.Key;
                var status = kvp.Value;

                // Check if service is running and not stale
                var isStale = now - status.LastHeartbeat > _staleThreshold;
                var hasRecentFailures = status.FailureCount > 0 && now - status.LastFailure < TimeSpan.FromHours(1);

                if (!status.IsRunning || isStale)
                {
                    unhealthyServices.Add(serviceName);
                }
                else if (hasRecentFailures)
                {
                    degradedServices.Add(serviceName);
                }
                else
                {
                    healthyServices.Add(serviceName);
                }

                // Add service data
                data[serviceName] = new
                {
                    isRunning = status.IsRunning,
                    lastHeartbeat = status.LastHeartbeat,
                    lastExecution = status.LastExecution,
                    executionCount = status.ExecutionCount,
                    failureCount = status.FailureCount,
                    lastFailure = status.LastFailure,
                    lastError = status.LastError,
                    isStale = isStale,
                    hasRecentFailures = hasRecentFailures
                };
            }

            // Add summary data
            data["summary"] = new
            {
                totalServices = _serviceStatuses.Count,
                healthyServices = healthyServices.Count,
                degradedServices = degradedServices.Count,
                unhealthyServices = unhealthyServices.Count,
                checkedAt = now
            };

            // Determine overall health status
            if (unhealthyServices.Any())
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Background services unhealthy: {string.Join(", ", unhealthyServices)}",
                    data: data));
            }

            if (degradedServices.Any())
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Background services degraded: {string.Join(", ", degradedServices)}",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"All {healthyServices.Count} background services healthy",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking background service health");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Error checking background service health",
                ex,
                new Dictionary<string, object> { { "error", ex.Message } }));
        }
    }

    /// <summary>
    /// Registers a background service for health monitoring.
    /// </summary>
    /// <param name="serviceName">Name of the background service</param>
    public void RegisterService(string serviceName)
    {
        _serviceStatuses.TryAdd(serviceName, new BackgroundServiceStatus
        {
            ServiceName = serviceName,
            IsRunning = false,
            LastHeartbeat = DateTime.UtcNow,
            ExecutionCount = 0,
            FailureCount = 0
        });

        _logger.LogInformation("Background service registered for health monitoring: {ServiceName}", serviceName);
    }

    /// <summary>
    /// Records a heartbeat for a background service.
    /// </summary>
    /// <param name="serviceName">Name of the background service</param>
    public void RecordHeartbeat(string serviceName)
    {
        _serviceStatuses.AddOrUpdate(serviceName,
            new BackgroundServiceStatus
            {
                ServiceName = serviceName,
                IsRunning = true,
                LastHeartbeat = DateTime.UtcNow,
                ExecutionCount = 0,
                FailureCount = 0
            },
            (key, existing) =>
            {
                existing.IsRunning = true;
                existing.LastHeartbeat = DateTime.UtcNow;
                return existing;
            });

        _logger.LogDebug("Heartbeat recorded for background service: {ServiceName}", serviceName);
    }

    /// <summary>
    /// Records a successful execution for a background service.
    /// </summary>
    /// <param name="serviceName">Name of the background service</param>
    /// <param name="executionTime">Execution time</param>
    public void RecordExecution(string serviceName, TimeSpan executionTime)
    {
        _serviceStatuses.AddOrUpdate(serviceName,
            new BackgroundServiceStatus
            {
                ServiceName = serviceName,
                IsRunning = true,
                LastHeartbeat = DateTime.UtcNow,
                LastExecution = DateTime.UtcNow,
                ExecutionCount = 1,
                FailureCount = 0,
                LastExecutionTime = executionTime
            },
            (key, existing) =>
            {
                existing.IsRunning = true;
                existing.LastHeartbeat = DateTime.UtcNow;
                existing.LastExecution = DateTime.UtcNow;
                existing.ExecutionCount++;
                existing.LastExecutionTime = executionTime;
                return existing;
            });

        _logger.LogDebug("Execution recorded for background service: {ServiceName}, Duration: {Duration}ms", 
            serviceName, executionTime.TotalMilliseconds);
    }

    /// <summary>
    /// Records a failure for a background service.
    /// </summary>
    /// <param name="serviceName">Name of the background service</param>
    /// <param name="error">Error message</param>
    public void RecordFailure(string serviceName, string error)
    {
        _serviceStatuses.AddOrUpdate(serviceName,
            new BackgroundServiceStatus
            {
                ServiceName = serviceName,
                IsRunning = true,
                LastHeartbeat = DateTime.UtcNow,
                FailureCount = 1,
                LastFailure = DateTime.UtcNow,
                LastError = error
            },
            (key, existing) =>
            {
                existing.IsRunning = true;
                existing.LastHeartbeat = DateTime.UtcNow;
                existing.FailureCount++;
                existing.LastFailure = DateTime.UtcNow;
                existing.LastError = error;
                return existing;
            });

        _logger.LogWarning("Failure recorded for background service: {ServiceName}, Error: {Error}", 
            serviceName, error);
    }

    /// <summary>
    /// Records that a background service has stopped.
    /// </summary>
    /// <param name="serviceName">Name of the background service</param>
    public void RecordStopped(string serviceName)
    {
        _serviceStatuses.AddOrUpdate(serviceName,
            new BackgroundServiceStatus
            {
                ServiceName = serviceName,
                IsRunning = false,
                LastHeartbeat = DateTime.UtcNow
            },
            (key, existing) =>
            {
                existing.IsRunning = false;
                existing.LastHeartbeat = DateTime.UtcNow;
                return existing;
            });

        _logger.LogInformation("Background service stopped: {ServiceName}", serviceName);
    }

    /// <summary>
    /// Gets the current status of all background services.
    /// </summary>
    /// <returns>Dictionary of service statuses</returns>
    public IReadOnlyDictionary<string, BackgroundServiceStatus> GetServiceStatuses()
    {
        return _serviceStatuses.ToList().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}

/// <summary>
/// Status information for a background service.
/// </summary>
public class BackgroundServiceStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime? LastExecution { get; set; }
    public long ExecutionCount { get; set; }
    public long FailureCount { get; set; }
    public DateTime? LastFailure { get; set; }
    public string? LastError { get; set; }
    public TimeSpan? LastExecutionTime { get; set; }
}

/// <summary>
/// Extension methods for BackgroundServiceHealthCheck integration.
/// </summary>
public static class BackgroundServiceHealthCheckExtensions
{
    /// <summary>
    /// Adds background service health checks to the health check builder.
    /// </summary>
    /// <param name="builder">Health checks builder</param>
    /// <param name="name">Health check name</param>
    /// <param name="failureStatus">Failure status</param>
    /// <param name="tags">Health check tags</param>
    /// <returns>Health checks builder</returns>
    public static IServiceCollection AddBackgroundServiceHealthCheck(
        this IServiceCollection services,
        string name = "background-services",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        services.AddSingleton<BackgroundServiceHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<BackgroundServiceHealthCheck>(name, failureStatus, tags ?? Enumerable.Empty<string>());
        
        return services;
    }
}