namespace Idevs.Common;

/// <summary>
/// Defines the types of errors that can occur in the application.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Validation error - input data does not meet requirements.
    /// </summary>
    Validation,

    /// <summary>
    /// Not found error - requested resource does not exist.
    /// </summary>
    NotFound,

    /// <summary>
    /// Conflict error - operation conflicts with current state.
    /// </summary>
    Conflict,

    /// <summary>
    /// Unauthorized error - authentication is required.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Forbidden error - insufficient permissions.
    /// </summary>
    Forbidden,

    /// <summary>
    /// Business logic failure - operation failed due to business rules.
    /// </summary>
    Failure,

    /// <summary>
    /// Unexpected error - unhandled exception or system error.
    /// </summary>
    Unexpected
}
