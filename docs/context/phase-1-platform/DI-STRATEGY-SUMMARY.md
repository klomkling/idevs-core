# Dependency Injection Strategy - Summary

**Date**: 2025-10-04  
**Decision**: Use Microsoft.Extensions.DependencyInjection with manual registration  
**Related**: [ADR-0005](../adrs/ADR-0005-DI-Container-Strategy.md), [Phase 1 Documentation](./phase-1-platform.md#10-dependency-injection--decorator-pattern)

---

## TL;DR

✅ **Use MEDI** (built-in .NET DI)  
✅ **Manual factory registration** for decorators  
❌ **No Autofac required**  
❌ **No Scrutor**  
❌ **No assembly scanning**

---

## Decision Summary

### What We Use

| Component | Package | License | Why |
|-----------|---------|---------|-----|
| **DI Container** | `Microsoft.Extensions.DependencyInjection` | MIT (built-in) | Standard .NET, zero friction |
| **Decorator Pattern** | Manual factory registration | N/A (our code) | Explicit, debuggable, zero deps |

### What We Don't Use

| Package | Reason Not Used |
|---------|-----------------|
| **Autofac** | Requires container replacement, has assembly scanning |
| **Scrutor** | Has assembly scanning (conflicts with principles) |
| **App.Metrics** | Archived project, unmaintained |

---

## Key Principles

From Phase 0 Design Principles:

> **"Explicit Over Magic"**
>
> - ✅ Explicit service registration
> - ✅ Source generators acceptable
> - ❌ Assembly scanning
> - ❌ Attribute-based magic registration

---

## Code Examples

### ✅ Correct: Manual Registration

```csharp
// Register handlers explicitly
services.AddCommandHandler<CreateOrder, CreateOrderHandler>();
services.AddCommandHandler<UpdateOrder, UpdateOrderHandler>();
services.AddCommandHandler<DeleteOrder, DeleteOrderHandler>();
```

### ❌ Incorrect: Assembly Scanning

```csharp
// ❌ DON'T DO THIS - conflicts with principles
services.Scan(scan => scan
    .FromAssemblyOf<ICommandHandler>()
    .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
    .AsImplementedInterfaces());
```

---

## Decorator Implementation

### Factory Pattern

```csharp
services.AddScoped<ICommandHandler<CreateOrder>>(sp =>
{
    // 1. Core handler
    ICommandHandler<CreateOrder> handler = 
        ActivatorUtilities.CreateInstance<CreateOrderHandler>(sp);
    
    // 2. Wrap with validation
    handler = new ValidationCommandHandler<CreateOrder>(
        handler, 
        sp.GetRequiredService<IValidator<CreateOrder>>()
    );
    
    // 3. Wrap with logging
    handler = new LoggingCommandHandler<CreateOrder>(
        handler,
        sp.GetRequiredService<ILogger<LoggingCommandHandler<CreateOrder>>>()
    );
    
    return handler;
});
```

### Helper Extension Method

```csharp
// Cleaner usage
services.AddCommandHandler<CreateOrder, CreateOrderHandler>();
services.AddCommandHandler<UpdateOrder, UpdateOrderHandler>();
```

---

## For Your Personal Apps

If you want Autofac in your personal applications (retail, billing, finance):

### Option 1: Use the Autofac Adapter (Future)

```csharp
// Install optional package
dotnet add package Idevs.DependencyInjection.Autofac

// Use Autofac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule<IdevsAutofacModule>(); // Pre-configured
});
```

### Option 2: Keep Using Explicit Registration

```csharp
// Your apps can still use manual registration
builder.Services.AddIdevs();
builder.Services.AddCommandHandler<CreateOrder, CreateOrderHandler>();
// ... more handlers
```

---

## Benefits

### For Framework Consumers

1. ✅ **Zero Friction**: Works with any .NET app out of the box
2. ✅ **Standard .NET**: Familiar to all .NET developers
3. ✅ **No Container Replacement**: Keep using MEDI
4. ✅ **Clear Dependencies**: Explicit registration shows what's used
5. ✅ **Easy Debugging**: No magic, clear call stacks

### For Framework Maintainers

1. ✅ **Zero External Dependencies**: Only built-in .NET APIs
2. ✅ **No License Concerns**: Everything is MIT/Apache 2.0
3. ✅ **Future-Proof**: Microsoft guarantees MEDI support
4. ✅ **Aligns with Principles**: "Explicit Over Magic"
5. ✅ **Better Performance**: No reflection-based assembly scanning

---

## Package Structure

### Core Framework (Required)

```
Idevs                     → Uses MEDI, manual registration
Idevs.Application         → Uses MEDI, manual registration
Idevs.Data                → Uses MEDI, manual registration
Idevs.Web                 → Uses MEDI, manual registration
```

**Dependencies**: Zero external DI packages

### Optional Adapters (Future)

```
Idevs.DependencyInjection.Autofac  → Optional Autofac support
```

**Use Case**: For consumers who must use Autofac

---

## Migration Path

### Current State (Your Apps)

Your personal apps likely use Autofac today:

```csharp
builder.RegisterAssemblyTypes(assembly)
    .Where(t => t.Name.EndsWith("Handler"))
    .AsImplementedInterfaces();
```

### Future State (Two Options)

#### Option A: Switch to Explicit (Recommended for learning)

```csharp
builder.Services.AddCommandHandler<CreateOrder, CreateOrderHandler>();
builder.Services.AddCommandHandler<UpdateOrder, UpdateOrderHandler>();
// ... more handlers
```

**Benefits**: Learn the pattern, better debugging, clearer code

#### Option B: Use Autofac Adapter (Keep current style)

```csharp
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule<IdevsAutofacModule>(); // Handles Idevs types
    container.RegisterModule<YourAppModule>();       // Your app's types
});
```

**Benefits**: Minimal changes to existing apps

---

## Frequently Asked Questions

### Q: Why not use Scrutor? It's MIT licensed

**A**: Scrutor includes assembly scanning functionality, which conflicts with our "Explicit Over Magic" principle. While you don't have to use the scanning features, having them available goes against our design philosophy.

### Q: Isn't manual registration tedious?

**A**: We provide helper methods (`AddCommandHandler`, `AddQueryHandler`) that reduce boilerplate. The explicitness is intentional and beneficial for:

- IDE navigation (F12 works)
- Compile-time safety
- Clear dependency graph
- Easy debugging

### Q: Can I still use Autofac in my apps?

**A**: Yes! Two ways:

1. Use the optional `Idevs.DependencyInjection.Autofac` adapter package
2. Register Idevs handlers manually in your Autofac modules

The core framework doesn't force you to use MEDI in your apps - it just uses MEDI internally.

### Q: What about Source Generators?

**A**: Source generators are acceptable per our principles. If manual registration becomes too verbose, we can create a source generator to auto-generate the registration code at compile time. This gives us the best of both worlds: explicit registration with minimal boilerplate.

---

## References

- [ADR-0005: DI Container Strategy](../adrs/ADR-0005-DI-Container-Strategy.md)
- [Phase 1: Platform Scaffolding](./phase-1-platform.md)
- [LICENSE-REVIEW.md](./LICENSE-REVIEW.md)
- [Phase 0: Design Principles](../phase-0-discovery/phase-0-discovery.md)

---

## Approval Status

- [x] Aligns with "Explicit Over Magic" principle
- [x] Zero licensing concerns
- [x] Zero external dependencies
- [x] Lower adoption barrier for consumers
- [x] Optional Autofac support for existing apps

**Status**: ✅ **Approved**

---

**Design Philosophy**: *"Build for everyone, optimize for you"*
