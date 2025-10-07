namespace Idevs.Services;

/// <summary>
/// Provides methods for recording application metrics.
/// </summary>
public interface IMetrics
{
    /// <summary>
    /// Increments a counter metric.
    /// </summary>
    /// <param name="name">The name of the counter.</param>
    /// <param name="increment">The amount to increment by (default is 1).</param>
    /// <param name="tags">Optional tags for the metric.</param>
    void IncrementCounter(string name, long increment = 1, params KeyValuePair<string, string>[] tags);

    /// <summary>
    /// Starts a timer to measure duration. Dispose the returned object to stop the timer.
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="tags">Optional tags for the metric.</param>
    /// <returns>An IDisposable that stops the timer when disposed.</returns>
    IDisposable StartTimer(string name, params KeyValuePair<string, string>[] tags);

    /// <summary>
    /// Records a histogram observation.
    /// </summary>
    /// <param name="name">The name of the histogram.</param>
    /// <param name="value">The value to observe.</param>
    /// <param name="tags">Optional tags for the metric.</param>
    void ObserveHistogram(string name, double value, params KeyValuePair<string, string>[] tags);

    /// <summary>
    /// Records a gauge value (a metric that can increase or decrease).
    /// </summary>
    /// <param name="name">The name of the gauge.</param>
    /// <param name="value">The current value.</param>
    /// <param name="tags">Optional tags for the metric.</param>
    void RecordGauge(string name, double value, params KeyValuePair<string, string>[] tags);
}
