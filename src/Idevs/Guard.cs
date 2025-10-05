using System.Diagnostics.CodeAnalysis;

namespace Idevs;

/// <summary>
/// Provides guard methods for argument validation.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Ensures the value is not null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="parameterName">The name of the parameter being checked.</param>
    /// <returns>The original value if it is not null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static T NotNull<T>([NotNull] T? value, string parameterName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        return value;
    }

    /// <summary>
    /// Ensures the string is not null or whitespace.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="parameterName">The name of the parameter being checked.</param>
    /// <returns>The original string if it is not null or whitespace.</returns>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    public static string NotNullOrWhiteSpace([NotNull] string? value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
