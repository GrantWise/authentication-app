using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Exceptions;
using MediatR;

namespace AuthenticationApi.Features.Authentication.LogoutAll;

/// <summary>
/// Handles logout all requests to terminate all user sessions across all devices.
/// Provides a security feature for users to revoke all active sessions when needed.
/// </summary>
public class LogoutAllHandler : IRequestHandler<LogoutAllRequest, LogoutAllResponse>
{
    private readonly ISessionService _sessionService;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the LogoutAllHandler class.
    /// </summary>
    /// <param name="sessionService">Service for session management</param>
    /// <param name="userService">Service for user operations</param>
    /// <param name="auditService">Service for audit logging</param>
    public LogoutAllHandler(
        ISessionService sessionService,
        IUserService userService,
        IAuditService auditService)
    {
        _sessionService = sessionService;
        _userService = userService;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the logout all request and terminates all sessions for the user.
    /// </summary>
    /// <param name="request">The logout all request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout all operation result</returns>
    public async Task<LogoutAllResponse> Handle(LogoutAllRequest request, CancellationToken cancellationToken)
    {
        // Verify that the user exists
        var user = await _userService.GetUserByIdAsync(request.UserId);
        if (user == null)
        {
            await _auditService.LogEventAsync("LOGOUT_ALL_FAILED", 
                request.UserId, 
                ipAddress: request.IpAddress, 
                details: "User not found");
            
            throw new NotFoundException("User", request.UserId.ToString());
        }

        // Get all active sessions for the user
        var activeSessions = await _sessionService.GetActiveSessionsForUserAsync(request.UserId);
        var sessionCount = activeSessions.Count();

        // Revoke all sessions for the user
        await _sessionService.RevokeAllSessionsForUserAsync(request.UserId);

        await _auditService.LogEventAsync("LOGOUT_ALL_SUCCESS", 
            request.UserId, 
            user.Username, 
            request.IpAddress, 
            details: $"All sessions terminated. Sessions revoked: {sessionCount}");

        return new LogoutAllResponse
        {
            Success = true,
            Message = "All sessions terminated successfully",
            SessionsTerminated = sessionCount
        };
    }
}