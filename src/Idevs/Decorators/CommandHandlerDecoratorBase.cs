using Idevs.Abstractions;
using Idevs.Common;

namespace Idevs.Decorators;

/// <summary>
/// Base class for command handler decorators that do not return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public abstract class CommandHandlerDecoratorBase<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Gets the inner command handler being decorated.
    /// </summary>
    protected ICommandHandler<TCommand> Inner { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandlerDecoratorBase{TCommand}"/> class.
    /// </summary>
    /// <param name="inner">The inner handler to decorate.</param>
    protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public abstract Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
