using System.Collections.ObjectModel;

namespace Idevs.Common;

/// <summary>
/// Represents an error that occurred during an operation.
/// </summary>
public sealed record Error
{
    /// <summary>
    /// Gets the error code (e.g., "VALIDATION_001", "NOT_FOUND_404").
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error type classification.
    /// </summary>
    public required ErrorType Type { get; init; }

    /// <summary>
    /// Gets additional error details (e.g., field-level validation errors).
    /// Key: field or property name, Value: list of error messages for that field.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Details { get; init; }

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <param name="details">Optional field-level details.</param>
    /// <returns>A validation error.</returns>
    public static Error Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details = null)
    {
        return new Error
        {
            Code = code,
            Message = message,
            Type = ErrorType.Validation,
            Details = details
        };
    }

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A not found error.</returns>
    public static Error NotFound(string code, string message)
    {
        return new Error
        {
            Code = code,
            Message = message,
            Type = ErrorType.NotFound
        };
    }

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A conflict error.</returns>
    public static Error Conflict(string code, string message)
    {
        return new Error
        {
            Code = code,
            Message = message,
            Type = ErrorType.Conflict
        };
    }

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>An unauthorized error.</returns>
    public static Error Unauthorized(string code, string message)
    {
        return new Error
        {
            Code = code,
            Message = message,
            Type = ErrorType.Unauthorized
        };
    }

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A forbidden error.</returns>
    public static Error Forbidden(string code, string message)
    {
        return new Error
        {
            Code = code,
            Message = message,
            Type = ErrorType.Forbidden
        };
    }

    /// <summary>
    /// Creates a business failure error.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A failure error.</returns>
    public static Error Failure(string code, string message)
    {
        return new Error
        {
            Code = code,
            Message = message,
            Type = ErrorType.Failure
        };
    }

    /// <summary>
    /// Creates an unexpected error.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>An unexpected error.</returns>
    public static Error Unexpected(string code, string message)
    {
        return new Error
        {
            Code = code,
            Message = message,
            Type = ErrorType.Unexpected
        };
    }
}
