using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Exceptions;
using MediatR;

namespace AuthenticationApi.Features.Authentication.Logout;

/// <summary>
/// Handles logout requests to terminate user sessions.
/// Revokes the current session while maintaining other active sessions on different devices.
/// </summary>
public class LogoutHandler : IRequestHandler<LogoutRequest, LogoutResponse>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the LogoutHandler class.
    /// </summary>
    /// <param name="jwtTokenService">Service for JWT token operations</param>
    /// <param name="sessionService">Service for session management</param>
    /// <param name="auditService">Service for audit logging</param>
    public LogoutHandler(
        IJwtTokenService jwtTokenService,
        ISessionService sessionService,
        IAuditService auditService)
    {
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the logout request and terminates the specified session.
    /// </summary>
    /// <param name="request">The logout request containing the refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout operation result</returns>
    public async Task<LogoutResponse> Handle(LogoutRequest request, CancellationToken cancellationToken)
    {
        // Extract JTI from refresh token
        var refreshTokenJti = _jwtTokenService.GetJtiFromToken(request.RefreshToken);
        if (string.IsNullOrEmpty(refreshTokenJti))
        {
            await _auditService.LogEventAsync("LOGOUT_FAILED", 
                ipAddress: request.IpAddress, 
                details: "Invalid refresh token format");
            
            throw new ValidationException("RefreshToken", "Invalid refresh token format");
        }

        // Get the session to be terminated
        var session = await _sessionService.GetSessionByJtiAsync(refreshTokenJti);
        if (session == null)
        {
            await _auditService.LogEventAsync("LOGOUT_FAILED", 
                ipAddress: request.IpAddress, 
                details: "Session not found");
            
            // Still return success as the session is effectively logged out
            return new LogoutResponse
            {
                Success = true,
                Message = "Logout successful"
            };
        }

        // Revoke the session
        await _sessionService.RevokeSessionAsync(refreshTokenJti);

        await _auditService.LogEventAsync("LOGOUT_SUCCESS", 
            session.UserId, 
            session.User?.Username, 
            request.IpAddress, 
            details: $"Session terminated for device: {session.DeviceInfo}");

        return new LogoutResponse
        {
            Success = true,
            Message = "Logout successful"
        };
    }
}