using Idevs.Abstractions;
using Idevs.Common;

namespace Idevs.Tests.TestHelpers;

public sealed class TestCommandHandler : ICommandHandler<TestCommand>
{
    public bool WasCalled { get; private set; }
    public TestCommand? LastCommand { get; private set; }

    public Task<Result> HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        LastCommand = command;
        return Task.FromResult(Result.Success());
    }
}
