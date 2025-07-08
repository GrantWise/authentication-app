using AuthenticationApi.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationApi.Features.Authentication.Refresh;

/// <summary>
/// Controller for handling token refresh operations.
/// Provides endpoint for refreshing expired access tokens using valid refresh tokens.
/// </summary>
[ApiController]
[Route("api/auth")]
public class RefreshController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the RefreshController class.
    /// </summary>
    /// <param name="mediator">MediatR mediator for handling requests</param>
    public RefreshController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// Implements token rotation where the refresh token is also replaced for enhanced security.
    /// </summary>
    /// <param name="request">The refresh token request</param>
    /// <returns>New access and refresh tokens</returns>
    /// <response code="200">Token refresh successful</response>
    /// <response code="400">Invalid refresh token or validation errors</response>
    /// <response code="404">Session not found</response>
    /// <response code="423">User account is locked</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 423)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<RefreshResponse>> RefreshToken([FromBody] RefreshRequest request)
    {
        try
        {
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.DeviceInfo = HttpContext.Request.Headers.UserAgent.ToString();

            var response = await _mediator.Send(request);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                errors = ex.Errors
            });
        }
        catch (AccountLockedException ex)
        {
            return StatusCode(423, new
            {
                message = ex.Message,
                lockoutEnd = ex.LockoutEnd,
                remainingMinutes = ex.RemainingLockoutDuration?.TotalMinutes
            });
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
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }
}