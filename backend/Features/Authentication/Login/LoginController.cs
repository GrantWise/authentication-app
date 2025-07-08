using AuthenticationApi.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthenticationApi.Features.Authentication.Login;

[ApiController]
[Route("api/auth")]
public class LoginController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public LoginController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Authenticates a user with username and password credentials.
    /// Implements rate limiting (5 attempts per 15 minutes) and account lockout policies.
    /// </summary>
    /// <param name="request">Login credentials and device information</param>
    /// <returns>JWT tokens and user information</returns>
    /// <response code="200">Login successful - returns access and refresh tokens</response>
    /// <response code="400">Invalid credentials or validation errors</response>
    /// <response code="423">Account locked due to failed attempts</response>
    /// <response code="429">Rate limit exceeded - too many attempts</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 423)]
    [ProducesResponseType(typeof(object), 429)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
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
            return BadRequest(new { 
                message = ex.Message,
                errors = ex.Errors 
            });
        }
        catch (AccountLockedException ex)
        {
            return StatusCode(423, new { 
                message = ex.Message,
                lockoutEnd = ex.LockoutEnd,
                remainingMinutes = ex.RemainingLockoutDuration?.TotalMinutes
            });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { 
                message = ex.Message,
                ruleCode = ex.RuleCode 
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }
}