namespace Idevs.Services.Implementations;

/// <summary>
/// No-operation implementation of <see cref="IMetrics"/> that discards all metrics.
/// Used as a default when no metrics provider is configured.
/// </summary>
public sealed class NoOpMetrics : IMetrics
{
    /// <inheritdoc />
    public void IncrementCounter(string name, long increment = 1, params KeyValuePair<string, string>[] tags)
    {
        // No-op
    }

    /// <inheritdoc />
    public IDisposable StartTimer(string name, params KeyValuePair<string, string>[] tags)
    {
        return NoOpDisposable.Instance;
    }

    /// <inheritdoc />
    public void ObserveHistogram(string name, double value, params KeyValuePair<string, string>[] tags)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, string>[] tags)
    {
        // No-op
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();

        private NoOpDisposable() { }

        public void Dispose()
        {
            // No-op
        }
    }
}
