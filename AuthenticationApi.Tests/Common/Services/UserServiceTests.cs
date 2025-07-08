using AuthenticationApi.Common.Services;
using AuthenticationApi.Common.Entities;
using AuthenticationApi.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using BCrypt.Net;

namespace AuthenticationApi.Tests.Common.Services;

/// <summary>
/// Unit tests for UserService focusing on password security and user management.
/// Tests password hashing, validation, account lockout, and MFA operations.
/// </summary>
public class UserServiceTests : TestBase
{
    private readonly UserService _userService;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public UserServiceTests()
    {
        _dataProtectionProvider = _serviceProvider.GetRequiredService<IDataProtectionProvider>();
        _userService = new UserService(_context, _dataProtectionProvider);
    }

    [Fact]
    public async Task HashPasswordAsync_ValidPassword_ReturnsHashedPasswordWithBCrypt()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hashedPassword = await _userService.HashPasswordAsync(password);

        // Assert
        hashedPassword.Should().NotBeNull();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Should().StartWith("$2a$12$"); // BCrypt format with 12 rounds
        
        // Verify the hash can be verified
        var isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsync_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = await _userService.HashPasswordAsync(password);

        // Act
        var isValid = await _userService.ValidatePasswordAsync(password, hashedPassword);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsync_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "SecurePassword123!";
        var incorrectPassword = "WrongPassword123!";
        var hashedPassword = await _userService.HashPasswordAsync(correctPassword);

        // Act
        var isValid = await _userService.ValidatePasswordAsync(incorrectPassword, hashedPassword);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserAsync_ValidUser_CreatesUserWithTimestamps()
    {
        // Arrange
        var user = CreateTestUser("newuser", "newuser@example.com");
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await _userService.CreateUserAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.UserId);
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("newuser@example.com");
        result.CreatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(5));

        // Verify user was saved to database
        var savedUser = await _context.Users.FindAsync(user.UserId);
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be("newuser");
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");

        // Act
        var result = await _userService.GetUserByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetUserByUsernameAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByUsernameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");

        // Act
        var result = await _userService.GetUserByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task IsUserLockedAsync_UnlockedUser_ReturnsFalse()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", isLocked: false);

        // Act
        var result = await _userService.IsUserLockedAsync(user.UserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserLockedAsync_LockedUserWithActiveLockout_ReturnsTrue()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", isLocked: true);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.IsUserLockedAsync(user.UserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserLockedAsync_LockedUserWithExpiredLockout_ReturnsFalse()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", isLocked: true);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(-5); // Expired 5 minutes ago
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.IsUserLockedAsync(user.UserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LockUserAsync_ValidUser_LocksUserWithCorrectLockoutEnd()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var lockoutDuration = TimeSpan.FromMinutes(30);
        var beforeLockout = DateTime.UtcNow;

        // Act
        await _userService.LockUserAsync(user.UserId, lockoutDuration);

        // Assert
        var lockedUser = await _context.Users.FindAsync(user.UserId);
        lockedUser.Should().NotBeNull();
        lockedUser!.IsLocked.Should().BeTrue();
        lockedUser.LockoutEnd.Should().BeCloseTo(beforeLockout.Add(lockoutDuration), TimeSpan.FromSeconds(5));
        lockedUser.UpdatedAt.Should().BeCloseTo(beforeLockout, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UnlockUserAsync_LockedUser_UnlocksUserAndResetsFailedAttempts()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", isLocked: true, failedLoginAttempts: 5);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        await _context.SaveChangesAsync();

        // Act
        await _userService.UnlockUserAsync(user.UserId);

        // Assert
        var unlockedUser = await _context.Users.FindAsync(user.UserId);
        unlockedUser.Should().NotBeNull();
        unlockedUser!.IsLocked.Should().BeFalse();
        unlockedUser.LockoutEnd.Should().BeNull();
        unlockedUser.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task IncrementFailedLoginAttemptAsync_ValidUser_IncrementsFailedAttempts()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", failedLoginAttempts: 2);
        var beforeIncrement = DateTime.UtcNow;

        // Act
        await _userService.IncrementFailedLoginAttemptAsync(user.UserId);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.FailedLoginAttempts.Should().Be(3);
        updatedUser.LastLoginAttempt.Should().BeCloseTo(beforeIncrement, TimeSpan.FromSeconds(5));
        updatedUser.UpdatedAt.Should().BeCloseTo(beforeIncrement, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ResetFailedLoginAttemptsAsync_ValidUser_ResetsToZero()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", failedLoginAttempts: 3);
        var beforeReset = DateTime.UtcNow;

        // Act
        await _userService.ResetFailedLoginAttemptsAsync(user.UserId);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.FailedLoginAttempts.Should().Be(0);
        updatedUser.LastLoginAttempt.Should().BeCloseTo(beforeReset, TimeSpan.FromSeconds(5));
        updatedUser.UpdatedAt.Should().BeCloseTo(beforeReset, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SetMfaSecretAsync_ValidUser_EncryptsAndStoresMfaSecret()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var mfaSecret = "JBSWY3DPEHPK3PXP";

        // Act
        await _userService.SetMfaSecretAsync(user.UserId, mfaSecret);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.MfaSecret.Should().NotBeNull();
        updatedUser.MfaSecret.Should().NotBe(mfaSecret); // Should be encrypted
    }

    [Fact]
    public async Task GetMfaSecretAsync_UserWithMfaSecret_ReturnsDecryptedSecret()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var originalSecret = "JBSWY3DPEHPK3PXP";
        await _userService.SetMfaSecretAsync(user.UserId, originalSecret);

        // Act
        var retrievedSecret = await _userService.GetMfaSecretAsync(user.UserId);

        // Assert
        retrievedSecret.Should().Be(originalSecret);
    }

    [Fact]
    public async Task GetMfaSecretAsync_UserWithoutMfaSecret_ReturnsNull()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");

        // Act
        var retrievedSecret = await _userService.GetMfaSecretAsync(user.UserId);

        // Assert
        retrievedSecret.Should().BeNull();
    }

    [Fact]
    public async Task EnableMfaAsync_ValidUser_EnablesMfaAndStoresSecret()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", mfaEnabled: false);
        var mfaSecret = "JBSWY3DPEHPK3PXP";

        // Act
        await _userService.EnableMfaAsync(user.UserId, mfaSecret);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.MfaEnabled.Should().BeTrue();
        updatedUser.MfaSecret.Should().NotBeNull();
        updatedUser.MfaSecret.Should().NotBe(mfaSecret); // Should be encrypted
    }

    [Fact]
    public async Task DisableMfaAsync_UserWithMfa_DisablesMfaAndClearsSecret()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", mfaEnabled: true);
        await _userService.SetMfaSecretAsync(user.UserId, "JBSWY3DPEHPK3PXP");

        // Act
        await _userService.DisableMfaAsync(user.UserId);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.MfaEnabled.Should().BeFalse();
        updatedUser.MfaSecret.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePasswordAsync_ValidUser_UpdatesPasswordAndClearsLockout()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com", isLocked: true, failedLoginAttempts: 5);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        user.PasswordResetToken = "reset-token";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _context.SaveChangesAsync();

        var originalPasswordHash = user.PasswordHash;
        var newPassword = "NewSecurePassword123!";

        // Act
        await _userService.UpdatePasswordAsync(user.UserId, newPassword);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordHash.Should().NotBe(originalPasswordHash);
        updatedUser.IsLocked.Should().BeFalse();
        updatedUser.LockoutEnd.Should().BeNull();
        updatedUser.FailedLoginAttempts.Should().Be(0);
        updatedUser.PasswordResetToken.Should().BeNull();
        updatedUser.PasswordResetTokenExpiry.Should().BeNull();

        // Verify new password works
        var isValid = await _userService.ValidatePasswordAsync(newPassword, updatedUser.PasswordHash);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task SetPasswordResetTokenAsync_ValidUser_SetsTokenAndExpiry()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var token = "reset-token-123";
        var expiry = DateTime.UtcNow.AddMinutes(15);

        // Act
        await _userService.SetPasswordResetTokenAsync(user.UserId, token, expiry);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordResetToken.Should().Be(token);
        updatedUser.PasswordResetTokenExpiry.Should().Be(expiry);
    }

    [Fact]
    public async Task GetUserByPasswordResetTokenAsync_ValidToken_ReturnsUser()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var token = "reset-token-123";
        var expiry = DateTime.UtcNow.AddMinutes(15);
        await _userService.SetPasswordResetTokenAsync(user.UserId, token, expiry);

        // Act
        var result = await _userService.GetUserByPasswordResetTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetUserByPasswordResetTokenAsync_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var token = "reset-token-123";
        var expiry = DateTime.UtcNow.AddMinutes(-5); // Expired 5 minutes ago
        await _userService.SetPasswordResetTokenAsync(user.UserId, token, expiry);

        // Act
        var result = await _userService.GetUserByPasswordResetTokenAsync(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HashPasswordAsync_MultiplePasswords_GeneratesUniqueHashes()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash1 = await _userService.HashPasswordAsync(password);
        var hash2 = await _userService.HashPasswordAsync(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt should generate unique salts
        
        // But both should validate correctly
        var isValid1 = await _userService.ValidatePasswordAsync(password, hash1);
        var isValid2 = await _userService.ValidatePasswordAsync(password, hash2);
        
        isValid1.Should().BeTrue();
        isValid2.Should().BeTrue();
    }
}