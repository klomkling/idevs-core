using System.Diagnostics;
using Idevs.Abstractions;
using Idevs.Common;
using Microsoft.Extensions.Logging;

namespace Idevs.Decorators;

/// <summary>
/// Decorator that adds logging to command handlers without a response.
/// Logs command execution start, completion, and failures with elapsed time.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public sealed class LoggingCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger<LoggingCommandHandlerDecorator<TCommand>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingCommandHandlerDecorator{TCommand}"/> class.
    /// </summary>
    /// <param name="inner">The inner handler to decorate.</param>
    /// <param name="logger">The logger instance.</param>
    public LoggingCommandHandlerDecorator(
        ICommandHandler<TCommand> inner,
        ILogger<LoggingCommandHandlerDecorator<TCommand>> logger)
        : base(inner)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandType = typeof(TCommand).Name;
        
        _logger.LogInformation("Executing command {CommandType}", commandType);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await Inner.HandleAsync(command, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Command {CommandType} completed successfully in {ElapsedMs}ms",
                    commandType,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Command {CommandType} failed in {ElapsedMs}ms with error: {ErrorCode} - {ErrorMessage}",
                    commandType,
                    stopwatch.ElapsedMilliseconds,
                    result.Error?.Code,
                    result.Error?.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Command {CommandType} threw an exception after {ElapsedMs}ms",
                commandType,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
