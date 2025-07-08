using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Exceptions;
using AuthenticationApi.Common.Services;
using MediatR;

namespace AuthenticationApi.Features.Authentication.Login;

public class LoginHandler : IRequestHandler<LoginRequest, LoginResponse>
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
    
    public LoginHandler(
        IUserService userService,
        IJwtTokenService jwtTokenService,
        ISessionService sessionService,
        IAuditService auditService)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _auditService = auditService;
    }
    
    public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken cancellationToken)
    {
        // Rate limiting is now handled by ASP.NET Core middleware
        
        // Get user by username
        var user = await _userService.GetUserByUsernameAsync(request.Username);
        
        if (user == null)
        {
            await _auditService.LogEventAsync("LOGIN_FAILED", 
                username: request.Username, 
                ipAddress: request.IpAddress, 
                details: "User not found");
            
            throw new ValidationException("Username", "Invalid username or password");
        }
        
        // Check if user is locked
        if (await _userService.IsUserLockedAsync(user.UserId))
        {
            await _auditService.LogEventAsync("LOGIN_FAILED", 
                user.UserId, 
                request.Username, 
                request.IpAddress, 
                details: "Account locked");
            
            throw new AccountLockedException(user.LockoutEnd ?? DateTime.UtcNow.AddMinutes(30));
        }
        
        // Validate password
        if (!await _userService.ValidatePasswordAsync(request.Password, user.PasswordHash))
        {
            await _userService.IncrementFailedLoginAttemptAsync(user.UserId);
            
            await _auditService.LogEventAsync("LOGIN_FAILED", 
                user.UserId, 
                request.Username, 
                request.IpAddress, 
                details: "Invalid password");
            
            // Check if user should be locked after failed attempt
            if (user.FailedLoginAttempts >= 4) // Will be 5 after increment
            {
                await _userService.LockUserAsync(user.UserId, TimeSpan.FromMinutes(30));
                await _auditService.LogEventAsync("ACCOUNT_LOCKED", 
                    user.UserId, 
                    request.Username, 
                    request.IpAddress, 
                    details: "Account locked due to failed login attempts");
            }
            
            throw new ValidationException("Password", "Invalid username or password");
        }
        
        // Reset failed login attempts on successful authentication
        await _userService.ResetFailedLoginAttemptsAsync(user.UserId);
        
        // Check if MFA is required
        if (user.MfaEnabled)
        {
            // For MFA flow, we don't generate tokens yet
            await _auditService.LogEventAsync("LOGIN_MFA_REQUIRED", 
                user.UserId, 
                request.Username, 
                request.IpAddress);
            
            return new LoginResponse
            {
                RequiresMfa = true,
                MfaChallenge = "Please enter your MFA code"
            };
        }
        
        // Generate tokens
        var accessToken = await _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = await _jwtTokenService.GenerateRefreshToken(user);
        var refreshTokenJti = _jwtTokenService.GetJtiFromToken(refreshToken);
        
        // Create session
        await _sessionService.CreateSessionAsync(user.UserId, refreshTokenJti!, request.DeviceInfo, request.IpAddress);
        
        await _auditService.LogEventAsync("LOGIN_SUCCESS", 
            user.UserId, 
            request.Username, 
            request.IpAddress);
        
        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = _jwtTokenService.GetTokenExpiration(accessToken),
            RefreshTokenExpiry = _jwtTokenService.GetTokenExpiration(refreshToken),
            RequiresMfa = false
        };
    }
}