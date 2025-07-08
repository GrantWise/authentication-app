using FluentValidation;
using MediatR;

namespace AuthenticationApi.Features.Authentication.Refresh;

/// <summary>
/// Request to refresh an expired access token using a valid refresh token.
/// Part of the JWT token rotation strategy for maintaining user sessions.
/// </summary>
public class RefreshRequest : IRequest<RefreshResponse>
{
    /// <summary>
    /// Gets or sets the refresh token to be used for generating a new access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device information for session tracking.
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the refresh request for audit purposes.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Response containing the new access token and updated refresh token.
/// </summary>
public class RefreshResponse
{
    /// <summary>
    /// Gets or sets the new access token with extended expiration.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new refresh token (token rotation for security).
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the new access token expires.
    /// </summary>
    public DateTime AccessTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets when the new refresh token expires.
    /// </summary>
    public DateTime RefreshTokenExpiry { get; set; }
}

/// <summary>
/// Validator for refresh token requests to ensure required fields are provided.
/// </summary>
public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    /// <summary>
    /// Initializes a new instance of the RefreshRequestValidator class.
    /// </summary>
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            .MinimumLength(10)
            .WithMessage("Invalid refresh token format");
    }
}