using FluentValidation;
using MediatR;

namespace AuthenticationApi.Features.Authentication.Login;

/// <summary>
/// Request model for user authentication with username and password.
/// Supports both desktop and mobile device authentication scenarios.
/// </summary>
public class LoginRequest : IRequest<LoginResponse>
{
    /// <summary>
    /// Gets or sets the username for authentication.
    /// Must be a valid username registered in the system.
    /// </summary>
    /// <example>john.doe</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication.
    /// Must meet the configured password complexity requirements.
    /// </summary>
    /// <example>SecurePassword123!</example>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device information for session tracking.
    /// Automatically populated from User-Agent header if not provided.
    /// </summary>
    /// <example>Mozilla/5.0 (Windows NT 10.0; Win64; x64)</example>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the login request.
    /// Automatically populated from the request if not provided.
    /// </summary>
    /// <example>192.168.1.100</example>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Response model for successful authentication containing JWT tokens and user information.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Gets or sets the access token for API authentication.
    /// Valid for 15 minutes and contains user claims.
    /// </summary>
    /// <example>eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token for obtaining new access tokens.
    /// Valid for 60 minutes and used for token rotation.
    /// </summary>
    /// <example>eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the access token expires (UTC).
    /// </summary>
    /// <example>2024-01-01T12:15:00Z</example>
    public DateTime AccessTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets when the refresh token expires (UTC).
    /// </summary>
    /// <example>2024-01-01T13:00:00Z</example>
    public DateTime RefreshTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets whether Multi-Factor Authentication is required.
    /// If true, tokens will not be provided and MFA challenge must be completed.
    /// </summary>
    /// <example>false</example>
    public bool RequiresMfa { get; set; }

    /// <summary>
    /// Gets or sets the MFA challenge message if MFA is required.
    /// Provides instructions for completing the MFA process.
    /// </summary>
    /// <example>Please enter your authentication code</example>
    public string? MfaChallenge { get; set; }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MaximumLength(255)
            .WithMessage("Username must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}