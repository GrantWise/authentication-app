using AuthenticationApi.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationApi.Features.Authentication.Logout;

/// <summary>
/// Controller for handling user logout operations.
/// Provides endpoint for terminating the current user session.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class LogoutController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the LogoutController class.
    /// </summary>
    /// <param name="mediator">MediatR mediator for handling requests</param>
    public LogoutController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Logs out the current user session by revoking the refresh token.
    /// This terminates only the current session, leaving other device sessions active.
    /// </summary>
    /// <param name="request">The logout request containing the refresh token</param>
    /// <returns>Logout operation result</returns>
    /// <response code="200">Logout successful</response>
    /// <response code="400">Invalid request or validation errors</response>
    /// <response code="401">Unauthorized - invalid or missing access token</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(LogoutResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<LogoutResponse>> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

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
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }
}