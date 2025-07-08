using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace AuthenticationApi.Common.Services;

/// <summary>
/// Service for implementing rate limiting functionality for authentication endpoints.
/// Tracks login attempts per username and IP address to prevent brute force attacks.
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// Checks if a login attempt should be allowed for the given username.
    /// </summary>
    /// <param name="username">The username attempting to login</param>
    /// <param name="ipAddress">The IP address of the login attempt</param>
    /// <returns>True if the attempt should be allowed, false if rate limited</returns>
    Task<bool> IsLoginAttemptAllowedAsync(string username, string? ipAddress);

    /// <summary>
    /// Records a login attempt for rate limiting tracking.
    /// </summary>
    /// <param name="username">The username that attempted to login</param>
    /// <param name="ipAddress">The IP address of the login attempt</param>
    /// <param name="successful">Whether the login attempt was successful</param>
    Task RecordLoginAttemptAsync(string username, string? ipAddress, bool successful);

    /// <summary>
    /// Gets the remaining attempts before rate limiting kicks in.
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <returns>The number of remaining attempts</returns>
    Task<int> GetRemainingAttemptsAsync(string username);
}

/// <summary>
/// Implementation of rate limiting service using in-memory caching.
/// Configured for 5 attempts per username per 15 minutes as per technical specification.
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly int _maxAttempts;
    private readonly TimeSpan _windowDuration;

    /// <summary>
    /// Initializes a new instance of the RateLimitingService class.
    /// </summary>
    /// <param name="cache">Memory cache for storing rate limiting data</param>
    /// <param name="configuration">Application configuration</param>
    public RateLimitingService(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _configuration = configuration;
        
        // Get configuration values with defaults matching technical specification
        _maxAttempts = _configuration.GetValue<int>("RateLimit:MaxLoginAttempts", 5);
        _windowDuration = TimeSpan.FromMinutes(_configuration.GetValue<int>("RateLimit:WindowMinutes", 15));
    }

    /// <summary>
    /// Checks if a login attempt should be allowed for the given username.
    /// </summary>
    /// <param name="username">The username attempting to login</param>
    /// <param name="ipAddress">The IP address of the login attempt</param>
    /// <returns>True if the attempt should be allowed, false if rate limited</returns>
    public async Task<bool> IsLoginAttemptAllowedAsync(string username, string? ipAddress)
    {
        await Task.CompletedTask; // Make async for consistency

        var key = GetRateLimitKey(username);
        
        if (_cache.TryGetValue(key, out RateLimitData? data) && data != null)
        {
            // Clean up old attempts outside the window
            data.Attempts.RemoveAll(attempt => DateTime.UtcNow - attempt.Timestamp > _windowDuration);
            
            // Check if we've exceeded the limit
            return data.Attempts.Count < _maxAttempts;
        }

        // No previous attempts, allow this one
        return true;
    }

    /// <summary>
    /// Records a login attempt for rate limiting tracking.
    /// </summary>
    /// <param name="username">The username that attempted to login</param>
    /// <param name="ipAddress">The IP address of the login attempt</param>
    /// <param name="successful">Whether the login attempt was successful</param>
    public async Task RecordLoginAttemptAsync(string username, string? ipAddress, bool successful)
    {
        await Task.CompletedTask; // Make async for consistency

        var key = GetRateLimitKey(username);
        
        var data = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _windowDuration.Add(TimeSpan.FromMinutes(5)); // Extra buffer
            return new RateLimitData();
        });

        if (data != null)
        {
            // Clean up old attempts
            data.Attempts.RemoveAll(attempt => DateTime.UtcNow - attempt.Timestamp > _windowDuration);
            
            // Record this attempt
            data.Attempts.Add(new LoginAttempt
            {
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                Successful = successful
            });

            // If login was successful, we could optionally reset the attempts
            // For now, we'll let them expire naturally for audit purposes
        }
    }

    /// <summary>
    /// Gets the remaining attempts before rate limiting kicks in.
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <returns>The number of remaining attempts</returns>
    public async Task<int> GetRemainingAttemptsAsync(string username)
    {
        await Task.CompletedTask; // Make async for consistency

        var key = GetRateLimitKey(username);
        
        if (_cache.TryGetValue(key, out RateLimitData? data) && data != null)
        {
            // Clean up old attempts
            data.Attempts.RemoveAll(attempt => DateTime.UtcNow - attempt.Timestamp > _windowDuration);
            
            return Math.Max(0, _maxAttempts - data.Attempts.Count);
        }

        return _maxAttempts;
    }

    /// <summary>
    /// Generates a cache key for rate limiting based on username.
    /// </summary>
    /// <param name="username">The username</param>
    /// <returns>The cache key</returns>
    private static string GetRateLimitKey(string username)
    {
        return $"rate_limit:login:{username.ToLowerInvariant()}";
    }
}

/// <summary>
/// Data structure for tracking rate limiting information.
/// </summary>
internal class RateLimitData
{
    /// <summary>
    /// Gets or sets the list of login attempts within the rate limiting window.
    /// </summary>
    public List<LoginAttempt> Attempts { get; set; } = new();
}

/// <summary>
/// Represents a single login attempt for rate limiting tracking.
/// </summary>
internal class LoginAttempt
{
    /// <summary>
    /// Gets or sets the timestamp of the login attempt.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the login attempt.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets whether the login attempt was successful.
    /// </summary>
    public bool Successful { get; set; }
}