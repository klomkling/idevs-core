namespace Idevs.Common;

/// <summary>
/// Represents the result of an operation that returns a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value returned by the operation (only accessible if successful).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result.</exception>
    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot access Value on a failed result.");
            }

            return _value!;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error that occurred (must be null if success).</param>
    /// <param name="value">The value returned by the operation (must be non-null if success).</param>
    private Result(bool isSuccess, Error? error, T? value)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value returned by the operation.</param>
    /// <returns>A successful result containing the value.</returns>
    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(true, null, value);
    }

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    /// <returns>A failed result.</returns>
    public new static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(false, error, default);
    }

    /// <summary>
    /// Deconstructs the result into its success state, value, and error.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="value">The value returned by the operation (default if failed).</param>
    /// <param name="error">The error that occurred (null if successful).</param>
    public void Deconstruct(out bool isSuccess, out T? value, out Error? error)
    {
        isSuccess = IsSuccess;
        value = IsSuccess ? _value : default;
        error = Error;
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// </summary>
    /// <param name="error">The error to wrap.</param>
    public static implicit operator Result<T>(Error error) => Failure(error);
}
