# Phase 1: Dependency Injection & Decorator Pattern

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Define service registration and decorator pattern using explicit registration (no assembly scanning).

## Design Principle

**"Explicit Over Magic"** - No Autofac, no Scrutor, no assembly scanning

**Decision:** See [ADR-0005: DI Container Strategy](../../adrs/ADR-0005-DI-Container-Strategy.md)

## Why Manual Registration?

1. ✅ **Zero External Dependencies** - Uses built-in Microsoft.Extensions.DependencyInjection
2. ✅ **Better Performance** - No reflection at startup
3. ✅ **Debuggable** - Clear call chains, no magic
4. ✅ **Lower Adoption Barrier** - Works with any .NET application

## Core Abstractions

See [phase-1-platform-ORIGINAL.md](../phase-1-platform-ORIGINAL.md#10-dependency-injection--decorator-pattern) lines 1205-1550 for complete implementation examples.

### Command Handler Interface

```csharp
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

### Decorator Base Class

```csharp
public abstract class CommandHandlerDecoratorBase<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected readonly ICommandHandler<TCommand> Inner;
    
    protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }
    
    public abstract Task<Result> HandleAsync(
        TCommand command, 
        CancellationToken cancellationToken = default);
}
```

## Registration Helpers

### Extension Methods

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommandHandler<TCommand, THandler>(
        this IServiceCollection services,
        bool addValidation = true,
        bool addLogging = true,
        bool addMetrics = false)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        services.AddScoped<ICommandHandler<TCommand>>(sp =>
        {
            ICommandHandler<TCommand> handler = ActivatorUtilities.CreateInstance<THandler>(sp);
            
            // Apply decorators in order
            if (addValidation)
            {
                var validator = sp.GetService<IValidator<TCommand>>();
                if (validator != null)
                {
                    handler = new ValidationCommandHandler<TCommand>(handler, validator);
                }
            }
            
            if (addLogging)
            {
                var logger = sp.GetRequiredService<ILogger<LoggingCommandHandler<TCommand>>>();
                handler = new LoggingCommandHandler<TCommand>(handler, logger);
            }
            
            if (addMetrics)
            {
                var metrics = sp.GetRequiredService<IMetrics>();
                handler = new MetricsCommandHandler<TCommand>(handler, metrics);
            }
            
            return handler;
        });
        
        return services;
    }
}
```

## Consumer Usage

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Idevs core services
builder.Services.AddIdevs();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

// Register handlers explicitly
builder.Services.AddCommandHandler<CreateOrder, CreateOrderHandler>();
builder.Services.AddCommandHandler<UpdateOrder, UpdateOrderHandler>();
builder.Services.AddCommandHandler<DeleteOrder, DeleteOrderHandler>(addValidation: false);

builder.Services.AddQueryHandler<GetOrder, Order, GetOrderHandler>();
```

## Benefits of Explicit Registration

1. ✅ **Clear Dependencies** - See exactly what's registered
2. ✅ **IDE Support** - Navigate with F12
3. ✅ **Compile-Time Safety** - Typos caught early
4. ✅ **Easy to Debug** - Clear call stack
5. ✅ **Opt-Out Capability** - Disable decorators per handler
6. ✅ **Zero External Dependencies** - Built-in .NET only

## Optional: Autofac Adapter

For consumers who want assembly scanning, provide separate package:

**Package:** `Idevs.DependencyInjection.Autofac` (optional)

```csharp
// Usage in consumer app
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule<IdevsAutofacModule>();
});
```

## Summary

| Aspect | Idevs Core | Consumer Apps |
|--------|------------|---------------|
| **DI Container** | MEDI (built-in) | Any choice |
| **Registration** | Explicit helpers | Consumer's choice |
| **Assembly Scanning** | ❌ Not provided | Via optional adapter |
| **Decorators** | ✅ Manual factory | ✅ Works with any DI |

**Design Philosophy:** *"Build for everyone, optimize for you"*

---

[← Back to Phase 1 Overview](../phase-1-platform.md)
