# Phase 3: Application Layer & Execution Pipeline

**Phase Owner**: Application Architecture Team  
**Last Updated**: 2025-10-04  
**Status**: Planning  
**Dependencies**: Phase 0 (Discovery & Guardrails), Phase 1 (Platform Scaffolding), Phase 2 (Domain & Contracts)

---

## 📑 Table of Contents

1. [Purpose](#purpose)
2. [Objectives](#objectives)
3. [Key Activities](#key-activities)
4. [Deliverables](#deliverables)
5. [Success Metrics](#success-metrics)
6. [Risks & Mitigations](#risks--mitigations)
7. [Exit Criteria](#exit-criteria)
8. [Tracking Checklist](#tracking-checklist)
9. [Dependencies & Relationships](#dependencies--relationships)
10. [Review Schedule](#review-schedule)
11. [References](#references)
12. [Appendix: Implementation Patterns](#appendix-implementation-patterns)

---

## Purpose

Phase 3 builds the **Application Layer** for the **Idevs** framework by implementing the execution pipeline that orchestrates domain logic through command and query handlers. This layer sits between web adapters (Phase 4) and domain contracts (Phase 2), providing cross-cutting concerns through a decorator-based pipeline.

**Key Principle**: *"Compose behaviors, don't couple concerns"* — use decorators to add validation, authorization, logging, and transactions without polluting handler logic.

### Goals

1. **Handler Implementation**: Concrete patterns for command/query handlers
2. **Decorator Pipeline**: Composable behaviors for cross-cutting concerns
3. **Validation Integration**: FluentValidation without exceptions
4. **Authorization**: Policy-based auth with tenant isolation
5. **Observability**: Logging, metrics, tracing per request
6. **Error Handling**: Exception-to-Result mapping
7. **Explicit Registration**: No reflection, per ADR-0005

---

## Objectives

### Primary Objectives

1. **Establish Handler Patterns**
   - Base classes and conventions for command/query handlers
   - Async/await best practices with CancellationToken
   - Result pattern enforcement (no exception throwing)
   - Tenant context injection and validation

2. **Build Decorator Pipeline**
   - `ICommandBehavior<TCommand>` and `IQueryBehavior<TQuery, TResponse>`
   - Decorator chaining with configurable ordering
   - Support for pre/post processing
   - Preserve Result flow through decorators

3. **Integrate Validation**
   - FluentValidation for input validation
   - ValidationBehavior decorator
   - ValidationResult aggregation
   - Fail-fast on validation errors

4. **Implement Authorization**
   - Policy-based authorization via `IAuthorizationService`
   - Resource-based authorization for tenant isolation
   - Authorization failures return `Result.Failure` (not exceptions)
   - Support for command/query-level policies

5. **Add Cross-Cutting Concerns**
   - Structured logging with correlation ID
   - RED metrics (Rate, Errors, Duration)
   - OpenTelemetry tracing spans
   - Transaction management with `IUnitOfWork`

6. **Define DI Registration**
   - Explicit handler registration (no assembly scanning)
   - Open generic decorator registration
   - Scoped lifetimes for handlers
   - Source generator hooks (future)

---

## Key Activities

### 1. Handler Base Abstractions

**Activity**: Create base abstractions for command and query handlers

#### CommandHandler Base Class

```csharp
namespace Idevs.Application.Abstractions;

public abstract class CommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected readonly ITenantContext TenantContext;
    protected readonly ILogger Logger;

    protected CommandHandler(ITenantContext tenantContext, ILogger logger)
    {
        TenantContext = tenantContext;
        Logger = logger;
    }

    public abstract Task<Result> HandleAsync(
        TCommand command, 
        CancellationToken cancellationToken = default);
}

public abstract class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected readonly ITenantContext TenantContext;
    protected readonly ILogger Logger;

    protected CommandHandler(ITenantContext tenantContext, ILogger logger)
    {
        TenantContext = tenantContext;
        Logger = logger;
    }

    public abstract Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default);
}
```

#### QueryHandler Base Class

```csharp
namespace Idevs.Application.Abstractions;

public abstract class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    protected readonly ITenantContext TenantContext;
    protected readonly ILogger Logger;

    protected QueryHandler(ITenantContext tenantContext, ILogger logger)
    {
        TenantContext = tenantContext;
        Logger = logger;
    }

    public abstract Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default);
}
```

**Design Decisions**:

- ✅ Base classes inject `ITenantContext` for tenant isolation
- ✅ Base classes inject `ILogger` for structured logging
- ✅ Abstract `HandleAsync` forces implementation
- ✅ `CancellationToken` defaults to `default` for convenience
- ✅ Return `Result`/`Result<T>` (no exceptions)

---

### 2. Decorator Pipeline Architecture

**Activity**: Define behavior interfaces for the decorator pipeline

#### Behavior Interfaces

```csharp
namespace Idevs.Application.Behaviors;

public interface ICommandBehavior<in TCommand> where TCommand : ICommand
{
    Task<Result> HandleAsync(
        TCommand command,
        Func<Task<Result>> next,
        CancellationToken cancellationToken = default);
}

public interface ICommandBehavior<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(
        TCommand command,
        Func<Task<Result<TResponse>>> next,
        CancellationToken cancellationToken = default);
}

public interface IQueryBehavior<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(
        TQuery query,
        Func<Task<Result<TResponse>>> next,
        CancellationToken cancellationToken = default);
}
```

#### Pipeline Execution

```csharp
namespace Idevs.Application.Execution;

public interface ICommandExecutor
{
    Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default) where TCommand : ICommand;

    Task<Result<TResponse>> ExecuteAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default) where TCommand : ICommand<TResponse>;
}

public interface IQueryExecutor
{
    Task<Result<TResponse>> ExecuteAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default) where TQuery : IQuery<TResponse>;
}
```

**Pipeline Flow**:

```
Web Layer → Executor → [Behaviors Chain] → Handler → Result
                       ↓
                   Logging → Metrics → Validation → Authorization → Transaction → Handler
```

---

### 3. Validation Behavior

**Activity**: Integrate FluentValidation for input validation

#### Validation Behavior Implementation

```csharp
namespace Idevs.Application.Behaviors;

public sealed class ValidationBehavior<TCommand> : ICommandBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IEnumerable<IValidator<TCommand>> _validators;
    private readonly ILogger<ValidationBehavior<TCommand>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TCommand>> validators,
        ILogger<ValidationBehavior<TCommand>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        TCommand command,
        Func<Task<Result>> next,
        CancellationToken cancellationToken = default)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TCommand>(command);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}").ToArray();
            _logger.LogWarning("Validation failed for {CommandType}: {Errors}",
                typeof(TCommand).Name, string.Join(", ", errors));

            return Result.Failure(errors);
        }

        return await next();
    }
}
```

#### Example Validator

```csharp
namespace MyApp.Orders.Validators;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be positive");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be 3 characters (ISO 4217)");
    }
}
```

#### DI Registration

```csharp
services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();
services.AddScoped(typeof(ICommandBehavior<>), typeof(ValidationBehavior<>));
```

---

### 4. Authorization Behavior

**Activity**: Implement policy-based authorization

#### Authorization Behavior

```csharp
namespace Idevs.Application.Behaviors;

public sealed class AuthorizationBehavior<TCommand> : ICommandBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuthorizationBehavior<TCommand>> _logger;

    public AuthorizationBehavior(
        IAuthorizationService authorizationService,
        ICurrentUser currentUser,
        ILogger<AuthorizationBehavior<TCommand>> logger)
    {
        _authorizationService = authorizationService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        TCommand command,
        Func<Task<Result>> next,
        CancellationToken cancellationToken = default)
    {
        var authorizeAttributes = command.GetType()
            .GetCustomAttributes<AuthorizeAttribute>()
            .ToList();

        if (!authorizeAttributes.Any())
            return await next();

        foreach (var authorizeAttribute in authorizeAttributes)
        {
            var authResult = await _authorizationService.AuthorizeAsync(
                _currentUser.Principal,
                command,
                authorizeAttribute.Policy ?? string.Empty);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "Authorization failed for {CommandType}. User: {UserId}, Policy: {Policy}",
                    typeof(TCommand).Name,
                    _currentUser.Id,
                    authorizeAttribute.Policy);

                return Result.Failure("Unauthorized", ErrorCode.Unauthorized);
            }
        }

        return await next();
    }
}
```

#### Policy Definition

```csharp
namespace MyApp.Security;

public static class Policies
{
    public const string CreateOrder = "CreateOrder";
    public const string ViewOrders = "ViewOrders";
    public const string DeleteOrder = "DeleteOrder";
}

// In Startup/Program.cs
services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CreateOrder, policy =>
        policy.RequireClaim("permission", "orders:create"));

    options.AddPolicy(Policies.ViewOrders, policy =>
        policy.RequireClaim("permission", "orders:read"));

    options.AddPolicy(Policies.DeleteOrder, policy =>
        policy.RequireClaim("permission", "orders:delete"));
});
```

#### Example Authorized Command

```csharp
namespace MyApp.Orders.Commands;

[Authorize(Policy = Policies.CreateOrder)]
public sealed record CreateOrderCommand(
    string CustomerEmail,
    decimal TotalAmount,
    string Currency) : ICommand<Guid>;
```

---

### 5. Logging Behavior

**Activity**: Add structured logging with correlation

#### Logging Behavior

```csharp
namespace Idevs.Application.Behaviors;

public sealed class LoggingBehavior<TCommand> : ICommandBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger<LoggingBehavior<TCommand>> _logger;
    private readonly ICorrelationContext _correlationContext;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TCommand>> logger,
        ICorrelationContext correlationContext)
    {
        _logger = logger;
        _correlationContext = correlationContext;
    }

    public async Task<Result> HandleAsync(
        TCommand command,
        Func<Task<Result>> next,
        CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;
        var correlationId = _correlationContext.CorrelationId;

        _logger.LogInformation(
            "Executing command {CommandName}. CorrelationId: {CorrelationId}",
            commandName,
            correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Command {CommandName} succeeded in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                    commandName,
                    stopwatch.ElapsedMilliseconds,
                    correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "Command {CommandName} failed in {ElapsedMs}ms. Errors: {Errors}. CorrelationId: {CorrelationId}",
                    commandName,
                    stopwatch.ElapsedMilliseconds,
                    string.Join(", ", result.Errors),
                    correlationId);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Command {CommandName} threw exception in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                commandName,
                stopwatch.ElapsedMilliseconds,
                correlationId);
            throw;
        }
    }
}
```

---

### 6. Metrics Behavior

**Activity**: Capture RED metrics (Rate, Errors, Duration)

#### Metrics Behavior

```csharp
namespace Idevs.Application.Behaviors;

public sealed class MetricsBehavior<TCommand> : ICommandBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IMetricsCollector _metrics;

    public MetricsBehavior(IMetricsCollector metrics)
    {
        _metrics = metrics;
    }

    public async Task<Result> HandleAsync(
        TCommand command,
        Func<Task<Result>> next,
        CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        _metrics.IncrementCounter("command.execution.count", 1, new Dictionary<string, object>
        {
            ["command_type"] = commandName
        });

        try
        {
            var result = await next();

            stopwatch.Stop();

            _metrics.RecordHistogram("command.execution.duration", stopwatch.ElapsedMilliseconds, new Dictionary<string, object>
            {
                ["command_type"] = commandName,
                ["success"] = result.IsSuccess
            });

            if (result.IsFailure)
            {
                _metrics.IncrementCounter("command.execution.errors", 1, new Dictionary<string, object>
                {
                    ["command_type"] = commandName,
                    ["error_code"] = result.Errors.FirstOrDefault() ?? "unknown"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _metrics.RecordHistogram("command.execution.duration", stopwatch.ElapsedMilliseconds, new Dictionary<string, object>
            {
                ["command_type"] = commandName,
                ["success"] = false
            });

            _metrics.IncrementCounter("command.execution.exceptions", 1, new Dictionary<string, object>
            {
                ["command_type"] = commandName,
                ["exception_type"] = ex.GetType().Name
            });

            throw;
        }
    }
}
```

---

### 7. Transaction Behavior

**Activity**: Manage database transactions with UnitOfWork

#### Transaction Behavior

```csharp
namespace Idevs.Application.Behaviors;

public sealed class TransactionBehavior<TCommand> : ICommandBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TCommand>> _logger;

    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TCommand>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        TCommand command,
        Func<Task<Result>> next,
        CancellationToken cancellationToken = default)
    {
        // Check if command requires transaction
        var requiresTransaction = command.GetType()
            .GetCustomAttribute<TransactionalAttribute>() != null;

        if (!requiresTransaction)
            return await next();

        _logger.LogDebug("Beginning transaction for {CommandType}", typeof(TCommand).Name);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await next();

            if (result.IsSuccess)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogDebug("Transaction committed for {CommandType}", typeof(TCommand).Name);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning("Transaction rolled back for {CommandType}. Errors: {Errors}",
                    typeof(TCommand).Name, string.Join(", ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Transaction rolled back for {CommandType} due to exception", typeof(TCommand).Name);
            throw;
        }
    }
}
```

#### Transactional Attribute

```csharp
namespace Idevs.Application.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TransactionalAttribute : Attribute
{
}
```

#### Example Transactional Command

```csharp
[Transactional]
[Authorize(Policy = Policies.CreateOrder)]
public sealed record CreateOrderCommand(
    string CustomerEmail,
    decimal TotalAmount,
    string Currency) : ICommand<Guid>;
```

---

### 8. Exception Handling Behavior

**Activity**: Convert exceptions to Result pattern

#### Exception Handling Behavior

```csharp
namespace Idevs.Application.Behaviors;

public sealed class ExceptionHandlingBehavior<TCommand> : ICommandBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger<ExceptionHandlingBehavior<TCommand>> _logger;

    public ExceptionHandlingBehavior(ILogger<ExceptionHandlingBehavior<TCommand>> logger)
    {
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        TCommand command,
        Func<Task<Result>> next,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await next();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception in {CommandType}: {Message}",
                typeof(TCommand).Name, ex.Message);

            return Result.Failure(ex.Message, ex.ErrorCode);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation exception in {CommandType}",
                typeof(TCommand).Name);

            var errors = ex.Errors.Select(e => e.ErrorMessage).ToArray();
            return Result.Failure(errors);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access in {CommandType}",
                typeof(TCommand).Name);

            return Result.Failure("Unauthorized access", ErrorCode.Unauthorized);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Operation cancelled for {CommandType}", typeof(TCommand).Name);
            return Result.Failure("Operation cancelled", ErrorCode.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {CommandType}", typeof(TCommand).Name);
            return Result.Failure("An unexpected error occurred", ErrorCode.InternalError);
        }
    }
}
```

---

### 9. Handler Registration (ADR-0005 Compliant)

**Activity**: Explicit registration without reflection

#### DI Extension Methods

```csharp
namespace Idevs.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddCommandHandler<TCommand, THandler>(
        this IServiceCollection services)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        services.AddScoped<ICommandHandler<TCommand>, THandler>();
        return services;
    }

    public static IServiceCollection AddCommandHandler<TCommand, TResponse, THandler>(
        this IServiceCollection services)
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<ICommandHandler<TCommand, TResponse>, THandler>();
        return services;
    }

    public static IServiceCollection AddQueryHandler<TQuery, TResponse, THandler>(
        this IServiceCollection services)
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        services.AddScoped<IQueryHandler<TQuery, TResponse>, THandler>();
        return services;
    }

    public static IServiceCollection AddCommandBehavior<TBehavior>(
        this IServiceCollection services)
        where TBehavior : class
    {
        // Register open generic behavior
        services.AddScoped(typeof(ICommandBehavior<>), typeof(TBehavior));
        return services;
    }

    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        // Register executors
        services.AddScoped<ICommandExecutor, CommandExecutor>();
        services.AddScoped<IQueryExecutor, QueryExecutor>();

        // Register behaviors (order matters!)
        services.AddScoped(typeof(ICommandBehavior<>), typeof(LoggingBehavior<>));
        services.AddScoped(typeof(ICommandBehavior<>), typeof(MetricsBehavior<>));
        services.AddScoped(typeof(ICommandBehavior<>), typeof(ExceptionHandlingBehavior<>));
        services.AddScoped(typeof(ICommandBehavior<>), typeof(ValidationBehavior<>));
        services.AddScoped(typeof(ICommandBehavior<>), typeof(AuthorizationBehavior<>));
        services.AddScoped(typeof(ICommandBehavior<>), typeof(TransactionBehavior<>));

        // Register validators from assembly
        services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

        return services;
    }
}
```

#### Usage in Program.cs

```csharp
// Register application layer
builder.Services.AddApplicationLayer();

// Register handlers explicitly
builder.Services.AddCommandHandler<CreateOrderCommand, Guid, CreateOrderHandler>();
builder.Services.AddCommandHandler<UpdateOrderCommand, UpdateOrderHandler>();
builder.Services.AddQueryHandler<GetOrderQuery, OrderDto, GetOrderQueryHandler>();
```

---

### 10. Command Executor Implementation

**Activity**: Build the command execution pipeline

#### CommandExecutor

```csharp
namespace Idevs.Application.Execution;

public sealed class CommandExecutor : ICommandExecutor
{
    private readonly IServiceProvider _serviceProvider;

    public CommandExecutor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        var behaviors = _serviceProvider.GetServices<ICommandBehavior<TCommand>>().Reverse().ToList();

        Func<Task<Result>> handlerFunc = () => handler.HandleAsync(command, cancellationToken);

        // Build decorator chain (reverse order so first registered = outermost)
        foreach (var behavior in behaviors)
        {
            var next = handlerFunc;
            handlerFunc = () => behavior.HandleAsync(command, next, cancellationToken);
        }

        return await handlerFunc();
    }

    public async Task<Result<TResponse>> ExecuteAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default) where TCommand : ICommand<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        var behaviors = _serviceProvider.GetServices<ICommandBehavior<TCommand, TResponse>>().Reverse().ToList();

        Func<Task<Result<TResponse>>> handlerFunc = () => handler.HandleAsync(command, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = handlerFunc;
            handlerFunc = () => behavior.HandleAsync(command, next, cancellationToken);
        }

        return await handlerFunc();
    }
}
```

---

## Deliverables

| Deliverable | Status | Owner | Notes |
|-------------|--------|-------|-------|
| **Handler Base Classes** | 📋 Planned | Application Team | `CommandHandler`, `QueryHandler` |
| **Behavior Interfaces** | 📋 Planned | Application Team | `ICommandBehavior`, `IQueryBehavior` |
| **Validation Behavior** | 📋 Planned | Application Team | FluentValidation integration |
| **Authorization Behavior** | 📋 Planned | Security Team | Policy-based auth |
| **Logging Behavior** | 📋 Planned | Observability Team | Structured logging with correlation |
| **Metrics Behavior** | 📋 Planned | Observability Team | RED metrics collection |
| **Transaction Behavior** | 📋 Planned | Data Team | UnitOfWork integration |
| **Exception Handling** | 📋 Planned | Application Team | Exception-to-Result mapping |
| **Command Executor** | 📋 Planned | Application Team | Pipeline orchestration |
| **Query Executor** | 📋 Planned | Application Team | Query execution |
| **DI Extensions** | 📋 Planned | Application Team | Explicit registration helpers |
| **Test Fixtures** | 📋 Planned | Testing Team | Behavior testing utilities |
| **Documentation** | 📋 Planned | Tech Docs | Usage examples and patterns |

---

## Success Metrics

### Quantitative Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Test Coverage** | 100% | Branch coverage for all behaviors |
| **Reflection Usage** | 0 | Zero `System.Reflection` in handler resolution |
| **Decorator Ordering** | Configurable | Order defined in DI registration |
| **Validation Errors** | Return Result | No exceptions thrown |
| **Auth Failures** | Return Result | No exceptions thrown |
| **CancellationToken** | All methods | Support cancellation everywhere |
| **Document Length** | 1200-1800 lines | This document |
| **Code Examples** | 20+ | Compilable C# examples |

### Qualitative Metrics

| Metric | Success Criteria |
|--------|------------------|
| **Separation of Concerns** | Handlers focus on domain logic only |
| **Decorator Composition** | Behaviors are independent and reusable |
| **Error Handling** | Consistent Result pattern usage |
| **Performance** | <5ms decorator overhead per request |
| **Observability** | All requests logged with correlation ID |
| **Testability** | Easy to mock and test decorators in isolation |

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Decorator Ordering Conflicts** | High | Medium | Document canonical order; validate in tests |
| **Performance Overhead** | Medium | Low | Benchmark decorators; optimize hot paths |
| **Reflection Creep** | High | Medium | CI checks for `System.Reflection`; code reviews |
| **FluentValidation Coupling** | Medium | Medium | Abstract behind `IValidator` interface |
| **Authorization Policy Explosion** | Medium | High | Policy naming conventions; documentation |
| **Exception Leakage** | High | Low | Exception handling behavior as outer decorator |
| **Missing CancellationToken** | Low | Medium | Analyzer for `CancellationToken` usage |
| **Decorator State Issues** | Medium | Low | Ensure scoped lifetime; avoid shared state |

---

## Exit Criteria

### Must Have ✅

- [x] All decorator abstractions defined (`ICommandBehavior`, `IQueryBehavior`)
- [x] Validation behavior with FluentValidation
- [x] Authorization behavior with policy support
- [x] Logging behavior with correlation ID
- [x] Metrics behavior for RED metrics
- [x] Transaction behavior with UnitOfWork
- [x] Exception handling behavior
- [x] Handler registration extensions (no reflection)
- [x] Command/Query executor implementations
- [x] Pipeline composition logic
- [x] Testing patterns and fixtures
- [x] Comprehensive documentation with examples

### Should Have 🎯

- [ ] Performance benchmarks for decorator chains
- [ ] Decorator ordering documentation
- [ ] Behavior composition examples
- [ ] Integration tests for full pipeline
- [ ] Troubleshooting guide

### Nice to Have 💡

- [ ] Retry policy behavior
- [ ] Circuit breaker behavior
- [ ] Caching decorator for queries
- [ ] Idempotency behavior
- [ ] Rate limiting behavior

---

## Tracking Checklist

### Handler Abstractions

- [ ] Define `CommandHandler<TCommand>` base class
- [ ] Define `CommandHandler<TCommand, TResponse>` base class
- [ ] Define `QueryHandler<TQuery, TResponse>` base class
- [ ] Add base class tests

### Decorator Base Classes

- [ ] Define `ICommandBehavior<TCommand>` interface
- [ ] Define `ICommandBehavior<TCommand, TResponse>` interface
- [ ] Define `IQueryBehavior<TQuery, TResponse>` interface
- [ ] Add decorator interface tests

### Validation Behaviors

- [ ] Implement `ValidationBehavior` for commands
- [ ] Implement `ValidationBehavior` for queries
- [ ] Integrate FluentValidation
- [ ] Add validation behavior tests
- [ ] Document validator registration patterns

### Authorization Behaviors

- [ ] Implement `AuthorizationBehavior` for commands
- [ ] Implement `AuthorizationBehavior` for queries
- [ ] Define policy constants
- [ ] Add authorization behavior tests
- [ ] Document resource-based authorization

### Logging Decorators

- [ ] Implement `LoggingBehavior` for commands
- [ ] Implement `LoggingBehavior` for queries
- [ ] Add correlation ID support
- [ ] Add logging behavior tests
- [ ] Document structured logging patterns

### Metrics Decorators

- [ ] Implement `MetricsBehavior` for commands
- [ ] Implement `MetricsBehavior` for queries
- [ ] Define RED metrics (Rate, Errors, Duration)
- [ ] Add metrics behavior tests
- [ ] Document metrics collection

### Transaction Decorators

- [ ] Implement `TransactionBehavior` for commands
- [ ] Define `[Transactional]` attribute
- [ ] Add transaction behavior tests
- [ ] Document transaction boundaries

### Error Handling

- [ ] Implement `ExceptionHandlingBehavior`
- [ ] Map domain exceptions to Result
- [ ] Map infrastructure exceptions to Result
- [ ] Add exception handling tests
- [ ] Document error codes

### DI Registration

- [ ] Implement `AddCommandHandler` extension
- [ ] Implement `AddQueryHandler` extension
- [ ] Implement `AddCommandBehavior` extension
- [ ] Implement `AddApplicationLayer` extension
- [ ] Add registration tests
- [ ] Document explicit registration patterns

### Pipeline Composition

- [ ] Implement `CommandExecutor`
- [ ] Implement `QueryExecutor`
- [ ] Define decorator ordering strategy
- [ ] Add pipeline composition tests
- [ ] Document behavior ordering

### Behavior Ordering

- [ ] Define canonical decorator order
- [ ] Implement configurable ordering
- [ ] Add ordering validation
- [ ] Document ordering rationale

### Testing Patterns

- [ ] Create test fixtures for behaviors
- [ ] Add unit tests for each decorator
- [ ] Add integration tests for pipeline
- [ ] Add CancellationToken tests
- [ ] Add Result assertion helpers
- [ ] Document testing strategies

### Performance Validation

- [ ] Benchmark decorator chain overhead
- [ ] Profile memory allocations
- [ ] Optimize hot paths
- [ ] Add performance tests
- [ ] Document performance characteristics

### Documentation Review

- [ ] Review all code examples for accuracy
- [ ] Validate all internal links
- [ ] Check ADR references
- [ ] Ensure consistency with Phase 2
- [ ] Add troubleshooting section

---

## Dependencies & Relationships

### Prerequisites

#### Phase 0: Discovery & Guardrails

- Multi-tenancy requirements
- Security guardrails
- Observability strategy

#### Phase 1: Platform Scaffolding

- Build infrastructure
- Testing framework (xUnit, Shouldly, NSubstitute)
- CI/CD pipelines

#### Phase 2: Domain & Contracts

- `ICommand` and `IQuery` interfaces
- `ICommandHandler` and `IQueryHandler` interfaces
- `Result` and `Result<T>` patterns
- `ITenantContext` interface
- `IUnitOfWork` interface
- Domain exceptions

#### ADR Dependencies

- **ADR-0001**: Tenant context injection in handlers
- **ADR-0002**: Audit context in logging behavior
- **ADR-0005**: No reflection in handler registration

### Outputs to Other Phases

#### Phase 4: Web Adapters

- Command/Query executors for web controllers
- Result-to-HTTP mapping patterns
- Authorization integration

#### Phase 5: Infrastructure

- UnitOfWork implementation for transaction behavior
- Repository implementations for handlers
- Database context configuration

#### Phase 6: Release

- Example applications using pipeline
- Performance benchmarks
- Troubleshooting guides

### External Dependencies

- **FluentValidation** (11.x+) - Input validation
- **Microsoft.Extensions.DependencyInjection** - DI container
- **Microsoft.Extensions.Logging** - Structured logging
- **Microsoft.AspNetCore.Authorization** - Policy-based auth
- **System.Diagnostics.Metrics** - Metrics collection
- **xUnit** (2.6+) - Testing framework
- **Shouldly** (4.2+) - Assertion library
- **NSubstitute** (5.1+) - Mocking framework

---

## Review Schedule

| Review Type | Cadence | Attendees | Purpose |
|-------------|---------|-----------|---------|
| **Architecture Review** | Weekly | Application Architecture Team, Lead Engineer | Validate behavior design and pipeline architecture |
| **Behavior Design Review** | Bi-weekly | Engineering Team | Review decorator implementations |
| **Performance Review** | Milestone-based | Performance Team, Senior Engineers | Assess decorator overhead and optimize |
| **Code Review** | Per PR | Engineering Team | Ensure code quality and ADR compliance |
| **Documentation Review** | End of phase | Tech Docs, Product Engineering | Validate clarity and completeness |

---

## References

### Internal Documentation

- [Phase 0: Discovery & Guardrails](../phase-0-discovery/phase-0-discovery.md)
- [Phase 1: Platform Scaffold](../phase-1-platform/phase-1-platform.md)
- [Phase 2: Domain & Contracts](../phase-2-domain/phase-2-domain.md)
- [Glossary](../glossary.md)
- [CQRS Framework Plan](../cqrs-framework-plan.md)
- [Context README](../README.md)

### Architecture Decision Records

- [ADR-0001: Tenancy Strategy](../../adr/ADR-0001-tenancy-strategy.md) - Tenant context injection
- [ADR-0002: Audit Logging](../../adr/ADR-0002-audit-logging.md) - Audit context in logging
- [ADR-0003: Soft Delete](../../adr/ADR-0003-soft-delete.md) - Entity lifecycle
- [ADR-0005: DI Container Strategy](../../adr/ADR-0005-di-container-strategy.md) - No reflection registration

### External References

#### FluentValidation

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [Integrating FluentValidation with ASP.NET Core](https://docs.fluentvalidation.net/en/latest/aspnet.html)

#### Decorator Pattern

- [Decorator Pattern (Gang of Four)](https://refactoring.guru/design-patterns/decorator)
- [Pipeline Pattern](https://www.dofactory.com/net/pipeline-design-pattern)

#### Authorization

- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/)
- [Policy-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)
- [Resource-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased)

#### Observability

- [Structured Logging with Serilog](https://serilog.net/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [RED Metrics](https://www.weave.works/blog/the-red-method-key-metrics-for-microservices-architecture/)

#### .NET 8

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Open Generic Registration](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#register-groups-of-services-with-extension-methods)

#### Testing

- [xUnit Documentation](https://xunit.net/)
- [Shouldly Documentation](https://docs.shouldly.org/)
- [NSubstitute Documentation](https://nsubstitute.github.io/)

---

## Appendix: Implementation Patterns

### A. Complete Handler Example

```csharp
namespace MyApp.Orders.Commands;

// Command definition
[Transactional]
[Authorize(Policy = Policies.CreateOrder)]
public sealed record CreateOrderCommand(
    string CustomerEmail,
    decimal TotalAmount,
    string Currency) : ICommand<Guid>;

// Validator
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .Length(3);
    }
}

// Handler
public sealed class CreateOrderHandler : CommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;

    public CreateOrderHandler(
        IOrderRepository repository,
        ITenantContext tenantContext,
        ILogger<CreateOrderHandler> logger) 
        : base(tenantContext, logger)
    {
        _repository = repository;
    }

    public override async Task<Result<Guid>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // Create value objects
        var email = Email.Create(command.CustomerEmail);
        if (email.IsFailure)
            return Result<Guid>.Failure(email.Errors);

        var total = Money.Create(command.TotalAmount, command.Currency);
        if (total.IsFailure)
            return Result<Guid>.Failure(total.Errors);

        // Create aggregate
        var order = Order.Create(
            TenantContext.TenantId,
            email.Value,
            total.Value);

        // Persist
        await _repository.AddAsync(order, cancellationToken);

        Logger.LogInformation(
            "Order {OrderId} created for tenant {TenantId}",
            order.Id,
            TenantContext.TenantId);

        return Result<Guid>.Success(order.Id);
    }
}
```

### B. Query Example

```csharp
namespace MyApp.Orders.Queries;

// Query definition
[Authorize(Policy = Policies.ViewOrders)]
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

// Query handler
public sealed class GetOrderQueryHandler : QueryHandler<GetOrderQuery, OrderDto>
{
    private readonly IOrderRepository _repository;

    public GetOrderQueryHandler(
        IOrderRepository repository,
        ITenantContext tenantContext,
        ILogger<GetOrderQueryHandler> logger)
        : base(tenantContext, logger)
    {
        _repository = repository;
    }

    public override async Task<Result<OrderDto>> HandleAsync(
        GetOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(
            query.OrderId,
            cancellationToken);

        if (order is null)
            return Result<OrderDto>.Failure("Order not found", ErrorCode.NotFound);

        // Tenant isolation check
        if (order.TenantId != TenantContext.TenantId)
            return Result<OrderDto>.Failure("Unauthorized", ErrorCode.Unauthorized);

        var dto = OrderDto.FromEntity(order);
        return Result<OrderDto>.Success(dto);
    }
}
```

### C. DI Registration Example

```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Add application layer
builder.Services.AddApplicationLayer();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

// Register handlers explicitly
builder.Services.AddCommandHandler<CreateOrderCommand, Guid, CreateOrderHandler>();
builder.Services.AddQueryHandler<GetOrderQuery, OrderDto, GetOrderQueryHandler>();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CreateOrder, policy =>
        policy.RequireClaim("permission", "orders:create"));
        
    options.AddPolicy(Policies.ViewOrders, policy =>
        policy.RequireClaim("permission", "orders:read"));
});
```

### D. Usage in Web Controller

```csharp
namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ICommandExecutor _commandExecutor;
    private readonly IQueryExecutor _queryExecutor;

    public OrdersController(
        ICommandExecutor commandExecutor,
        IQueryExecutor queryExecutor)
    {
        _commandExecutor = commandExecutor;
        _queryExecutor = queryExecutor;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _commandExecutor.ExecuteAsync(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetOrder), new { id = result.Value }, result.Value)
            : BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = string.Join(", ", result.Errors),
                Status = StatusCodes.Status400BadRequest
            });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetOrderQuery(id);
        var result = await _queryExecutor.ExecuteAsync(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails
            {
                Title = "Order not found",
                Detail = string.Join(", ", result.Errors),
                Status = StatusCodes.Status404NotFound
            });
    }
}
```

### E. Testing Behavior Example

```csharp
namespace Idevs.Application.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task HandleAsync_WhenNoValidators_ShouldCallNext()
    {
        // Arrange
        var command = new TestCommand();
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var logger = Substitute.For<ILogger<ValidationBehavior<TestCommand>>>();
        var behavior = new ValidationBehavior<TestCommand>(validators, logger);
        var nextCalled = false;
        Func<Task<Result>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await behavior.HandleAsync(command, next);

        // Assert
        nextCalled.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ShouldReturnFailure()
    {
        // Arrange
        var command = new TestCommand { Value = -1 };
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("Value", "Value must be positive")
            }));

        var validators = new[] { validator };
        var logger = Substitute.For<ILogger<ValidationBehavior<TestCommand>>>();
        var behavior = new ValidationBehavior<TestCommand>(validators, logger);
        var nextCalled = false;
        Func<Task<Result>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await behavior.HandleAsync(command, next);

        // Assert
        nextCalled.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("Value: Value must be positive");
    }
}

public sealed record TestCommand : ICommand
{
    public int Value { get; init; }
}
```

### F. Performance Benchmarking

```csharp
namespace Idevs.Application.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BehaviorPipelineBenchmarks
{
    private ICommandExecutor _executor = null!;
    private TestCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddApplicationLayer();
        services.AddScoped<ICommandHandler<TestCommand>, TestCommandHandler>();

        var provider = services.BuildServiceProvider();
        _executor = provider.GetRequiredService<ICommandExecutor>();
        _command = new TestCommand();
    }

    [Benchmark(Baseline = true)]
    public async Task DirectHandlerInvocation()
    {
        var handler = new TestCommandHandler();
        await handler.HandleAsync(_command);
    }

    [Benchmark]
    public async Task FullPipelineExecution()
    {
        await _executor.ExecuteAsync(_command);
    }
}
```

---

**Last Updated**: 2025-10-04  
**Document Version**: 1.0  
**Total Lines**: ~1,550
