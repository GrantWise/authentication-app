using MediatR;

namespace AuthenticationApi.Features.Authentication.Verify;

/// <summary>
/// Request to verify the validity of an access token.
/// Used by client applications to check if the current token is still valid.
/// </summary>
public class VerifyRequest : IRequest<VerifyResponse>
{
    /// <summary>
    /// Gets or sets the access token to be verified.
    /// This is typically extracted from the Authorization header.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
}

/// <summary>
/// Response containing token verification results and user information.
/// </summary>
public class VerifyResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with the token.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the username associated with the token.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the user roles associated with the token.
    /// </summary>
    public string? Roles { get; set; }

    /// <summary>
    /// Gets or sets when the token expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when the token was issued.
    /// </summary>
    public DateTime? IssuedAt { get; set; }

    /// <summary>
    /// Gets or sets an error message if the token is invalid.
    /// </summary>
    public string? ErrorMessage { get; set; }
}