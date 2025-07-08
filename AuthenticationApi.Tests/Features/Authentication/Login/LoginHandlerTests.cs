using AuthenticationApi.Features.Authentication.Login;
using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Exceptions;
using AuthenticationApi.Common.Entities;
using AuthenticationApi.Tests.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthenticationApi.Tests.Features.Authentication.Login;

/// <summary>
/// Unit tests for LoginHandler covering security scenarios and business logic.
/// Tests authentication flow, account lockout, MFA requirements, and audit logging.
/// </summary>
public class LoginHandlerTests : TestBase
{
    private readonly LoginHandler _handler;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<IAuditService> _mockAuditService;

    public LoginHandlerTests()
    {
        _mockUserService = MockServices.CreateMockUserService();
        _mockJwtTokenService = MockServices.CreateMockJwtTokenService();
        _mockSessionService = MockServices.CreateMockSessionService();
        _mockAuditService = MockServices.CreateMockAuditService();
        
        _handler = new LoginHandler(
            _mockUserService.Object,
            _mockJwtTokenService.Object,
            _mockSessionService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessfulLoginResponse()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", "$2a$12$hash", false, 0, false);
        var request = new LoginRequest 
        { 
            Username = "testuser", 
            Password = "ValidPassword123!", 
            DeviceInfo = "Test Device",
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.ValidatePasswordAsync("ValidPassword123!", "$2a$12$hash"))
            .ReturnsAsync(true);
        _mockJwtTokenService.Setup(x => x.GenerateAccessToken(user))
            .ReturnsAsync("access-token");
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken(user))
            .ReturnsAsync("refresh-token");
        _mockJwtTokenService.Setup(x => x.GetJtiFromToken("refresh-token"))
            .Returns("refresh-jti");
        _mockJwtTokenService.Setup(x => x.GetTokenExpiration("access-token"))
            .Returns(DateTime.UtcNow.AddMinutes(15));
        _mockJwtTokenService.Setup(x => x.GetTokenExpiration("refresh-token"))
            .Returns(DateTime.UtcNow.AddMinutes(60));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.RequiresMfa.Should().BeFalse();
        result.AccessTokenExpiry.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        result.RefreshTokenExpiry.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));

        // Verify security operations
        _mockUserService.Verify(x => x.ResetFailedLoginAttemptsAsync(user.UserId), Times.Once);
        _mockSessionService.Verify(x => x.CreateSessionAsync(
            user.UserId, 
            "refresh-jti", 
            "Test Device", 
            "127.0.0.1"), Times.Once);
        _mockAuditService.Verify(x => x.LogEventAsync(
            "LOGIN_SUCCESS",
            user.UserId,
            "testuser",
            "127.0.0.1",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsValidationExceptionAndIncrementsFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", "$2a$12$hash", false, 0, false);
        var request = new LoginRequest 
        { 
            Username = "testuser", 
            Password = "WrongPassword", 
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.ValidatePasswordAsync("WrongPassword", "$2a$12$hash"))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(request, CancellationToken.None));

        TestHelpers.GetFieldFromValidationException(exception).Should().Be("Password");
        exception.Message.Should().Be("Invalid username or password");

        // Verify failed login attempt is incremented
        _mockUserService.Verify(x => x.IncrementFailedLoginAttemptAsync(user.UserId), Times.Once);
        
        // Verify audit logging
        _mockAuditService.Verify(x => x.LogEventAsync(
            "LOGIN_FAILED",
            user.UserId,
            "testuser",
            "127.0.0.1",
            It.IsAny<string>(),
            "Invalid password"), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsValidationExceptionAndLogsAuditEvent()
    {
        // Arrange
        var request = new LoginRequest 
        { 
            Username = "nonexistent", 
            Password = "AnyPassword", 
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(request, CancellationToken.None));

        TestHelpers.GetFieldFromValidationException(exception).Should().Be("Username");
        exception.Message.Should().Be("Invalid username or password");

        // Verify audit logging for user not found
        _mockAuditService.Verify(x => x.LogEventAsync(
            "LOGIN_FAILED",
            null,
            "nonexistent",
            "127.0.0.1",
            It.IsAny<string>(),
            "User not found"), Times.Once);
    }

    [Fact]
    public async Task Handle_UserLocked_ThrowsAccountLockedExceptionAndLogsAuditEvent()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", "$2a$12$hash", true, 5, false);
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        var request = new LoginRequest 
        { 
            Username = "testuser", 
            Password = "ValidPassword123!", 
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.IsUserLockedAsync(user.UserId))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AccountLockedException>(() => 
            _handler.Handle(request, CancellationToken.None));

        exception.LockoutEnd.Should().Be(user.LockoutEnd);

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogEventAsync(
            "LOGIN_FAILED",
            user.UserId,
            "testuser",
            "127.0.0.1",
            It.IsAny<string>(),
            "Account locked"), Times.Once);
    }

    [Fact]
    public async Task Handle_FifthFailedLoginAttempt_LocksAccountAndLogsAuditEvent()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", "$2a$12$hash", false, 4, false);
        var request = new LoginRequest 
        { 
            Username = "testuser", 
            Password = "WrongPassword", 
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.ValidatePasswordAsync("WrongPassword", "$2a$12$hash"))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(request, CancellationToken.None));

        TestHelpers.GetFieldFromValidationException(exception).Should().Be("Password");

        // Verify account lockout
        _mockUserService.Verify(x => x.LockUserAsync(user.UserId, TimeSpan.FromMinutes(30)), Times.Once);
        
        // Verify audit logging for lockout
        _mockAuditService.Verify(x => x.LogEventAsync(
            "ACCOUNT_LOCKED",
            user.UserId,
            "testuser",
            "127.0.0.1",
            It.IsAny<string>(),
            "Account locked due to failed login attempts"), Times.Once);
    }

    [Fact]
    public async Task Handle_MfaEnabledUser_ReturnsMfaRequiredResponse()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", "$2a$12$hash", false, 0, true);
        var request = new LoginRequest 
        { 
            Username = "testuser", 
            Password = "ValidPassword123!", 
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.ValidatePasswordAsync("ValidPassword123!", "$2a$12$hash"))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequiresMfa.Should().BeTrue();
        result.MfaChallenge.Should().Be("Please enter your MFA code");
        result.AccessToken.Should().BeEmpty();
        result.RefreshToken.Should().BeEmpty();

        // Verify MFA audit logging
        _mockAuditService.Verify(x => x.LogEventAsync(
            "LOGIN_MFA_REQUIRED",
            user.UserId,
            "testuser",
            "127.0.0.1",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);

        // Verify tokens are not generated for MFA flow
        _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ResetsFailedLoginAttempts()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", "$2a$12$hash", false, 3, false);
        var request = new LoginRequest 
        { 
            Username = "testuser", 
            Password = "ValidPassword123!", 
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.ValidatePasswordAsync("ValidPassword123!", "$2a$12$hash"))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequiresMfa.Should().BeFalse();

        // Verify failed login attempts are reset
        _mockUserService.Verify(x => x.ResetFailedLoginAttemptsAsync(user.UserId), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidLogin_CreatesSessionWithCorrectParameters()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", "$2a$12$hash", false, 0, false);
        var request = new LoginRequest 
        { 
            Username = "testuser", 
            Password = "ValidPassword123!", 
            DeviceInfo = "iPhone 14 Pro",
            IpAddress = "192.168.1.100"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.ValidatePasswordAsync("ValidPassword123!", "$2a$12$hash"))
            .ReturnsAsync(true);
        _mockJwtTokenService.Setup(x => x.GetJtiFromToken(It.IsAny<string>()))
            .Returns("unique-jti");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify session creation with correct parameters
        _mockSessionService.Verify(x => x.CreateSessionAsync(
            user.UserId,
            "unique-jti",
            "iPhone 14 Pro",
            "192.168.1.100"), Times.Once);
    }
}