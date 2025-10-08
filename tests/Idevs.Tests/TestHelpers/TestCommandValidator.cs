using FluentValidation;

namespace Idevs.Tests.TestHelpers;

public sealed class TestCommandValidator : AbstractValidator<TestCommand>
{
    public TestCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.Value).GreaterThan(0).WithMessage("Value must be positive");
    }
}
