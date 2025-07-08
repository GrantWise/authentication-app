using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Exceptions;
using MediatR;

namespace AuthenticationApi.Features.Authentication.Refresh;

/// <summary>
/// Handles refresh token requests to generate new access tokens.
/// Implements token rotation strategy where refresh tokens are rotated on each use for enhanced security.
/// </summary>
public class RefreshHandler : IRequestHandler<RefreshRequest, RefreshResponse>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the RefreshHandler class.
    /// </summary>
    /// <param name="jwtTokenService">Service for JWT token operations</param>
    /// <param name="sessionService">Service for session management</param>
    /// <param name="userService">Service for user operations</param>
    /// <param name="auditService">Service for audit logging</param>
    public RefreshHandler(
        IJwtTokenService jwtTokenService,
        ISessionService sessionService,
        IUserService userService,
        IAuditService auditService)
    {
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _userService = userService;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the refresh token request and generates new tokens.
    /// </summary>
    /// <param name="request">The refresh request containing the refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    public async Task<RefreshResponse> Handle(RefreshRequest request, CancellationToken cancellationToken)
    {
        // Validate the refresh token
        if (!_jwtTokenService.ValidateToken(request.RefreshToken))
        {
            await _auditService.LogEventAsync("TOKEN_REFRESH_FAILED", 
                ipAddress: request.IpAddress, 
                details: "Invalid refresh token");
            
            throw new ValidationException("RefreshToken", "Invalid or expired refresh token");
        }

        // Extract JTI from refresh token
        var refreshTokenJti = _jwtTokenService.GetJtiFromToken(request.RefreshToken);
        if (string.IsNullOrEmpty(refreshTokenJti))
        {
            await _auditService.LogEventAsync("TOKEN_REFRESH_FAILED", 
                ipAddress: request.IpAddress, 
                details: "Missing JTI in refresh token");
            
            throw new ValidationException("RefreshToken", "Invalid refresh token format");
        }

        // Check if session exists and is active
        var session = await _sessionService.GetSessionByJtiAsync(refreshTokenJti);
        if (session == null || !await _sessionService.IsSessionActiveAsync(refreshTokenJti))
        {
            await _auditService.LogEventAsync("TOKEN_REFRESH_FAILED", 
                ipAddress: request.IpAddress, 
                details: "Session not found or expired");
            
            throw new NotFoundException("Session", refreshTokenJti);
        }

        // Get the user associated with this session
        var user = await _userService.GetUserByIdAsync(session.UserId);
        if (user == null)
        {
            await _auditService.LogEventAsync("TOKEN_REFRESH_FAILED", 
                session.UserId, 
                ipAddress: request.IpAddress, 
                details: "User not found for session");
            
            throw new NotFoundException("User", session.UserId.ToString());
        }

        // Check if user is locked
        if (await _userService.IsUserLockedAsync(user.UserId))
        {
            await _auditService.LogEventAsync("TOKEN_REFRESH_FAILED", 
                user.UserId, 
                user.Username, 
                request.IpAddress, 
                details: "User account is locked");
            
            // Revoke the session since user is locked
            await _sessionService.RevokeSessionAsync(refreshTokenJti);
            
            throw new AccountLockedException(user.LockoutEnd ?? DateTime.UtcNow.AddMinutes(30));
        }

        // Generate new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(user);
        var newRefreshTokenJti = _jwtTokenService.GetJtiFromToken(newRefreshToken);

        // Update the session with new refresh token JTI (token rotation)
        await _sessionService.RevokeSessionAsync(refreshTokenJti);
        await _sessionService.CreateSessionAsync(user.UserId, newRefreshTokenJti!, request.DeviceInfo, request.IpAddress);

        await _auditService.LogEventAsync("TOKEN_REFRESH_SUCCESS", 
            user.UserId, 
            user.Username, 
            request.IpAddress);

        return new RefreshResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiry = _jwtTokenService.GetTokenExpiration(newAccessToken),
            RefreshTokenExpiry = _jwtTokenService.GetTokenExpiration(newRefreshToken)
        };
    }
}