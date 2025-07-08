using MediatR;

namespace AuthenticationApi.Features.Authentication.LogoutAll;

/// <summary>
/// Request to logout from all active sessions across all devices.
/// Revokes all refresh tokens associated with the user account.
/// </summary>
public class LogoutAllRequest : IRequest<LogoutAllResponse>
{
    /// <summary>
    /// Gets or sets the user ID whose sessions should be terminated.
    /// This is typically extracted from the JWT token claims.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the logout request for audit purposes.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Response indicating the logout all operation result.
/// </summary>
public class LogoutAllResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the logout operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets a message describing the logout result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of sessions that were terminated.
    /// </summary>
    public int SessionsTerminated { get; set; }
}