using Idevs.Abstractions;
using Idevs.Common;

namespace Idevs.Decorators;

/// <summary>
/// Base class for command handler decorators that return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the command.</typeparam>
public abstract class CommandHandlerDecoratorBase<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Gets the inner command handler being decorated.
    /// </summary>
    protected ICommandHandler<TCommand, TResponse> Inner { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandlerDecoratorBase{TCommand, TResponse}"/> class.
    /// </summary>
    /// <param name="inner">The inner handler to decorate.</param>
    protected CommandHandlerDecoratorBase(ICommandHandler<TCommand, TResponse> inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public abstract Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
