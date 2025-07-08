using AuthenticationApi.Features.Authentication.Register;
using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Exceptions;
using AuthenticationApi.Common.Entities;
using AuthenticationApi.Tests.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthenticationApi.Tests.Features.Authentication.Register;

/// <summary>
/// Unit tests for RegisterHandler covering validation scenarios and business logic.
/// Tests user registration, duplicate detection, password hashing, and audit logging.
/// </summary>
public class RegisterHandlerTests : TestBase
{
    private readonly RegisterHandler _handler;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IAuditService> _mockAuditService;

    public RegisterHandlerTests()
    {
        _mockUserService = MockServices.CreateMockUserService();
        _mockAuditService = MockServices.CreateMockAuditService();
        
        _handler = new RegisterHandler(
            _mockUserService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_ValidRegistration_ReturnsSuccessfulRegistrationResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            IpAddress = "127.0.0.1",
            DeviceInfo = "Test Device"
        };

        var hashedPassword = "$2a$12$hashedpassword";
        var createdUser = new User
        {
            UserId = Guid.NewGuid(),
            Username = "newuser",
            Email = "newuser@example.com",
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow,
            Roles = "User"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.GetUserByEmailAsync("newuser@example.com"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.HashPasswordAsync("SecurePassword123!"))
            .ReturnsAsync(hashedPassword);
        _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(createdUser.UserId);
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("newuser@example.com");
        result.Message.Should().Be("Account created successfully. You can now log in.");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify user creation with correct properties
        _mockUserService.Verify(x => x.CreateUserAsync(It.Is<User>(u => 
            u.Username == "newuser" &&
            u.Email == "newuser@example.com" &&
            u.PasswordHash == hashedPassword &&
            u.Roles == "User" &&
            u.MfaEnabled == false &&
            u.IsLocked == false &&
            u.FailedLoginAttempts == 0)), Times.Once);

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogEventAsync(
            "USER_REGISTERED",
            createdUser.UserId,
            "newuser",
            "127.0.0.1",
            It.IsAny<string>(),
            "User account created successfully"), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsValidationException()
    {
        // Arrange
        var existingUser = CreateTestUser("existinguser", "existing@example.com");
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "newemail@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("existinguser"))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(request, CancellationToken.None));

        TestHelpers.GetFieldFromValidationException(exception).Should().Be("Username");
        exception.Message.Should().Be("This username is already taken");

        // Verify audit logging for failed registration
        _mockAuditService.Verify(x => x.LogEventAsync(
            "REGISTRATION_FAILED",
            null,
            "existinguser",
            "127.0.0.1",
            It.IsAny<string>(),
            "Username already exists"), Times.Once);

        // Verify user creation is not attempted
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        // Arrange
        var existingUser = CreateTestUser("existinguser", "existing@example.com");
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.GetUserByEmailAsync("existing@example.com"))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(request, CancellationToken.None));

        TestHelpers.GetFieldFromValidationException(exception).Should().Be("Email");
        exception.Message.Should().Be("This email address is already registered");

        // Verify audit logging for failed registration
        _mockAuditService.Verify(x => x.LogEventAsync(
            "REGISTRATION_FAILED",
            null,
            "newuser",
            "127.0.0.1",
            It.IsAny<string>(),
            "Email already exists"), Times.Once);

        // Verify user creation is not attempted
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRegistration_HashesPasswordCorrectly()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "MySecurePassword123!",
            ConfirmPassword = "MySecurePassword123!",
            IpAddress = "127.0.0.1"
        };

        var hashedPassword = "$2a$12$uniquehashedpassword";
        var createdUser = new User { UserId = Guid.NewGuid(), Username = "newuser", Email = "newuser@example.com" };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.GetUserByEmailAsync("newuser@example.com"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.HashPasswordAsync("MySecurePassword123!"))
            .ReturnsAsync(hashedPassword);
        _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify password hashing was called with correct password
        _mockUserService.Verify(x => x.HashPasswordAsync("MySecurePassword123!"), Times.Once);

        // Verify user creation with hashed password
        _mockUserService.Verify(x => x.CreateUserAsync(It.Is<User>(u => 
            u.PasswordHash == hashedPassword)), Times.Once);
    }

    [Fact]
    public async Task Handle_UnexpectedError_ThrowsBusinessRuleExceptionAndLogsAuditEvent()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.GetUserByEmailAsync("newuser@example.com"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.HashPasswordAsync("SecurePassword123!"))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _handler.Handle(request, CancellationToken.None));

        exception.Message.Should().Be("Registration failed due to an unexpected error");
        exception.RuleCode.Should().Be("REGISTRATION_ERROR");

        // Verify audit logging for error
        _mockAuditService.Verify(x => x.LogEventAsync(
            "REGISTRATION_ERROR",
            null,
            "newuser",
            "127.0.0.1",
            It.IsAny<string>(),
            "Registration failed: Database connection failed"), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationException_IsRethrown()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            IpAddress = "127.0.0.1"
        };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("newuser"))
            .ThrowsAsync(new ValidationException("Username", "Custom validation error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(request, CancellationToken.None));

        TestHelpers.GetFieldFromValidationException(exception).Should().Be("Username");
        exception.Message.Should().Be("Custom validation error");

        // Verify audit logging is not called for validation exceptions
        _mockAuditService.Verify(x => x.LogEventAsync(
            "REGISTRATION_ERROR",
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRegistration_SetsDefaultUserProperties()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            IpAddress = "127.0.0.1"
        };

        var createdUser = new User { UserId = Guid.NewGuid(), Username = "newuser", Email = "newuser@example.com" };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.GetUserByEmailAsync("newuser@example.com"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify user creation with correct default properties
        _mockUserService.Verify(x => x.CreateUserAsync(It.Is<User>(u => 
            u.UserId != Guid.Empty &&
            u.Username == "newuser" &&
            u.Email == "newuser@example.com" &&
            u.Salt == string.Empty &&
            u.Roles == "User" &&
            u.MfaEnabled == false &&
            u.MfaSecret == null &&
            u.IsLocked == false &&
            u.LockoutEnd == null &&
            u.FailedLoginAttempts == 0 &&
            u.LastLoginAttempt == null &&
            u.CreatedAt <= DateTime.UtcNow &&
            u.UpdatedAt <= DateTime.UtcNow)), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRegistration_ChecksBothUsernameAndEmailForDuplicates()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            IpAddress = "127.0.0.1"
        };

        var createdUser = new User { UserId = Guid.NewGuid(), Username = "newuser", Email = "newuser@example.com" };

        _mockUserService.Setup(x => x.GetUserByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.GetUserByEmailAsync("newuser@example.com"))
            .ReturnsAsync((User?)null);
        _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify both username and email are checked for duplicates
        _mockUserService.Verify(x => x.GetUserByUsernameAsync("newuser"), Times.Once);
        _mockUserService.Verify(x => x.GetUserByEmailAsync("newuser@example.com"), Times.Once);
    }
}