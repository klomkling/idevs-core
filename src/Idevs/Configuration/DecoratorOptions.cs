namespace Idevs.Configuration;

/// <summary>
/// Configuration options for command and query handler decorators.
/// </summary>
public sealed class DecoratorOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether logging is enabled for handlers.
    /// Default is true.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether validation is enabled for handlers.
    /// Default is true.
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics collection is enabled for handlers.
    /// Default is true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Creates a copy of these decorator options.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public DecoratorOptions Clone() => new()
    {
        EnableLogging = EnableLogging,
        EnableValidation = EnableValidation,
        EnableMetrics = EnableMetrics
    };
}
