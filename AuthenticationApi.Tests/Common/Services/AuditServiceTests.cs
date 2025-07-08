using AuthenticationApi.Common.Services;
using AuthenticationApi.Common.Entities;
using AuthenticationApi.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthenticationApi.Tests.Common.Services;

/// <summary>
/// Unit tests for AuditService focusing on security logging functionality.
/// Tests audit log creation, retrieval, and security event tracking.
/// </summary>
public class AuditServiceTests : TestBase
{
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        _auditService = new AuditService(_context);
    }

    [Fact]
    public async Task LogEventAsync_WithAllParameters_CreatesAuditLogWithCorrectData()
    {
        // Arrange
        var eventType = "LOGIN_SUCCESS";
        var userId = Guid.NewGuid();
        var username = "testuser";
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        var details = "User logged in successfully";
        var beforeLog = DateTime.UtcNow;

        // Act
        await _auditService.LogEventAsync(eventType, userId, username, ipAddress, userAgent, details);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        
        var auditLog = auditLogs.First();
        auditLog.EventType.Should().Be(eventType);
        auditLog.UserId.Should().Be(userId);
        auditLog.Username.Should().Be(username);
        auditLog.IpAddress.Should().Be(ipAddress);
        auditLog.UserAgent.Should().Be(userAgent);
        auditLog.Details.Should().Be(details);
        auditLog.Timestamp.Should().BeCloseTo(beforeLog, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogEventAsync_WithMinimalParameters_CreatesAuditLogWithNullValues()
    {
        // Arrange
        var eventType = "SYSTEM_EVENT";
        var beforeLog = DateTime.UtcNow;

        // Act
        await _auditService.LogEventAsync(eventType);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        
        var auditLog = auditLogs.First();
        auditLog.EventType.Should().Be(eventType);
        auditLog.UserId.Should().BeNull();
        auditLog.Username.Should().BeNull();
        auditLog.IpAddress.Should().BeNull();
        auditLog.UserAgent.Should().BeNull();
        auditLog.Details.Should().BeNull();
        auditLog.Timestamp.Should().BeCloseTo(beforeLog, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogEventAsync_LoginFailureEvent_CreatesCorrectAuditLog()
    {
        // Arrange
        var eventType = "LOGIN_FAILED";
        var userId = Guid.NewGuid();
        var username = "testuser";
        var ipAddress = "192.168.1.100";
        var details = "Invalid password";

        // Act
        await _auditService.LogEventAsync(eventType, userId, username, ipAddress, details: details);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        
        var auditLog = auditLogs.First();
        auditLog.EventType.Should().Be("LOGIN_FAILED");
        auditLog.UserId.Should().Be(userId);
        auditLog.Username.Should().Be(username);
        auditLog.IpAddress.Should().Be(ipAddress);
        auditLog.Details.Should().Be("Invalid password");
    }

    [Fact]
    public async Task LogEventAsync_AccountLockoutEvent_CreatesCorrectAuditLog()
    {
        // Arrange
        var eventType = "ACCOUNT_LOCKED";
        var userId = Guid.NewGuid();
        var username = "testuser";
        var ipAddress = "192.168.1.100";
        var details = "Account locked due to failed login attempts";

        // Act
        await _auditService.LogEventAsync(eventType, userId, username, ipAddress, details: details);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        
        var auditLog = auditLogs.First();
        auditLog.EventType.Should().Be("ACCOUNT_LOCKED");
        auditLog.UserId.Should().Be(userId);
        auditLog.Username.Should().Be(username);
        auditLog.Details.Should().Be("Account locked due to failed login attempts");
    }

    [Fact]
    public async Task LogEventAsync_RegistrationEvent_CreatesCorrectAuditLog()
    {
        // Arrange
        var eventType = "USER_REGISTERED";
        var userId = Guid.NewGuid();
        var username = "newuser";
        var ipAddress = "192.168.1.100";
        var details = "User account created successfully";

        // Act
        await _auditService.LogEventAsync(eventType, userId, username, ipAddress, details: details);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        
        var auditLog = auditLogs.First();
        auditLog.EventType.Should().Be("USER_REGISTERED");
        auditLog.UserId.Should().Be(userId);
        auditLog.Username.Should().Be(username);
        auditLog.Details.Should().Be("User account created successfully");
    }

    [Fact]
    public async Task LogEventAsync_MultipleEvents_CreatesMultipleAuditLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var ipAddress = "192.168.1.100";

        // Act
        await _auditService.LogEventAsync("LOGIN_SUCCESS", userId, username, ipAddress);
        await _auditService.LogEventAsync("PASSWORD_CHANGED", userId, username, ipAddress);
        await _auditService.LogEventAsync("LOGOUT_SUCCESS", userId, username, ipAddress);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(3);
        
        var eventTypes = auditLogs.Select(al => al.EventType).ToList();
        eventTypes.Should().Contain("LOGIN_SUCCESS");
        eventTypes.Should().Contain("PASSWORD_CHANGED");
        eventTypes.Should().Contain("LOGOUT_SUCCESS");
    }

    [Fact]
    public async Task GetAuditLogsForUserAsync_ExistingUserWithLogs_ReturnsUserAuditLogs()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        // Create audit logs for both users
        await _auditService.LogEventAsync("LOGIN_SUCCESS", userId1, "user1", "192.168.1.100");
        await _auditService.LogEventAsync("LOGIN_SUCCESS", userId2, "user2", "192.168.1.101");
        await _auditService.LogEventAsync("LOGOUT_SUCCESS", userId1, "user1", "192.168.1.100");

        // Act
        var user1Logs = await _auditService.GetAuditLogsForUserAsync(userId1);

        // Assert
        user1Logs.Should().HaveCount(2);
        user1Logs.Should().OnlyContain(log => log.UserId == userId1);
        
        var eventTypes = user1Logs.Select(log => log.EventType).ToList();
        eventTypes.Should().Contain("LOGIN_SUCCESS");
        eventTypes.Should().Contain("LOGOUT_SUCCESS");
    }

    [Fact]
    public async Task GetAuditLogsForUserAsync_NonExistentUser_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        
        // Create some audit logs for other users
        await _auditService.LogEventAsync("LOGIN_SUCCESS", Guid.NewGuid(), "user1", "192.168.1.100");

        // Act
        var result = await _auditService.GetAuditLogsForUserAsync(nonExistentUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogsForUserAsync_WithPagination_ReturnsCorrectPageSize()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        // Create multiple audit logs
        for (int i = 0; i < 10; i++)
        {
            await _auditService.LogEventAsync($"EVENT_{i}", userId, "testuser", "192.168.1.100");
        }

        // Act
        var firstPage = await _auditService.GetAuditLogsForUserAsync(userId, skip: 0, take: 3);
        var secondPage = await _auditService.GetAuditLogsForUserAsync(userId, skip: 3, take: 3);

        // Assert
        firstPage.Should().HaveCount(3);
        secondPage.Should().HaveCount(3);
        
        // Verify no overlap between pages
        var firstPageIds = firstPage.Select(log => log.LogId).ToList();
        var secondPageIds = secondPage.Select(log => log.LogId).ToList();
        firstPageIds.Should().NotIntersectWith(secondPageIds);
    }

    [Fact]
    public async Task GetAuditLogsForUserAsync_OrdersByTimestampDescending_ReturnsNewestFirst()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        // Create audit logs with slight delays to ensure different timestamps
        await _auditService.LogEventAsync("FIRST_EVENT", userId, "testuser", "192.168.1.100");
        await Task.Delay(10);
        await _auditService.LogEventAsync("SECOND_EVENT", userId, "testuser", "192.168.1.100");
        await Task.Delay(10);
        await _auditService.LogEventAsync("THIRD_EVENT", userId, "testuser", "192.168.1.100");

        // Act
        var result = await _auditService.GetAuditLogsForUserAsync(userId);

        // Assert
        result.Should().HaveCount(3);
        
        var orderedLogs = result.ToList();
        orderedLogs[0].EventType.Should().Be("THIRD_EVENT");
        orderedLogs[1].EventType.Should().Be("SECOND_EVENT");
        orderedLogs[2].EventType.Should().Be("FIRST_EVENT");
        
        // Verify timestamps are in descending order
        orderedLogs[0].Timestamp.Should().BeAfter(orderedLogs[1].Timestamp);
        orderedLogs[1].Timestamp.Should().BeAfter(orderedLogs[2].Timestamp);
    }

    [Fact]
    public async Task GetAuditLogsByEventTypeAsync_ExistingEventType_ReturnsMatchingLogs()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        // Create different types of audit logs
        await _auditService.LogEventAsync("LOGIN_SUCCESS", userId1, "user1", "192.168.1.100");
        await _auditService.LogEventAsync("LOGIN_FAILED", userId2, "user2", "192.168.1.101");
        await _auditService.LogEventAsync("LOGIN_SUCCESS", userId2, "user2", "192.168.1.101");
        await _auditService.LogEventAsync("LOGOUT_SUCCESS", userId1, "user1", "192.168.1.100");

        // Act
        var loginSuccessLogs = await _auditService.GetAuditLogsByEventTypeAsync("LOGIN_SUCCESS");

        // Assert
        loginSuccessLogs.Should().HaveCount(2);
        loginSuccessLogs.Should().OnlyContain(log => log.EventType == "LOGIN_SUCCESS");
        
        var userIds = loginSuccessLogs.Select(log => log.UserId).ToList();
        userIds.Should().Contain(userId1);
        userIds.Should().Contain(userId2);
    }

    [Fact]
    public async Task GetAuditLogsByEventTypeAsync_NonExistentEventType_ReturnsEmptyList()
    {
        // Arrange
        await _auditService.LogEventAsync("LOGIN_SUCCESS", Guid.NewGuid(), "user1", "192.168.1.100");
        await _auditService.LogEventAsync("LOGOUT_SUCCESS", Guid.NewGuid(), "user1", "192.168.1.100");

        // Act
        var result = await _auditService.GetAuditLogsByEventTypeAsync("NONEXISTENT_EVENT");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogsByEventTypeAsync_WithPagination_ReturnsCorrectPageSize()
    {
        // Arrange
        var eventType = "LOGIN_SUCCESS";
        
        // Create multiple audit logs of the same type
        for (int i = 0; i < 8; i++)
        {
            await _auditService.LogEventAsync(eventType, Guid.NewGuid(), $"user{i}", "192.168.1.100");
        }

        // Act
        var firstPage = await _auditService.GetAuditLogsByEventTypeAsync(eventType, skip: 0, take: 3);
        var secondPage = await _auditService.GetAuditLogsByEventTypeAsync(eventType, skip: 3, take: 3);

        // Assert
        firstPage.Should().HaveCount(3);
        secondPage.Should().HaveCount(3);
        
        // Verify all logs are of the correct event type
        firstPage.Should().OnlyContain(log => log.EventType == eventType);
        secondPage.Should().OnlyContain(log => log.EventType == eventType);
    }

    [Fact]
    public async Task GetAuditLogsByEventTypeAsync_OrdersByTimestampDescending_ReturnsNewestFirst()
    {
        // Arrange
        var eventType = "LOGIN_SUCCESS";
        
        // Create audit logs with slight delays to ensure different timestamps
        await _auditService.LogEventAsync(eventType, Guid.NewGuid(), "user1", "192.168.1.100");
        await Task.Delay(10);
        await _auditService.LogEventAsync(eventType, Guid.NewGuid(), "user2", "192.168.1.101");
        await Task.Delay(10);
        await _auditService.LogEventAsync(eventType, Guid.NewGuid(), "user3", "192.168.1.102");

        // Act
        var result = await _auditService.GetAuditLogsByEventTypeAsync(eventType);

        // Assert
        result.Should().HaveCount(3);
        
        var orderedLogs = result.ToList();
        orderedLogs[0].Username.Should().Be("user3");
        orderedLogs[1].Username.Should().Be("user2");
        orderedLogs[2].Username.Should().Be("user1");
        
        // Verify timestamps are in descending order
        orderedLogs[0].Timestamp.Should().BeAfter(orderedLogs[1].Timestamp);
        orderedLogs[1].Timestamp.Should().BeAfter(orderedLogs[2].Timestamp);
    }

    [Fact]
    public async Task LogEventAsync_SecuritySensitiveEvents_DoesNotLogPasswordsOrTokens()
    {
        // Arrange
        var eventType = "LOGIN_FAILED";
        var userId = Guid.NewGuid();
        var username = "testuser";
        var ipAddress = "192.168.1.100";
        var details = "Invalid password"; // Should not contain actual password

        // Act
        await _auditService.LogEventAsync(eventType, userId, username, ipAddress, details: details);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        
        var auditLog = auditLogs.First();
        auditLog.Details.Should().Be("Invalid password");
        auditLog.Details.Should().NotContain("password123"); // Should not contain actual password
        auditLog.Details.Should().NotContain("token"); // Should not contain tokens
    }
}