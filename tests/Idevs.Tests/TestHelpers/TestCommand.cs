using Idevs.Abstractions;

namespace Idevs.Tests.TestHelpers;

public record TestCommand(string Name, int Value) : ICommand;
