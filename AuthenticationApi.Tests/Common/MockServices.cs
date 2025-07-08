using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Entities;
using Moq;

namespace AuthenticationApi.Tests.Common;

/// <summary>
/// Provides pre-configured mock services for testing.
/// Reduces boilerplate code and ensures consistent mock behavior across tests.
/// </summary>
public static class MockServices
{
    /// <summary>
    /// Creates a mock IAuditService with basic logging setup.
    /// </summary>
    public static Mock<IAuditService> CreateMockAuditService()
    {
        var mockAuditService = new Mock<IAuditService>();
        
        // Setup LogEventAsync to return successfully
        mockAuditService
            .Setup(x => x.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        return mockAuditService;
    }

    /// <summary>
    /// Creates a mock IJwtTokenService with standard token operations.
    /// </summary>
    public static Mock<IJwtTokenService> CreateMockJwtTokenService()
    {
        var mockJwtService = new Mock<IJwtTokenService>();
        
        // Setup token generation
        mockJwtService
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .ReturnsAsync("mock-access-token");
        
        mockJwtService
            .Setup(x => x.GenerateRefreshToken(It.IsAny<User>()))
            .ReturnsAsync("mock-refresh-token");
        
        // Setup token validation
        mockJwtService
            .Setup(x => x.ValidateToken(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Setup JTI extraction
        mockJwtService
            .Setup(x => x.GetJtiFromToken(It.IsAny<string>()))
            .Returns("mock-jti");
        
        // Setup token expiration
        mockJwtService
            .Setup(x => x.GetTokenExpiration(It.IsAny<string>()))
            .Returns(DateTime.UtcNow.AddMinutes(15));
        
        return mockJwtService;
    }

    /// <summary>
    /// Creates a mock IUserService with standard user operations.
    /// </summary>
    public static Mock<IUserService> CreateMockUserService()
    {
        var mockUserService = new Mock<IUserService>();
        
        // Setup password hashing
        mockUserService
            .Setup(x => x.HashPasswordAsync(It.IsAny<string>()))
            .ReturnsAsync("$2a$12$mocked.hash");
        
        // Setup password validation
        mockUserService
            .Setup(x => x.ValidatePasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Setup user lockout checks
        mockUserService
            .Setup(x => x.IsUserLockedAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);
        
        // Setup user operations
        mockUserService
            .Setup(x => x.IncrementFailedLoginAttemptAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        
        mockUserService
            .Setup(x => x.ResetFailedLoginAttemptsAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        
        mockUserService
            .Setup(x => x.LockUserAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        
        return mockUserService;
    }

    /// <summary>
    /// Creates a mock ISessionService with standard session operations.
    /// </summary>
    public static Mock<ISessionService> CreateMockSessionService()
    {
        var mockSessionService = new Mock<ISessionService>();
        
        // Setup session creation
        mockSessionService
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new ActiveSession
            {
                UserId = Guid.NewGuid(),
                RefreshTokenJti = "mock-jti",
                DeviceInfo = "Mock Device",
                IpAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        
        // Setup session validation
        mockSessionService
            .Setup(x => x.IsSessionActiveAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Setup session revocation
        mockSessionService
            .Setup(x => x.RevokeSessionAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        return mockSessionService;
    }
}