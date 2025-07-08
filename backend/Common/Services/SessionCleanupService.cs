using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.HealthChecks;
using System.Diagnostics;

namespace AuthenticationApi.Common.Services;

/// <summary>
/// Background service that periodically cleans up expired authentication sessions.
/// Configurable service that removes expired session records to maintain database performance.
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly BackgroundServiceHealthCheck _healthCheck;
    private readonly bool _enabled;
    private readonly TimeSpan _interval;
    private readonly int _batchSize;
    private const string ServiceName = "SessionCleanupService";

    /// <summary>
    /// Initializes a new instance of the SessionCleanupService class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services</param>
    /// <param name="logger">Logger for service operations</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="healthCheck">Background service health check</param>
    public SessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupService> logger,
        IConfiguration configuration,
        BackgroundServiceHealthCheck healthCheck)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _healthCheck = healthCheck;

        // Load configuration with defaults from technical specification
        _enabled = _configuration.GetValue<bool>("SessionCleanup:Enabled", true);
        var intervalMinutes = _configuration.GetValue<int>("SessionCleanup:IntervalMinutes", 30);
        _interval = TimeSpan.FromMinutes(intervalMinutes);
        _batchSize = _configuration.GetValue<int>("SessionCleanup:BatchSize", 100);

        _logger.LogInformation("Session cleanup service configured: Enabled={Enabled}, Interval={Interval}, BatchSize={BatchSize}",
            _enabled, _interval, _batchSize);
        
        // Register with health check
        _healthCheck.RegisterService(ServiceName);
    }

    /// <summary>
    /// Executes the background session cleanup task.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the service</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Session cleanup service is disabled");
            return;
        }

        _logger.LogInformation("Session cleanup service started");
        _healthCheck.RecordHeartbeat(ServiceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _healthCheck.RecordHeartbeat(ServiceName);
                var stopwatch = Stopwatch.StartNew();
                
                await PerformCleanupAsync(stoppingToken);
                
                stopwatch.Stop();
                _healthCheck.RecordExecution(ServiceName, stopwatch.Elapsed);
                
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is being stopped
                _logger.LogInformation("Session cleanup service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during session cleanup");
                _healthCheck.RecordFailure(ServiceName, ex.Message);
                
                // Wait before retrying to avoid rapid failure loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
        _healthCheck.RecordStopped(ServiceName);
    }

    /// <summary>
    /// Performs the actual cleanup of expired sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        var startTime = DateTime.UtcNow;
        _logger.LogDebug("Starting session cleanup at {StartTime}", startTime);

        try
        {
            // Use the existing cleanup method from ISessionService
            await sessionService.CleanupExpiredSessionsAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Session cleanup completed successfully in {Duration}ms", duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Session cleanup failed after {Duration}ms", duration.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Handles service startup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Session cleanup service starting up");
        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Handles service shutdown.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Session cleanup service shutting down");
        await base.StopAsync(cancellationToken);
    }
}