namespace AuthenticationApi.Common.Exceptions;

/// <summary>
/// Exception thrown when business rule validation fails.
/// Used for scenarios like invalid input data, business constraint violations, etc.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the validation errors that occurred.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationException class.
    /// </summary>
    /// <param name="message">User-friendly error message describing the validation failure</param>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the ValidationException class.
    /// </summary>
    /// <param name="message">User-friendly error message describing the validation failure</param>
    /// <param name="innerException">The exception that caused this exception</param>
    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the ValidationException class with detailed validation errors.
    /// </summary>
    /// <param name="errors">Dictionary of field names and their associated error messages</param>
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Initializes a new instance of the ValidationException class for a single field validation error.
    /// </summary>
    /// <param name="field">The field name that failed validation</param>
    /// <param name="error">The validation error message</param>
    public ValidationException(string field, string error)
        : base($"Validation failed for field '{field}': {error}")
    {
        Errors = new Dictionary<string, string[]>
        {
            [field] = new[] { error }
        };
    }
}