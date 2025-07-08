namespace AuthenticationApi.Common.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// Used for scenarios like business logic constraints, domain rule violations, etc.
/// </summary>
public class BusinessRuleException : Exception
{
    /// <summary>
    /// Gets the business rule code that was violated.
    /// </summary>
    public string? RuleCode { get; }

    /// <summary>
    /// Initializes a new instance of the BusinessRuleException class.
    /// </summary>
    /// <param name="message">User-friendly error message describing the business rule violation</param>
    public BusinessRuleException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the BusinessRuleException class.
    /// </summary>
    /// <param name="message">User-friendly error message describing the business rule violation</param>
    /// <param name="innerException">The exception that caused this exception</param>
    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the BusinessRuleException class with a specific rule code.
    /// </summary>
    /// <param name="ruleCode">The code identifying the specific business rule that was violated</param>
    /// <param name="message">User-friendly error message describing the business rule violation</param>
    public BusinessRuleException(string ruleCode, string message) : base(message)
    {
        RuleCode = ruleCode;
    }

    /// <summary>
    /// Initializes a new instance of the BusinessRuleException class with a specific rule code.
    /// </summary>
    /// <param name="ruleCode">The code identifying the specific business rule that was violated</param>
    /// <param name="message">User-friendly error message describing the business rule violation</param>
    /// <param name="innerException">The exception that caused this exception</param>
    public BusinessRuleException(string ruleCode, string message, Exception innerException) : base(message, innerException)
    {
        RuleCode = ruleCode;
    }
}