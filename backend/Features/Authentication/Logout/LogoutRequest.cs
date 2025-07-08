using FluentValidation;
using MediatR;

namespace AuthenticationApi.Features.Authentication.Logout;

/// <summary>
/// Request to logout from the current session by revoking the refresh token.
/// Invalidates the current session while leaving other device sessions active.
/// </summary>
public class LogoutRequest : IRequest<LogoutResponse>
{
    /// <summary>
    /// Gets or sets the refresh token for the session to be terminated.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address of the logout request for audit purposes.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Response indicating the logout operation result.
/// </summary>
public class LogoutResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the logout was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets a message describing the logout result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Validator for logout requests to ensure required fields are provided.
/// </summary>
public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    /// <summary>
    /// Initializes a new instance of the LogoutRequestValidator class.
    /// </summary>
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required for logout")
            .MinimumLength(10)
            .WithMessage("Invalid refresh token format");
    }
}