# Guide 2: MediatR Integration

**Phase**: 3 - Application Layer  
**Component**: MediatR Integration & Request Pipeline  
**Prerequisites**: Guide 1 (Handler Base Classes)  
**Estimated Time**: 3-4 hours

---

## Table of Contents

1. [Overview](#overview)
2. [Design Rationale](#design-rationale)
3. [Installation & Setup](#installation--setup)
4. [Request/Response Patterns](#requestresponse-patterns)
5. [Pipeline Configuration](#pipeline-configuration)
6. [Implementation](#implementation)
7. [Usage Examples](#usage-examples)
8. [Unit Testing](#unit-testing)
9. [Best Practices](#best-practices)
10. [Common Pitfalls](#common-pitfalls)
11. [Next Steps](#next-steps)

---

## Overview

MediatR is an in-process mediator implementation that routes requests to appropriate handlers. It provides:

- **Decoupling**: Controllers/clients don't directly depend on handlers
- **Pipeline behaviors**: Cross-cutting concerns as decorators
- **Single responsibility**: Each handler focuses on one use case
- **Testability**: Easy to mock `IMediator` or test handlers directly

### Key Components

| Component | Purpose |
|-----------|---------|
| `IMediator` | Sends requests to handlers |
| `IRequest<TResponse>` | Base interface for commands/queries |
| `IRequestHandler<TRequest, TResponse>` | Handles requests |
| `IPipelineBehavior<TRequest, TResponse>` | Decorates handlers with cross-cutting logic |

---

## Design Rationale

### Why MediatR?

1. **Convention-based routing**: No manual handler registration needed
2. **Pipeline support**: Behaviors wrap handlers in order
3. **Industry standard**: Widely adopted in .NET ecosystem
4. **Performance**: Minimal overhead, uses compiled expressions
5. **Extensibility**: Easy to add custom behaviors

### Integration with Idevs Contracts

We bridge MediatR's `IRequest<T>` with our `ICommand<T>`/`IQuery<T>`:

```csharp
// Our contracts
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

// MediatR sees them as IRequest<Result<T>>
// Our handlers implement IRequestHandler<TCommand, Result<T>>
```

This enables:
- Type-safe routing via MediatR
- Result<T> pattern for all responses
- Clear command/query semantics

---

## Installation & Setup

### 1. Install NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="MediatR" Version="12.2.0" />
  <PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.1.0" />
</ItemGroup>
```

### 2. Project Structure

```
Idevs.Application/
├── Contracts/
│   ├── ICommand.cs
│   ├── IQuery.cs
│   ├── ICommandHandler.cs
│   └── IQueryHandler.cs
├── Behaviors/
│   ├── LoggingBehavior.cs
│   ├── ValidationBehavior.cs
│   └── TransactionBehavior.cs
├── Products/
│   ├── Commands/
│   │   └── Create/
│   │       ├── CreateProductCommand.cs
│   │       ├── CreateProductCommandHandler.cs
│   │       └── CreateProductCommandValidator.cs
│   └── Queries/
│       └── GetById/
│           ├── GetProductByIdQuery.cs
│           └── GetProductByIdQueryHandler.cs
└── DependencyInjection.cs
```

---

## Request/Response Patterns

### Pattern 1: Command with Result<T>

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Marker interface that bridges our command with MediatR.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
```

**Usage**:
```csharp
public sealed record CreateProductCommand(
    string Name,
    decimal Price
) : ICommand<Guid>; // MediatR sees: IRequest<Result<Guid>>
```

---

### Pattern 2: Query with Result<T>

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Marker interface that bridges our query with MediatR.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
```

**Usage**:
```csharp
public sealed record GetProductByIdQuery(Guid Id) 
    : IQuery<ProductDto>; // MediatR sees: IRequest<Result<ProductDto>>
```

---

### Pattern 3: Handler Implementation

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Command handler that MediatR can invoke.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
```

**Implementation**:
```csharp
public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, Guid>
{
    // MediatR will invoke this
    public async Task<Result<Guid>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // Handler logic
    }
}
```

---

## Pipeline Configuration

### Behavior Execution Order

Behaviors execute in the order they're registered:

```
1. LoggingBehavior          → Logs request/response
2. ValidationBehavior       → Validates command/query
3. AuthorizationBehavior    → Checks permissions
4. TransactionBehavior      → Wraps in UoW (commands only)
5. CachingBehavior          → Cache-aside (queries only)
6. ErrorHandlingBehavior    → Maps exceptions
   ↓
   HANDLER EXECUTION
   ↓
6. ErrorHandlingBehavior    ← Maps exceptions
5. CachingBehavior          ← Updates cache
4. TransactionBehavior      ← Commits/rollback
3. AuthorizationBehavior    ← (no post-processing)
2. ValidationBehavior       ← (no post-processing)
1. LoggingBehavior          ← Logs result
```

### IPipelineBehavior<TRequest, TResponse>

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
```

**Key Points**:
- `request`: The command/query instance
- `next`: Delegate to invoke next behavior or handler
- `cancellationToken`: Propagate cancellation
- **Must call `next()` to continue pipeline**

---

## Implementation

### 1. Dependency Injection Setup

```csharp
namespace Idevs.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application layer services including MediatR.
    /// </summary>
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        Assembly assembly)
    {
        // Register MediatR with assembly scanning
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            
            // Register behaviors in execution order
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));
            config.AddOpenBehavior(typeof(CachingBehavior<,>));
            config.AddOpenBehavior(typeof(ErrorHandlingBehavior<,>));
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

---

### 2. Sample Logging Behavior

```csharp
namespace Idevs.Application.Behaviors;

/// <summary>
/// Logs command/query execution with timing.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();

        _logger.LogInformation(
            "Handling {RequestName} ({RequestId})",
            requestName,
            requestId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} ({RequestId}) in {ElapsedMs}ms",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error handling {RequestName} ({RequestId}) after {ElapsedMs}ms",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
```

---

### 3. Minimal Validation Behavior

```csharp
namespace Idevs.Application.Behaviors;

/// <summary>
/// Validates requests using FluentValidation.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            var error = Error.Validation(
                "Validation.Error",
                "One or more validation errors occurred.",
                failures.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
                    .ToArray());

            // Cast to Result<T> assuming TResponse is Result<T>
            return (TResponse)(object)Result.Failure<object>(error);
        }

        return await next();
    }
}
```

---

### 4. Transaction Behavior (Commands Only)

```csharp
namespace Idevs.Application.Behaviors;

/// <summary>
/// Wraps commands in a transaction/unit of work.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply transaction to commands
        if (!IsCommand(request))
            return await next();

        _logger.LogInformation(
            "Beginning transaction for {RequestName}",
            typeof(TRequest).Name);

        try
        {
            var response = await next();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Committed transaction for {RequestName}",
                typeof(TRequest).Name);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Rolling back transaction for {RequestName}",
                typeof(TRequest).Name);

            throw;
        }
    }

    private static bool IsCommand(TRequest request) =>
        request.GetType()
            .GetInterfaces()
            .Any(i => i.IsGenericType && 
                      i.GetGenericTypeDefinition() == typeof(ICommand<>));
}
```

---

## Usage Examples

### Example 1: Basic Command Execution

```csharp
namespace Idevs.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Price,
            request.CategoryId);

        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(new { productId = result.Value })
            : BadRequest(new { error = result.Error });
    }
}
```

---

### Example 2: Query Execution with Caching

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(
    Guid id,
    CancellationToken cancellationToken)
{
    var query = new GetProductByIdQuery(id);

    var result = await _mediator.Send(query, cancellationToken);

    return result.Match(
        onSuccess: product => Ok(product),
        onFailure: error => error.Type switch
        {
            ErrorType.NotFound => NotFound(new { error }),
            _ => BadRequest(new { error })
        });
}
```

---

### Example 3: Background Job with MediatR

```csharp
namespace Idevs.Jobs;

public sealed class DailyReportJob : IJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<DailyReportJob> _logger;

    public DailyReportJob(IMediator mediator, ILogger<DailyReportJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting daily report generation");

        var command = new GenerateDailyReportCommand(DateTime.UtcNow.Date);

        var result = await _mediator.Send(command, context.CancellationToken);

        if (result.IsSuccess)
            _logger.LogInformation("Daily report generated: {ReportId}", result.Value);
        else
            _logger.LogError("Failed to generate report: {Error}", result.Error);
    }
}
```

---

### Example 4: Batch Operations

```csharp
public sealed class BulkProductImportHandler
{
    private readonly IMediator _mediator;

    public async Task<Result<BulkImportResult>> Handle(
        BulkProductImportCommand command,
        CancellationToken cancellationToken)
    {
        var results = new List<Result<Guid>>();

        foreach (var item in command.Items)
        {
            var createCommand = new CreateProductCommand(
                item.Name,
                item.Price,
                item.CategoryId);

            var result = await _mediator.Send(createCommand, cancellationToken);
            results.Add(result);
        }

        var succeeded = results.Count(r => r.IsSuccess);
        var failed = results.Count(r => r.IsFailure);

        return Result.Success(new BulkImportResult(succeeded, failed));
    }
}
```

---

## Unit Testing

### Test 1: Testing with Real MediatR

```csharp
namespace Idevs.Application.Tests.Integration;

public sealed class MediatRPipelineTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public MediatRPipelineTests()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddApplication(typeof(CreateProductCommand).Assembly);

        // Mock infrastructure dependencies
        services.AddSingleton(Substitute.For<IRepository<Product>>());
        services.AddSingleton(Substitute.For<IUnitOfWork>());

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_ValidCommand_ExecutesAllBehaviors()
    {
        // Arrange
        var command = new CreateProductCommand("Test", 10m, Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.ShouldNotBeNull();
        // Logging, validation, transaction behaviors all executed
    }

    [Fact]
    public async Task Send_InvalidCommand_ReturnValidationError()
    {
        // Arrange
        var command = new CreateProductCommand("", -1m, Guid.Empty);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
```

---

### Test 2: Mocking IMediator

```csharp
namespace Idevs.Api.Tests.Controllers;

public sealed class ProductsControllerTests
{
    [Fact]
    public async Task CreateProduct_SuccessfulCommand_ReturnsOk()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var productId = Guid.NewGuid();

        mediator
            .Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(productId));

        var controller = new ProductsController(mediator);
        var request = new CreateProductRequest("Test", 10m, Guid.NewGuid());

        // Act
        var result = await controller.CreateProduct(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        await mediator.Received(1).Send(
            Arg.Is<CreateProductCommand>(c => c.Name == "Test"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_FailedCommand_ReturnsBadRequest()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();

        mediator
            .Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>(Error.Validation("Test", "Invalid")));

        var controller = new ProductsController(mediator);
        var request = new CreateProductRequest("", -1m, Guid.Empty);

        // Act
        var result = await controller.CreateProduct(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
```

---

### Test 3: Behavior Isolation Testing

```csharp
public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_SuccessfulRequest_LogsInformationMessages()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, string>>>();
        var behavior = new LoggingBehavior<TestRequest, string>(logger);
        var request = new TestRequest();

        RequestHandlerDelegate<string> next = () => Task.FromResult("Success");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.ShouldBe("Success");

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Handling")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Handled")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_ExceptionThrown_LogsError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, string>>>();
        var behavior = new LoggingBehavior<TestRequest, string>(logger);
        var request = new TestRequest();

        RequestHandlerDelegate<string> next = () => 
            throw new InvalidOperationException("Test exception");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await behavior.Handle(request, next, CancellationToken.None));

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private sealed record TestRequest;
}
```

---

## Best Practices

### ✅ DO

1. **Register behaviors in correct order**
   ```csharp
   // Logging first, error handling last
   config.AddOpenBehavior(typeof(LoggingBehavior<,>));
   config.AddOpenBehavior(typeof(ValidationBehavior<,>));
   config.AddOpenBehavior(typeof(ErrorHandlingBehavior<,>));
   ```

2. **Always call next() in behaviors**
   ```csharp
   public async Task<TResponse> Handle(...)
   {
       // Pre-processing
       var response = await next(); // ✅ Call next
       // Post-processing
       return response;
   }
   ```

3. **Use open generics for behaviors**
   ```csharp
   config.AddOpenBehavior(typeof(LoggingBehavior<,>)); // ✅
   ```

4. **Inject IMediator, not handlers**
   ```csharp
   public ProductsController(IMediator mediator) // ✅
   public ProductsController(CreateProductCommandHandler handler) // ❌
   ```

5. **Pass CancellationToken through**
   ```csharp
   await _mediator.Send(command, cancellationToken); // ✅
   ```

---

### ❌ DON'T

1. **Don't forget to call next()**
   ```csharp
   public async Task<TResponse> Handle(...)
   {
       // Do work
       return default; // ❌ Breaks pipeline!
   }
   ```

2. **Don't swallow exceptions without re-throwing**
   ```csharp
   try
   {
       return await next();
   }
   catch
   {
       return default; // ❌ Silent failure
   }
   ```

3. **Don't register behaviors multiple times**
   ```csharp
   config.AddOpenBehavior(typeof(LoggingBehavior<,>));
   config.AddOpenBehavior(typeof(LoggingBehavior<,>)); // ❌ Duplicate
   ```

4. **Don't mix sync and async in handlers**
   ```csharp
   public Task<Result<Guid>> Handle(...)
   {
       DoSyncWork(); // ❌ Blocks thread
       return Task.FromResult(Result.Success(id));
   }
   ```

5. **Don't create IMediator instances manually**
   ```csharp
   var mediator = new Mediator(...); // ❌ Use DI
   ```

---

## Common Pitfalls

### Pitfall 1: Behavior Order Matters

**Problem**: Validation runs after transaction starts.

```csharp
// ❌ Bad order
config.AddOpenBehavior(typeof(TransactionBehavior<,>));
config.AddOpenBehavior(typeof(ValidationBehavior<,>));
// Now validation errors still commit empty transaction!
```

**Solution**: Validate before transaction.

```csharp
// ✅ Correct order
config.AddOpenBehavior(typeof(ValidationBehavior<,>));
config.AddOpenBehavior(typeof(TransactionBehavior<,>));
```

---

### Pitfall 2: Forgetting Assembly Registration

**Problem**: Handlers not found at runtime.

```csharp
// ❌ Bad: Wrong assembly
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
// Handlers are in different assembly!
```

**Solution**: Register correct assembly.

```csharp
// ✅ Good
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
});
```

---

### Pitfall 3: Not Handling Validation Errors

**Problem**: ValidationBehavior short-circuits but returns wrong type.

```csharp
// ❌ Bad: Assumes TResponse is always Result<T>
if (failures.Any())
    return default; // Returns null!
```

**Solution**: Properly cast to Result<T>.

```csharp
// ✅ Good
if (failures.Any())
{
    var error = Error.Validation(...);
    return (TResponse)(object)Result.Failure<object>(error);
}
```

---

### Pitfall 4: Circular Dependencies

**Problem**: Behavior depends on service that depends on IMediator.

```csharp
// ❌ Bad
public sealed class NotificationBehavior<TRequest, TResponse>
{
    private readonly IMediator _mediator; // Circular!

    public async Task<TResponse> Handle(...)
    {
        var notification = new SomethingHappenedNotification();
        await _mediator.Publish(notification); // Breaks!
    }
}
```

**Solution**: Use IPublisher or defer notifications.

```csharp
// ✅ Good: Collect domain events, publish after handler
```

---

## Next Steps

1. **Read Guide 3**: Validation Pipeline (`03-VALIDATION-PIPELINE.md`)
2. **Implement DependencyInjection.AddApplication()**: Wire up MediatR
3. **Create Sample Behavior**: Implement LoggingBehavior
4. **Test Pipeline**: Write integration test with all behaviors
5. **Refactor Controllers**: Replace handler injection with IMediator

---

**Summary**: MediatR provides a clean mediator pattern implementation that routes commands/queries to handlers and enables pipeline behaviors for cross-cutting concerns. Proper configuration and behavior ordering are critical for correct operation.

---

**Implementation Checklist**:

- [ ] Install MediatR NuGet packages
- [ ] Create `DependencyInjection.AddApplication()` extension
- [ ] Implement `LoggingBehavior<TRequest, TResponse>`
- [ ] Implement `ValidationBehavior<TRequest, TResponse>`
- [ ] Implement `TransactionBehavior<TRequest, TResponse>`
- [ ] Register MediatR with correct assembly
- [ ] Register behaviors in correct order
- [ ] Write integration test for full pipeline
- [ ] Refactor controllers to use IMediator
- [ ] Document behavior execution order

---

**Last Updated**: 2025-01-08  
**Next Guide**: 03-VALIDATION-PIPELINE.md
