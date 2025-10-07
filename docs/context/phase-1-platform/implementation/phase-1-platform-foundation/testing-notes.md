# Testing Notes - Phase 1 Platform Foundation

**Date**: 2025-10-07

---

## Overview

This document outlines testing strategy, coverage targets, and intentional gaps for Phase 1 implementation.

---

## Coverage Targets

### Global Target

- **Minimum Line Coverage**: 80%
- **Minimum Branch Coverage**: 75%
- **Mutation Testing**: Not required for Phase 1 (future consideration)

### Per-Component Targets

| Component | Target Coverage | Priority |
|-----------|----------------|----------|
| **CQRS Abstractions** | 90%+ | High |
| **Result Pattern** | 95%+ | Critical |
| **Decorators** | 85%+ | High |
| **DI Extensions** | 80%+ | High |
| **Default Implementations** | 70%+ | Medium |
| **Configuration Options** | 60%+ | Low |

---

## Testing Strategy

### Test Pyramid

```
        ┌─────────────┐
        │   E2E/Int   │  10% - DI pipeline integration
        └─────────────┘
       ┌───────────────┐
       │  Integration  │  20% - Multiple components together
       └───────────────┘
     ┌─────────────────┐
     │   Unit Tests    │   70% - Individual components
     └─────────────────┘
```

---

## Test Organization

### Folder Structure

```
tests/Idevs.Tests/
├── Abstractions/
│   ├── CommandHandlerTests.cs
│   └── QueryHandlerTests.cs
├── Common/
│   ├── ResultTests.cs
│   └── ErrorTests.cs
├── Decorators/
│   ├── LoggingDecoratorTests.cs
│   ├── ValidationDecoratorTests.cs
│   └── MetricsDecoratorTests.cs
├── DependencyInjection/
│   ├── AddIdevsTests.cs
│   ├── AddCommandHandlerTests.cs
│   └── DecoratorPipelineTests.cs
├── Services/
│   ├── DefaultTenantContextTests.cs
│   ├── DefaultCurrentUserTests.cs
│   └── NoOpMetricsTests.cs
└── TestHelpers/
    ├── TestCommand.cs
    ├── TestCommandHandler.cs
    └── TestValidator.cs
```

---

## Test Categories

### 1. Unit Tests

**Purpose**: Test individual components in isolation

**Naming Convention**: `MethodName_StateUnderTest_ExpectedBehavior`

**Example**:

```csharp
[Fact]
public void Result_Success_ReturnsSuccessResult()
{
    // Arrange & Act
    var result = Result.Success();

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.IsFailure.ShouldBeFalse();
    result.Error.ShouldBeNull();
}

[Fact]
public void Result_Failure_ReturnsFailureWithError()
{
    // Arrange
    var error = Error.Validation("TEST001", "Test error");

    // Act
    var result = Result.Failure(error);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.IsFailure.ShouldBeTrue();
    result.Error.ShouldBe(error);
}
```

---

### 2. Integration Tests

**Purpose**: Test multiple components working together

**Example**: Testing decorator pipeline composition

```csharp
[Fact]
public async Task DecoratorPipeline_ExecutesInCorrectOrder()
{
    // Arrange
    var executionOrder = new List<string>();
    var services = new ServiceCollection();

    services.AddIdevs();
    services.AddLogging();
    services.AddSingleton(executionOrder); // Track execution
    services.AddCommandHandler<TestCommand, TestCommandHandler>();

    var sp = services.BuildServiceProvider();
    var handler = sp.GetRequiredService<ICommandHandler<TestCommand>>();

    // Act
    await handler.HandleAsync(new TestCommand(), CancellationToken.None);

    // Assert
    executionOrder.ShouldBe(new[]
    {
        "Logging:Start",
        "Validation:Validate",
        "Metrics:Start",
        "Handler:Execute",
        "Metrics:Stop",
        "Logging:Stop"
    });
}
```

---

### 3. Behavioral Tests

**Purpose**: Test expected behavior in various scenarios

**Scenarios**:

- Successful command execution
- Validation failure
- Handler exception
- Decorator short-circuiting
- Missing optional dependencies (validator)

```csharp
[Fact]
public async Task ValidationDecorator_WithoutValidator_SkipsValidation()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddIdevs();
    services.AddLogging();
    services.AddCommandHandler<TestCommand, TestCommandHandler>();
    // Note: No validator registered

    var sp = services.BuildServiceProvider();
    var handler = sp.GetRequiredService<ICommandHandler<TestCommand>>();

    // Act
    var result = await handler.HandleAsync(new TestCommand(), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue(); // Should not fail due to missing validator
}
```

---

## Testing Tools

### Primary Framework: xUnit

- **Version**: Latest stable supporting .NET 9
- **Reason**: Industry standard, good async support, parallel execution

### Assertion Library: Shouldly

- **Version**: Latest stable
- **Reason**: Readable assertions, better error messages

**Example**:

```csharp
result.IsSuccess.ShouldBeTrue();
result.Value.ShouldBe(expectedValue);
result.Error.ShouldBeNull();
```

### Mocking Library: NSubstitute

- **Version**: Latest stable supporting .NET 9
- **Reason**: Clean syntax, easy to use

**Example**:

```csharp
var logger = Substitute.For<ILogger<TestClass>>();
logger.Received(1).LogInformation(Arg.Any<string>(), Arg.Any<object[]>());
```

---

## Code Coverage Collection

### Tools

- **coverlet.collector** (xUnit integration)
- **ReportGenerator** (HTML reports)

### Commands

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html;MarkdownSummaryGithub"

# Check coverage threshold
dotnet test --collect:"XPlat Code Coverage" \
  /p:Threshold=80 \
  /p:ThresholdType=line \
  /p:ThresholdStat=total
```

---

## Intentional Coverage Gaps

### 1. Configuration Property Getters/Setters

**Reason**: Auto-properties don't need tests

```csharp
public class DecoratorOptions
{
    public bool EnableLogging { get; set; } = true; // ← Not tested
    public bool EnableValidation { get; set; } = true; // ← Not tested
}
```

### 2. Simple Pass-Through Implementations

**Reason**: No business logic to test

```csharp
public class NoOpMetrics : IMetrics
{
    public void IncrementCounter(string name, long increment = 1, params KeyValuePair<string, string>[] tags)
    {
        // No-op - nothing to test
    }
}
```

### 3. Marker Interfaces

**Reason**: No implementation to test

```csharp
public interface ICommand { } // ← Not tested (marker only)
```

### 4. Defensive Null Checks (ArgumentNullException)

**Reason**: Low value, covered by integration tests

```csharp
public LoggingDecorator(ICommandHandler<TCommand> inner)
{
    _inner = inner ?? throw new ArgumentNullException(nameof(inner)); // ← Not explicitly tested
}
```

---

## Test Data Builders

### Example: TestCommand Builder

```csharp
public record TestCommand(string Name, int Value) : ICommand;

public class TestCommandBuilder
{
    private string _name = "Test";
    private int _value = 42;

    public TestCommandBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TestCommandBuilder WithValue(int value)
    {
        _value = value;
        return this;
    }

    public TestCommand Build() => new TestCommand(_name, _value);
}

// Usage
var command = new TestCommandBuilder()
    .WithName("CustomName")
    .WithValue(100)
    .Build();
```

---

## Performance Tests

### Benchmark Scope (Future)

Not required for Phase 1, but planned for future:

- Decorator overhead measurement
- Memory allocation profiling
- Throughput testing (commands/second)

### Tool: BenchmarkDotNet

```csharp
[MemoryDiagnoser]
public class HandlerBenchmarks
{
    [Benchmark]
    public async Task WithoutDecorators() { /* ... */ }

    [Benchmark]
    public async Task WithAllDecorators() { /* ... */ }
}
```

---

## CI Integration

### GitHub Actions Workflow

```yaml
- name: Run tests with coverage
  run: |
    dotnet test \
      --configuration Release \
      --collect:"XPlat Code Coverage" \
      --results-directory ./coverage

- name: Check coverage threshold
  run: |
    dotnet test \
      --configuration Release \
      --collect:"XPlat Code Coverage" \
      /p:Threshold=80 \
      /p:ThresholdType=line

- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v3
  with:
    files: ./coverage/**/coverage.cobertura.xml
```

---

## Testing Checklist

### Before PR

- [ ] All tests pass locally
- [ ] Coverage meets 80% threshold
- [ ] No flaky tests (run tests 3 times)
- [ ] Integration tests validate DI pipeline
- [ ] Test names follow naming convention
- [ ] Test code follows .editorconfig rules

### Code Review Focus

- [ ] Test readability (Arrange/Act/Assert clear)
- [ ] Edge cases covered
- [ ] Negative cases tested
- [ ] Async/cancellation handled correctly
- [ ] No test interdependencies

---

## Known Testing Challenges

### Challenge 1: Async Decorator Testing

**Issue**: Difficult to verify async execution order

**Solution**: Use TaskCompletionSource to control timing:

```csharp
var tcs = new TaskCompletionSource<Result>();
innerHandler.HandleAsync(Arg.Any<TestCommand>())
    .Returns(tcs.Task);

// Now we can control when inner handler completes
tcs.SetResult(Result.Success());
```

### Challenge 2: Logger Verification

**Issue**: ILogger extension methods are hard to mock

**Solution**: Use custom logger that captures messages:

```csharp
public class TestLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = new();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, 
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }

    // Implement other ILogger methods...
}
```

---

## References

- [xUnit Documentation](https://xunit.net/)
- [Shouldly Documentation](https://github.com/shouldly/shouldly)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)

---

**Last Updated**: 2025-10-07
