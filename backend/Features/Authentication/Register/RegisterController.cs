using AuthenticationApi.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthenticationApi.Features.Authentication.Register;

/// <summary>
/// Controller for handling user registration operations.
/// Implements rate limiting and comprehensive validation for secure user creation.
/// </summary>
[ApiController]
[Route("api/auth")]
public class RegisterController : ControllerBase
{
    private readonly IMediator _mediator;
    
    /// <summary>
    /// Initializes a new instance of the RegisterController class.
    /// </summary>
    /// <param name="mediator">MediatR mediator for handling requests</param>
    public RegisterController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Creates a new user account with comprehensive validation and security measures.
    /// Implements rate limiting to prevent abuse and ensures unique usernames and emails.
    /// </summary>
    /// <param name="request">User registration details including username, email, and password</param>
    /// <returns>Registration confirmation with user details</returns>
    /// <response code="201">Registration successful - user account created</response>
    /// <response code="400">Invalid input data or validation errors</response>
    /// <response code="409">Username or email already exists</response>
    /// <response code="429">Rate limit exceeded - too many registration attempts</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("register")]
    [EnableRateLimiting("GeneralPolicy")]
    [ProducesResponseType(typeof(RegisterResponse), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 409)]
    [ProducesResponseType(typeof(object), 429)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Populate IP address and device info from request context
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.DeviceInfo = HttpContext.Request.Headers.UserAgent.ToString();
            
            var response = await _mediator.Send(request);
            
            // Return 201 Created for successful registration
            return CreatedAtAction(nameof(Register), new { userId = response.UserId }, response);
        }
        catch (ValidationException ex)
        {
            // Handle validation errors (username/email already exists, password requirements, etc.)
            var field = ex.Errors.FirstOrDefault().Key;
            
            if (field == "Username" && ex.Message.Contains("already taken"))
            {
                return Conflict(new { 
                    message = ex.Message,
                    field = field,
                    code = "USERNAME_EXISTS"
                });
            }
            
            if (field == "Email" && ex.Message.Contains("already registered"))
            {
                return Conflict(new { 
                    message = ex.Message,
                    field = field,
                    code = "EMAIL_EXISTS"
                });
            }
            
            return BadRequest(new { 
                message = ex.Message,
                field = field,
                errors = ex.Errors 
            });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { 
                message = ex.Message,
                ruleCode = ex.RuleCode 
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { 
                message = "An error occurred during registration. Please try again later." 
            });
        }
    }
}