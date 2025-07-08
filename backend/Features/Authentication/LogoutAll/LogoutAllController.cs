using AuthenticationApi.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthenticationApi.Features.Authentication.LogoutAll;

/// <summary>
/// Controller for handling logout all operations.
/// Provides endpoint for terminating all user sessions across all devices.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class LogoutAllController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the LogoutAllController class.
    /// </summary>
    /// <param name="mediator">MediatR mediator for handling requests</param>
    public LogoutAllController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Logs out all active sessions for the current user across all devices.
    /// This terminates all sessions and requires the user to log in again on all devices.
    /// </summary>
    /// <returns>Logout all operation result</returns>
    /// <response code="200">Logout all successful</response>
    /// <response code="401">Unauthorized - invalid or missing access token</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("logout-all")]
    [ProducesResponseType(typeof(LogoutAllResponse), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<LogoutAllResponse>> LogoutAll()
    {
        try
        {
            // Extract user ID from JWT token claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var request = new LogoutAllRequest
            {
                UserId = userId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var response = await _mediator.Send(request);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                ruleCode = ex.RuleCode
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during logout all operation" });
        }
    }
}