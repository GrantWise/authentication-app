using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuthenticationApi.Common.Services;
using AuthenticationApi.Common.Middleware;

namespace AuthenticationApi.Features.Monitoring;

/// <summary>
/// Controller for exposing business metrics for monitoring and dashboard purposes.
/// Provides comprehensive metrics about authentication, users, sessions, and system performance.
/// </summary>
[ApiController]
[Route("api/metrics")]
[Authorize] // Require authentication for metrics access
public class MetricsController : ControllerBase
{
    private readonly BusinessMetricsService _businessMetricsService;
    private readonly ILogger<MetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of the MetricsController class.
    /// </summary>
    /// <param name="businessMetricsService">Service for retrieving business metrics</param>
    /// <param name="logger">Logger for controller operations</param>
    public MetricsController(
        BusinessMetricsService businessMetricsService,
        ILogger<MetricsController> logger)
    {
        _businessMetricsService = businessMetricsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets comprehensive business metrics summary including authentication, users, sessions, and system metrics.
    /// </summary>
    /// <returns>Complete business metrics summary</returns>
    /// <response code="200">Metrics retrieved successfully</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(BusinessMetricsSummary), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<BusinessMetricsSummary>> GetBusinessMetrics()
    {
        try
        {
            var correlationId = HttpContext.GetCorrelationId();
            _logger.LogInformation("Business metrics requested. CorrelationId: {CorrelationId}", correlationId);
            
            var metrics = await _businessMetricsService.GetBusinessMetricsSummaryAsync();
            
            _logger.LogDebug("Business metrics retrieved successfully. CorrelationId: {CorrelationId}", correlationId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business metrics");
            return StatusCode(500, new { message = "Error retrieving business metrics" });
        }
    }

    /// <summary>
    /// Gets authentication-related metrics including login attempts, success rates, and security events.
    /// </summary>
    /// <returns>Authentication metrics</returns>
    /// <response code="200">Authentication metrics retrieved successfully</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("authentication")]
    [ProducesResponseType(typeof(AuthenticationMetrics), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<AuthenticationMetrics>> GetAuthenticationMetrics()
    {
        try
        {
            var correlationId = HttpContext.GetCorrelationId();
            _logger.LogInformation("Authentication metrics requested. CorrelationId: {CorrelationId}", correlationId);
            
            var metrics = await _businessMetricsService.GetAuthenticationMetricsAsync();
            
            _logger.LogDebug("Authentication metrics retrieved successfully. CorrelationId: {CorrelationId}", correlationId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authentication metrics");
            return StatusCode(500, new { message = "Error retrieving authentication metrics" });
        }
    }

    /// <summary>
    /// Gets user-related metrics including total users, registrations, and MFA adoption.
    /// </summary>
    /// <returns>User metrics</returns>
    /// <response code="200">User metrics retrieved successfully</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserMetrics), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<UserMetrics>> GetUserMetrics()
    {
        try
        {
            var correlationId = HttpContext.GetCorrelationId();
            _logger.LogInformation("User metrics requested. CorrelationId: {CorrelationId}", correlationId);
            
            var metrics = await _businessMetricsService.GetUserMetricsAsync();
            
            _logger.LogDebug("User metrics retrieved successfully. CorrelationId: {CorrelationId}", correlationId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user metrics");
            return StatusCode(500, new { message = "Error retrieving user metrics" });
        }
    }

    /// <summary>
    /// Gets session-related metrics including active sessions, session duration, and cleanup statistics.
    /// </summary>
    /// <returns>Session metrics</returns>
    /// <response code="200">Session metrics retrieved successfully</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(SessionMetrics), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<SessionMetrics>> GetSessionMetrics()
    {
        try
        {
            var correlationId = HttpContext.GetCorrelationId();
            _logger.LogInformation("Session metrics requested. CorrelationId: {CorrelationId}", correlationId);
            
            var metrics = await _businessMetricsService.GetSessionMetricsAsync();
            
            _logger.LogDebug("Session metrics retrieved successfully. CorrelationId: {CorrelationId}", correlationId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session metrics");
            return StatusCode(500, new { message = "Error retrieving session metrics" });
        }
    }

    /// <summary>
    /// Gets system performance metrics including database performance and API statistics.
    /// </summary>
    /// <returns>System metrics</returns>
    /// <response code="200">System metrics retrieved successfully</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("system")]
    [ProducesResponseType(typeof(SystemMetrics), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<SystemMetrics>> GetSystemMetrics()
    {
        try
        {
            var correlationId = HttpContext.GetCorrelationId();
            _logger.LogInformation("System metrics requested. CorrelationId: {CorrelationId}", correlationId);
            
            var metrics = await _businessMetricsService.GetSystemMetricsAsync();
            
            _logger.LogDebug("System metrics retrieved successfully. CorrelationId: {CorrelationId}", correlationId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system metrics");
            return StatusCode(500, new { message = "Error retrieving system metrics" });
        }
    }
}