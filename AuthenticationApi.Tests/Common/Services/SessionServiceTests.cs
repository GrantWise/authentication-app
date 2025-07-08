using AuthenticationApi.Common.Services;
using AuthenticationApi.Common.Entities;
using AuthenticationApi.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthenticationApi.Tests.Common.Services;

/// <summary>
/// Unit tests for SessionService focusing on session lifecycle and security.
/// Tests session creation, validation, revocation, and cleanup operations.
/// </summary>
public class SessionServiceTests : TestBase
{
    private readonly SessionService _sessionService;

    public SessionServiceTests()
    {
        _sessionService = new SessionService(_context);
    }

    [Fact]
    public async Task CreateSessionAsync_ValidParameters_CreatesSessionWithCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jti = "test-jti-123";
        var deviceInfo = "iPhone 14 Pro";
        var ipAddress = "192.168.1.100";
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await _sessionService.CreateSessionAsync(userId, jti, deviceInfo, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.RefreshTokenJti.Should().Be(jti);
        result.DeviceInfo.Should().Be(deviceInfo);
        result.IpAddress.Should().Be(ipAddress);
        result.CreatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(5));
        result.ExpiresAt.Should().BeCloseTo(beforeCreate.AddMinutes(60), TimeSpan.FromSeconds(5));

        // Verify session was saved to database
        var savedSession = await _context.ActiveSessions.FirstOrDefaultAsync(s => s.RefreshTokenJti == jti);
        savedSession.Should().NotBeNull();
        savedSession!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetSessionByJtiAsync_ExistingSession_ReturnsSessionWithUser()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var jti = "test-jti-123";
        var session = await SeedTestSessionAsync(user.UserId, jti);

        // Act
        var result = await _sessionService.GetSessionByJtiAsync(jti);

        // Assert
        result.Should().NotBeNull();
        result!.RefreshTokenJti.Should().Be(jti);
        result.UserId.Should().Be(user.UserId);
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetSessionByJtiAsync_NonExistentSession_ReturnsNull()
    {
        // Act
        var result = await _sessionService.GetSessionByJtiAsync("nonexistent-jti");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSessionsForUserAsync_UserWithActiveSessions_ReturnsActiveSessionsOnly()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        
        // Create active session
        var activeSession = await SeedTestSessionAsync(user.UserId, "active-jti");
        
        // Create expired session
        var expiredSession = await SeedTestSessionAsync(user.UserId, "expired-jti");
        expiredSession.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.GetActiveSessionsForUserAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().RefreshTokenJti.Should().Be("active-jti");
    }

    [Fact]
    public async Task GetActiveSessionsForUserAsync_UserWithNoActiveSessions_ReturnsEmptyList()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");

        // Act
        var result = await _sessionService.GetActiveSessionsForUserAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSessionsForUserAsync_MultipleActiveSessions_ReturnsOrderedByCreatedDate()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        
        // Create sessions with different creation times
        var session1 = await SeedTestSessionAsync(user.UserId, "jti-1");
        
        // Wait a bit to ensure different creation times
        await Task.Delay(10);
        var session2 = await SeedTestSessionAsync(user.UserId, "jti-2");
        
        await Task.Delay(10);
        var session3 = await SeedTestSessionAsync(user.UserId, "jti-3");

        // Act
        var result = await _sessionService.GetActiveSessionsForUserAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        // Should be ordered by CreatedAt descending (newest first)
        var sessionList = result.ToList();
        sessionList[0].RefreshTokenJti.Should().Be("jti-3");
        sessionList[1].RefreshTokenJti.Should().Be("jti-2");
        sessionList[2].RefreshTokenJti.Should().Be("jti-1");
    }

    [Fact]
    public async Task IsSessionActiveAsync_ActiveSession_ReturnsTrue()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var jti = "active-jti";
        var session = await SeedTestSessionAsync(user.UserId, jti);

        // Act
        var result = await _sessionService.IsSessionActiveAsync(jti);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSessionActiveAsync_ExpiredSession_ReturnsFalse()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var jti = "expired-jti";
        var session = await SeedTestSessionAsync(user.UserId, jti);
        
        // Make session expired
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.IsSessionActiveAsync(jti);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSessionActiveAsync_NonExistentSession_ReturnsFalse()
    {
        // Act
        var result = await _sessionService.IsSessionActiveAsync("nonexistent-jti");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeSessionAsync_ExistingSession_RemovesSessionFromDatabase()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        var jti = "revoke-jti";
        var session = await SeedTestSessionAsync(user.UserId, jti);

        // Verify session exists
        var existingSession = await _context.ActiveSessions.FirstOrDefaultAsync(s => s.RefreshTokenJti == jti);
        existingSession.Should().NotBeNull();

        // Act
        await _sessionService.RevokeSessionAsync(jti);

        // Assert
        var revokedSession = await _context.ActiveSessions.FirstOrDefaultAsync(s => s.RefreshTokenJti == jti);
        revokedSession.Should().BeNull();
    }

    [Fact]
    public async Task RevokeSessionAsync_NonExistentSession_DoesNotThrowException()
    {
        // Act & Assert
        var act = async () => await _sessionService.RevokeSessionAsync("nonexistent-jti");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAllSessionsForUserAsync_UserWithMultipleSessions_RemovesAllUserSessions()
    {
        // Arrange
        var user1 = await SeedTestUserAsync("user1", "user1@example.com");
        var user2 = await SeedTestUserAsync("user2", "user2@example.com");
        
        // Create sessions for both users
        await SeedTestSessionAsync(user1.UserId, "user1-jti-1");
        await SeedTestSessionAsync(user1.UserId, "user1-jti-2");
        await SeedTestSessionAsync(user2.UserId, "user2-jti-1");

        // Verify sessions exist
        var initialSessions = await _context.ActiveSessions.ToListAsync();
        initialSessions.Should().HaveCount(3);

        // Act
        await _sessionService.RevokeAllSessionsForUserAsync(user1.UserId);

        // Assert
        var remainingSessions = await _context.ActiveSessions.ToListAsync();
        remainingSessions.Should().HaveCount(1);
        remainingSessions.First().RefreshTokenJti.Should().Be("user2-jti-1");
        remainingSessions.First().UserId.Should().Be(user2.UserId);
    }

    [Fact]
    public async Task RevokeAllSessionsForUserAsync_UserWithNoSessions_DoesNotThrowException()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");

        // Act & Assert
        var act = async () => await _sessionService.RevokeAllSessionsForUserAsync(user.UserId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_MixedSessions_RemovesOnlyExpiredSessions()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        
        // Create active session
        var activeSession = await SeedTestSessionAsync(user.UserId, "active-jti");
        
        // Create expired sessions
        var expiredSession1 = await SeedTestSessionAsync(user.UserId, "expired-jti-1");
        expiredSession1.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);
        
        var expiredSession2 = await SeedTestSessionAsync(user.UserId, "expired-jti-2");
        expiredSession2.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
        
        await _context.SaveChangesAsync();

        // Verify initial state
        var initialSessions = await _context.ActiveSessions.ToListAsync();
        initialSessions.Should().HaveCount(3);

        // Act
        await _sessionService.CleanupExpiredSessionsAsync();

        // Assert
        var remainingSessions = await _context.ActiveSessions.ToListAsync();
        remainingSessions.Should().HaveCount(1);
        remainingSessions.First().RefreshTokenJti.Should().Be("active-jti");
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_NoExpiredSessions_DoesNotRemoveAnything()
    {
        // Arrange
        var user = await SeedTestUserAsync("testuser", "test@example.com");
        await SeedTestSessionAsync(user.UserId, "active-jti-1");
        await SeedTestSessionAsync(user.UserId, "active-jti-2");

        // Verify initial state
        var initialSessions = await _context.ActiveSessions.ToListAsync();
        initialSessions.Should().HaveCount(2);

        // Act
        await _sessionService.CleanupExpiredSessionsAsync();

        // Assert
        var remainingSessions = await _context.ActiveSessions.ToListAsync();
        remainingSessions.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateSessionAsync_ExpirationTime_SetsCorrectExpirationTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jti = "test-jti";
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await _sessionService.CreateSessionAsync(userId, jti, "device", "ip");

        // Assert
        result.ExpiresAt.Should().BeCloseTo(beforeCreate.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateSessionAsync_NullDeviceInfo_HandlesNullValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jti = "test-jti";

        // Act
        var result = await _sessionService.CreateSessionAsync(userId, jti, null, null);

        // Assert
        result.Should().NotBeNull();
        result.DeviceInfo.Should().BeNull();
        result.IpAddress.Should().BeNull();
        result.UserId.Should().Be(userId);
        result.RefreshTokenJti.Should().Be(jti);
    }
}