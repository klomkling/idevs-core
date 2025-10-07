using Idevs.Common;

namespace Idevs.Abstractions;

/// <summary>
/// Defines a handler for processing commands that do not return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the specified command asynchronously.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result.</returns>
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
