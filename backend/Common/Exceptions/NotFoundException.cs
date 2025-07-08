namespace AuthenticationApi.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested resource cannot be found.
/// Used for scenarios like user not found, session not found, etc.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the NotFoundException class.
    /// </summary>
    /// <param name="message">User-friendly error message describing what was not found</param>
    public NotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the NotFoundException class.
    /// </summary>
    /// <param name="message">User-friendly error message describing what was not found</param>
    /// <param name="innerException">The exception that caused this exception</param>
    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the NotFoundException class for a specific resource type and identifier.
    /// </summary>
    /// <param name="resourceType">The type of resource that was not found (e.g., "User", "Session")</param>
    /// <param name="identifier">The identifier that was used to search for the resource</param>
    public NotFoundException(string resourceType, string identifier)
        : base($"{resourceType} with identifier '{identifier}' was not found")
    {
    }
}