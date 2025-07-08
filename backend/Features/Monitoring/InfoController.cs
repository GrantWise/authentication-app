using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Diagnostics;
using AuthenticationApi.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using AuthenticationApi.Common.Data;

namespace AuthenticationApi.Features.Monitoring;

/// <summary>
/// Controller for providing application information and status.
/// Exposes version, environment, and system status information for monitoring and operations.
/// </summary>
[ApiController]
[Route("api/info")]
public class InfoController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AuthenticationDbContext _context;
    private readonly IKeyManagementService _keyManagementService;
    private readonly ILogger<InfoController> _logger;

    public InfoController(
        IConfiguration configuration,
        AuthenticationDbContext context,
        IKeyManagementService keyManagementService,
        ILogger<InfoController> logger)
    {
        _configuration = configuration;
        _context = context;
        _keyManagementService = keyManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets comprehensive application information including version, environment, and system status.
    /// </summary>
    /// <returns>Application information and status</returns>
    /// <response code="200">Application information retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApplicationInfo), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<ApplicationInfo>> GetApplicationInfo()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var process = Process.GetCurrentProcess();

            var info = new ApplicationInfo
            {
                Application = new ApplicationDetails
                {
                    Name = assembly.GetName().Name ?? "AuthenticationApi",
                    Version = assembly.GetName().Version?.ToString() ?? "1.0.0",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    BuildDate = GetBuildDate(assembly),
                    StartTime = process.StartTime,
                    Uptime = DateTime.Now - process.StartTime
                },
                System = new SystemDetails
                {
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = process.WorkingSet64,
                    OperatingSystem = Environment.OSVersion.ToString(),
                    RuntimeVersion = Environment.Version.ToString(),
                    ThreadCount = process.Threads.Count
                },
                Configuration = new ConfigurationDetails
                {
                    DatabaseProvider = GetDatabaseProvider(),
                    JwtIssuer = _configuration["JwtSettings:Issuer"] ?? "Unknown",
                    DataProtectionEnabled = _configuration.GetValue<bool>("JwtSettings:UseDataProtectionForKeys", false),
                    SessionCleanupEnabled = _configuration.GetValue<bool>("SessionCleanup:Enabled", true),
                    MetricsEnabled = _configuration.GetValue<bool>("Monitoring:EnableMetrics", true),
                    HealthChecksEnabled = _configuration.GetValue<bool>("Monitoring:EnableDetailedHealthChecks", true)
                },
                Status = await GetSystemStatusInternal()
            };

            _logger.LogDebug("Application info retrieved successfully");
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application information");
            return StatusCode(500, new { message = "Error retrieving application information" });
        }
    }

    /// <summary>
    /// Gets basic application version information.
    /// </summary>
    /// <returns>Application version details</returns>
    /// <response code="200">Version information retrieved successfully</response>
    [HttpGet("version")]
    [ProducesResponseType(typeof(ApplicationDetails), 200)]
    public ActionResult<ApplicationDetails> GetVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var process = Process.GetCurrentProcess();

            var version = new ApplicationDetails
            {
                Name = assembly.GetName().Name ?? "AuthenticationApi",
                Version = assembly.GetName().Version?.ToString() ?? "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                BuildDate = GetBuildDate(assembly),
                StartTime = process.StartTime,
                Uptime = DateTime.Now - process.StartTime
            };

            return Ok(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version information");
            return StatusCode(500, new { message = "Error retrieving version information" });
        }
    }

    /// <summary>
    /// Gets system health status.
    /// </summary>
    /// <returns>System status information</returns>
    /// <response code="200">System status retrieved successfully</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatus), 200)]
    public async Task<ActionResult<SystemStatus>> GetSystemStatus()
    {
        try
        {
            var status = await GetSystemStatusInternal();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system status");
            return StatusCode(500, new { message = "Error retrieving system status" });
        }
    }

    private async Task<SystemStatus> GetSystemStatusInternal()
    {
        var status = new SystemStatus
        {
            Database = await GetDatabaseStatus(),
            KeyManagement = await GetKeyManagementStatus(),
            Timestamp = DateTime.UtcNow
        };

        status.Overall = DetermineOverallStatus(status);
        return status;
    }

    private async Task<ServiceStatus> GetDatabaseStatus()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var canConnect = await _context.Database.CanConnectAsync();
            stopwatch.Stop();

            if (canConnect)
            {
                var userCount = await _context.Users.CountAsync();
                var sessionCount = await _context.ActiveSessions.CountAsync();

                return new ServiceStatus
                {
                    Status = "Healthy",
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        { "userCount", userCount },
                        { "activeSessionCount", sessionCount },
                        { "provider", GetDatabaseProvider() }
                    }
                };
            }
            else
            {
                return new ServiceStatus
                {
                    Status = "Unhealthy",
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        { "error", "Cannot connect to database" }
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database status");
            return new ServiceStatus
            {
                Status = "Unhealthy",
                ResponseTime = -1,
                Details = new Dictionary<string, object>
                {
                    { "error", ex.Message }
                }
            };
        }
    }

    private async Task<ServiceStatus> GetKeyManagementStatus()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var currentKeyId = await _keyManagementService.GetCurrentKeyIdAsync();
            var shouldRotate = await _keyManagementService.ShouldRotateKeysAsync();
            stopwatch.Stop();

            return new ServiceStatus
            {
                Status = "Healthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    { "currentKeyId", currentKeyId },
                    { "shouldRotate", shouldRotate }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key management status");
            return new ServiceStatus
            {
                Status = "Unhealthy",
                ResponseTime = -1,
                Details = new Dictionary<string, object>
                {
                    { "error", ex.Message }
                }
            };
        }
    }

    private string GetDatabaseProvider()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
            ? "SQLite" 
            : "SQL Server";
    }

    private DateTime GetBuildDate(Assembly assembly)
    {
        try
        {
            var attribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
            if (attribute != null && attribute.Key == "BuildDate")
            {
                if (DateTime.TryParse(attribute.Value, out var buildDate))
                {
                    return buildDate;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine build date");
        }

        // Fallback to file creation time
        try
        {
            var fileInfo = new FileInfo(assembly.Location);
            return fileInfo.CreationTime;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private string DetermineOverallStatus(SystemStatus status)
    {
        var services = new[] { status.Database, status.KeyManagement };
        
        if (services.All(s => s.Status == "Healthy"))
            return "Healthy";
        
        if (services.Any(s => s.Status == "Unhealthy"))
            return "Unhealthy";
        
        return "Degraded";
    }
}

/// <summary>
/// Application information response model.
/// </summary>
public class ApplicationInfo
{
    public ApplicationDetails Application { get; set; } = new();
    public SystemDetails System { get; set; } = new();
    public ConfigurationDetails Configuration { get; set; } = new();
    public SystemStatus Status { get; set; } = new();
}

/// <summary>
/// Application details including version and runtime information.
/// </summary>
public class ApplicationDetails
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime BuildDate { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// System details including hardware and runtime information.
/// </summary>
public class SystemDetails
{
    public string MachineName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public long WorkingSet { get; set; }
    public string OperatingSystem { get; set; } = string.Empty;
    public string RuntimeVersion { get; set; } = string.Empty;
    public int ThreadCount { get; set; }
}

/// <summary>
/// Configuration details for the application.
/// </summary>
public class ConfigurationDetails
{
    public string DatabaseProvider { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public bool DataProtectionEnabled { get; set; }
    public bool SessionCleanupEnabled { get; set; }
    public bool MetricsEnabled { get; set; }
    public bool HealthChecksEnabled { get; set; }
}

/// <summary>
/// System status information.
/// </summary>
public class SystemStatus
{
    public string Overall { get; set; } = string.Empty;
    public ServiceStatus Database { get; set; } = new();
    public ServiceStatus KeyManagement { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Individual service status information.
/// </summary>
public class ServiceStatus
{
    public string Status { get; set; } = string.Empty;
    public long ResponseTime { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}