namespace Idevs.Configuration;

/// <summary>
/// Configuration options for the Idevs framework.
/// </summary>
public sealed class IdevsOptions
{
    /// <summary>
    /// Gets or sets the decorator configuration options.
    /// </summary>
    public DecoratorOptions Decorators { get; set; } = new();

    /// <summary>
    /// Gets or sets the service name for telemetry and logging.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for telemetry and logging.
    /// </summary>
    public string? ServiceVersion { get; set; }
}
