using AuthenticationApi.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthenticationApi.Features.Authentication.PasswordReset;

/// <summary>
/// Controller for handling password reset operations.
/// Implements rate limiting and comprehensive security measures for password recovery.
/// </summary>
[ApiController]
[Route("api/auth/password-reset")]
public class PasswordResetController : ControllerBase
{
    private readonly IMediator _mediator;
    
    /// <summary>
    /// Initializes a new instance of the PasswordResetController class.
    /// </summary>
    /// <param name="mediator">MediatR mediator for handling requests</param>
    public PasswordResetController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Initiates a password reset process by sending a reset token to the user.
    /// Implements rate limiting to prevent abuse and always returns success to prevent user enumeration.
    /// </summary>
    /// <param name="request">Password reset initiation request with username or email</param>
    /// <returns>Success message regardless of whether user exists</returns>
    /// <response code="200">Reset request processed - email sent if account exists</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="429">Rate limit exceeded - too many reset attempts</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("initiate")]
    [EnableRateLimiting("GeneralPolicy")]
    [ProducesResponseType(typeof(InitiatePasswordResetResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 429)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<InitiatePasswordResetResponse>> InitiatePasswordReset([FromBody] InitiatePasswordResetRequest request)
    {
        try
        {
            // Populate IP address and device info from request context
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.DeviceInfo = HttpContext.Request.Headers.UserAgent.ToString();
            
            var response = await _mediator.Send(request);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { 
                message = ex.Message,
                field = ex.Errors.FirstOrDefault().Key,
                errors = ex.Errors 
            });
        }
        catch (Exception)
        {
            // Never reveal internal errors for security
            return StatusCode(500, new { 
                message = "An error occurred while processing your request. Please try again later." 
            });
        }
    }
    
    /// <summary>
    /// Completes a password reset by validating the reset token and updating the user's password.
    /// Implements comprehensive validation and security measures.
    /// </summary>
    /// <param name="request">Password reset completion request with token and new password</param>
    /// <returns>Success confirmation for password reset</returns>
    /// <response code="200">Password reset successful</response>
    /// <response code="400">Invalid token, expired token, or validation errors</response>
    /// <response code="423">Account locked - contact support</response>
    /// <response code="429">Rate limit exceeded - too many attempts</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("complete")]
    [EnableRateLimiting("GeneralPolicy")]
    [ProducesResponseType(typeof(CompletePasswordResetResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 423)]
    [ProducesResponseType(typeof(object), 429)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<CompletePasswordResetResponse>> CompletePasswordReset([FromBody] CompletePasswordResetRequest request)
    {
        try
        {
            // Populate IP address and device info from request context
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.DeviceInfo = HttpContext.Request.Headers.UserAgent.ToString();
            
            var response = await _mediator.Send(request);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { 
                message = ex.Message,
                field = ex.Errors.FirstOrDefault().Key,
                errors = ex.Errors 
            });
        }
        catch (BusinessRuleException ex)
        {
            if (ex.RuleCode == "ACCOUNT_LOCKED")
            {
                return StatusCode(423, new { 
                    message = ex.Message,
                    ruleCode = ex.RuleCode 
                });
            }
            
            return BadRequest(new { 
                message = ex.Message,
                ruleCode = ex.RuleCode 
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { 
                message = "An error occurred while resetting your password. Please try again later." 
            });
        }
    }
}