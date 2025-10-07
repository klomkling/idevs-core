using System.Diagnostics;
using Idevs.Abstractions;
using Idevs.Common;
using Idevs.Services;

namespace Idevs.Decorators;

/// <summary>
/// Decorator that adds metrics collection to command handlers without a response.
/// Records command execution duration, success/failure counts, and other metrics.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public sealed class MetricsCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    private readonly IMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsCommandHandlerDecorator{TCommand}"/> class.
    /// </summary>
    /// <param name="inner">The inner handler to decorate.</param>
    /// <param name="metrics">The metrics collector.</param>
    public MetricsCommandHandlerDecorator(
        ICommandHandler<TCommand> inner,
        IMetrics metrics)
        : base(inner)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <inheritdoc />
    public override async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandType = typeof(TCommand).Name;
        var tags = new[] { new KeyValuePair<string, string>("command_type", commandType) };

        _metrics.IncrementCounter("command.executions", 1, tags);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await Inner.HandleAsync(command, cancellationToken);

            stopwatch.Stop();

            var statusTags = new[]
            {
                new KeyValuePair<string, string>("command_type", commandType),
                new KeyValuePair<string, string>("status", result.IsSuccess ? "success" : "failure")
            };

            _metrics.ObserveHistogram("command.duration_ms", stopwatch.ElapsedMilliseconds, statusTags);
            _metrics.IncrementCounter(
                result.IsSuccess ? "command.successes" : "command.failures",
                1,
                tags);

            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();

            var errorTags = new[]
            {
                new KeyValuePair<string, string>("command_type", commandType),
                new KeyValuePair<string, string>("status", "exception")
            };

            _metrics.ObserveHistogram("command.duration_ms", stopwatch.ElapsedMilliseconds, errorTags);
            _metrics.IncrementCounter("command.exceptions", 1, tags);

            throw;
        }
    }
}
