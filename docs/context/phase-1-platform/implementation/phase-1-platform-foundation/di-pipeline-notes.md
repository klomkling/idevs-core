# DI Pipeline Notes - Phase 1 Platform Foundation

**Date**: 2025-10-07

---

## Overview

This document summarizes the handler registration and decorator composition strategy for Phase 1 implementation.

---

## Decorator Composition Strategy

### Pipeline Order

The decorators are composed in the following order (outermost to innermost):

```
┌─────────────────────────────────────┐
│  LoggingDecorator                   │  ← Outermost (executed first)
│  ├─────────────────────────────┐   │
│  │  ValidationDecorator         │   │
│  │  ├──────────────────────┐   │   │
│  │  │  MetricsDecorator     │   │   │
│  │  │  ├───────────────┐   │   │   │
│  │  │  │  CoreHandler  │   │   │   │  ← Innermost (business logic)
│  │  │  └───────────────┘   │   │   │
│  │  └──────────────────────┘   │   │
│  └─────────────────────────────┘   │
└─────────────────────────────────────┘
```

### Execution Flow

**Success Path**:
1. **Logging**: Log start of command execution
2. **Validation**: Validate command (if validator present)
3. **Metrics**: Start timer, increment counter
4. **CoreHandler**: Execute business logic
5. **Metrics**: Stop timer, record duration
6. **Validation**: (already passed)
7. **Logging**: Log success with elapsed time

**Failure Path** (Validation Failure):
1. **Logging**: Log start of command execution
2. **Validation**: Validation fails → return Result.Failure
3. **Logging**: Log failure with validation errors
4. *(Metrics and CoreHandler never executed)*

---

## Registration API Design

### AddIdevs() Extension

Registers core Idevs services with default implementations:

```csharp
builder.Services.AddIdevs(options =>
{
    options.Decorators.EnableLogging = true;    // Default
    options.Decorators.EnableValidation = true; // Default
    options.Decorators.EnableMetrics = true;    // Default
});
```

**Registered Services**:
- `ITenantContext` → `DefaultTenantContext` (scoped)
- `ICurrentUser` → `DefaultCurrentUser` (scoped)
- `IMetrics` → `NoOpMetrics` (singleton)
- `IdevsOptions` via Options pattern

---

### AddCommandHandler<TCommand, THandler>() Extension

Registers a command handler with decorator pipeline:

```csharp
// Basic registration (all decorators enabled)
services.AddCommandHandler<CreateOrder, CreateOrderHandler>();

// Custom decorator options
services.AddCommandHandler<CreateOrder, CreateOrderHandler>(options =>
{
    options.EnableLogging = true;
    options.EnableValidation = false;  // Skip validation for this handler
    options.EnableMetrics = true;
});
```

**Implementation Strategy**:

```csharp
public static IServiceCollection AddCommandHandler<TCommand, THandler>(
    this IServiceCollection services,
    Action<DecoratorOptions>? configureDecorators = null)
    where TCommand : ICommand
    where THandler : class, ICommandHandler<TCommand>
{
    services.AddScoped<ICommandHandler<TCommand>>(sp =>
    {
        // Resolve options
        var globalOptions = sp.GetRequiredService<IOptions<IdevsOptions>>().Value;
        var decoratorOptions = globalOptions.Decorators.Clone();
        configureDecorators?.Invoke(decoratorOptions);

        // 1. Create core handler
        ICommandHandler<TCommand> handler = ActivatorUtilities.CreateInstance<THandler>(sp);

        // 2. Wrap with metrics (innermost decorator)
        if (decoratorOptions.EnableMetrics)
        {
            var metrics = sp.GetRequiredService<IMetrics>();
            handler = new MetricsCommandHandlerDecorator<TCommand>(handler, metrics);
        }

        // 3. Wrap with validation
        if (decoratorOptions.EnableValidation)
        {
            var validator = sp.GetService<IValidator<TCommand>>();
            if (validator != null)
            {
                handler = new ValidationCommandHandlerDecorator<TCommand>(handler, validator);
            }
        }

        // 4. Wrap with logging (outermost decorator)
        if (decoratorOptions.EnableLogging)
        {
            var logger = sp.GetRequiredService<ILogger<LoggingCommandHandlerDecorator<TCommand>>>();
            handler = new LoggingCommandHandlerDecorator<TCommand>(handler, logger);
        }

        return handler;
    });

    return services;
}
```

---

## Decorator Implementation Pattern

Each decorator follows this pattern:

```csharp
public sealed class XxxDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _inner;
    private readonly IDependency _dependency;

    public XxxDecorator(
        ICommandHandler<TCommand> inner,
        IDependency dependency)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
    }

    public override async Task<Result> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        // Pre-processing
        // ...

        var result = await _inner.HandleAsync(command, cancellationToken);

        // Post-processing
        // ...

        return result;
    }
}
```

---

## Service Lifetime Management

| Service | Lifetime | Reason |
|---------|----------|--------|
| `ICommandHandler<T>` | **Scoped** | Per-request lifetime |
| `ITenantContext` | **Scoped** | Request-specific tenant |
| `ICurrentUser` | **Scoped** | Request-specific user |
| `IUnitOfWork` | **Scoped** | Transaction per request |
| `IMetrics` | **Singleton** | Shared metrics collector |
| `IValidator<T>` | **Scoped** or **Singleton** | Consumer choice (FluentValidation) |

---

## Validator Resolution Strategy

The validation decorator uses **optional** validator resolution:

```csharp
var validator = sp.GetService<IValidator<TCommand>>(); // Returns null if not registered
if (validator != null)
{
    handler = new ValidationCommandHandlerDecorator<TCommand>(handler, validator);
}
```

**Benefits**:
- ✅ Commands without validation don't require empty validators
- ✅ FluentValidation validators can be registered separately
- ✅ No unnecessary validator instantiation

**Consumer Registration**:

```csharp
// Using FluentValidation
services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

// Manual registration
services.AddScoped<IValidator<CreateOrder>, CreateOrderValidator>();
```

---

## Explicit Registration Philosophy

### Why Explicit?

```csharp
// ❌ Assembly Scanning (NOT USED)
services.Scan(scan => scan
    .FromAssemblyOf<ICommandHandler>()
    .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
    .AsImplementedInterfaces());

// ✅ Explicit Registration (USED)
services.AddCommandHandler<CreateOrder, CreateOrderHandler>();
services.AddCommandHandler<UpdateOrder, UpdateOrderHandler>();
services.AddCommandHandler<DeleteOrder, DeleteOrderHandler>();
```

**Advantages**:
1. **IDE Navigation**: F12 works to jump to handler implementation
2. **Compile-Time Safety**: Typos caught at compile time
3. **No Reflection**: Zero runtime overhead for handler discovery
4. **Clear Dependencies**: Easy to see what's registered
5. **Debugging**: Clear stack traces without magic

**Disadvantages**:
1. **Verbosity**: More lines of code
2. **Maintenance**: Must register each handler manually

**Mitigation**:
- Provide optional source generator in future if verbosity becomes issue
- Keep registration methods concise and consistent

---

## Testing Strategy

### Unit Testing Decorators

Test each decorator in isolation:

```csharp
[Fact]
public async Task LoggingDecorator_Logs_Start_And_Success()
{
    // Arrange
    var innerHandler = Substitute.For<ICommandHandler<TestCommand>>();
    innerHandler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
        .Returns(Result.Success());

    var logger = Substitute.For<ILogger<LoggingCommandHandlerDecorator<TestCommand>>>();

    var decorator = new LoggingCommandHandlerDecorator<TestCommand>(innerHandler, logger);

    // Act
    await decorator.HandleAsync(new TestCommand(), CancellationToken.None);

    // Assert
    logger.Received(1).LogInformation(Arg.Any<string>(), Arg.Any<object[]>());
}
```

### Integration Testing Pipeline

Test full decorator pipeline with real DI container:

```csharp
[Fact]
public async Task AddCommandHandler_Registers_Full_Pipeline()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddIdevs();
    services.AddLogging();
    services.AddCommandHandler<TestCommand, TestCommandHandler>();

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand>>();
    var result = await handler.HandleAsync(new TestCommand(), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    // Verify decorators were applied (check logs, metrics, etc.)
}
```

---

## Performance Considerations

### Decorator Overhead

- **Logging**: < 1ms overhead (log message formatting)
- **Validation**: Varies by validator complexity (typically < 10ms)
- **Metrics**: < 1ms overhead (counter increment, histogram observation)

**Total Decorator Overhead**: Typically < 20ms

**Mitigation**:
- Use structured logging to avoid string formatting overhead
- Cache validation rules where possible
- Use efficient metrics collectors (e.g., System.Diagnostics.Metrics)

### Memory Allocation

- Decorators are created once per request (scoped lifetime)
- No heap allocations for decorator wrapping (stack-based)
- Validator instances reused within scope

---

## References

- [Phase 1 Platform Plan](../../phase-1-platform.md)
- [DI Strategy Summary](../../DI-STRATEGY-SUMMARY.md)
- [AGENTS.md](../../../../../AGENTS.md)

---

**Last Updated**: 2025-10-07
