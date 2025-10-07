using FluentValidation;
using Idevs.Abstractions;
using Idevs.Common;

namespace Idevs.Decorators;

/// <summary>
/// Decorator that adds validation to command handlers without a response.
/// Validates the command using FluentValidation before executing the handler.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public sealed class ValidationCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    private readonly IValidator<TCommand> _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationCommandHandlerDecorator{TCommand}"/> class.
    /// </summary>
    /// <param name="inner">The inner handler to decorate.</param>
    /// <param name="validator">The validator for the command.</param>
    public ValidationCommandHandlerDecorator(
        ICommandHandler<TCommand> inner,
        IValidator<TCommand> validator)
        : base(inner)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <inheritdoc />
    public override async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errorDetails = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<string>)g.Select(e => e.ErrorMessage).ToList());

            var error = Error.Validation(
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                errorDetails);

            return Result.Failure(error);
        }

        return await Inner.HandleAsync(command, cancellationToken);
    }
}
