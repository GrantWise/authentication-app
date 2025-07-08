using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationApi.Features.Authentication.Verify;

/// <summary>
/// Controller for handling token verification operations.
/// Provides endpoint for validating access tokens and retrieving user information.
/// </summary>
[ApiController]
[Route("api/auth")]
public class VerifyController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the VerifyController class.
    /// </summary>
    /// <param name="mediator">MediatR mediator for handling requests</param>
    public VerifyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Verifies the validity of an access token and returns user information.
    /// This endpoint can be used by client applications to validate tokens and get user details.
    /// </summary>
    /// <returns>Token verification result with user information</returns>
    /// <response code="200">Token verification completed (check IsValid property for result)</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("verify")]
    [ProducesResponseType(typeof(VerifyResponse), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<VerifyResponse>> VerifyToken()
    {
        try
        {
            // Extract token from Authorization header
            var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Ok(new VerifyResponse
                {
                    IsValid = false,
                    ErrorMessage = "Missing or invalid Authorization header"
                });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var request = new VerifyRequest { AccessToken = token };

            var response = await _mediator.Send(request);
            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during token verification" });
        }
    }
}