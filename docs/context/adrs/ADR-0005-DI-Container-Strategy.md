# ADR-0005: Dependency Injection Container Strategy

**Status**: Accepted  
**Date**: 2025-10-04  
**Decision Date**: 2025-10-05  
**Deciders**: Architecture Team, Platform Lead  
**Related**: ADR-0001 (Tenancy), Phase 0 (Design Principles)

---

## Context

The Idevs framework is a **library/building-block framework** intended for reuse across multiple first-party applications (retail, billing, finance) and potentially by external consumers via NuGet. A key architectural decision is whether to:

1. **Use Microsoft.Extensions.DependencyInjection (MEDI)** - The standard .NET DI container
2. **Require Autofac** - A more feature-rich third-party DI container
3. **Abstract the DI container** - Allow consumers to choose

### Current Design Principles (from Phase 0)

From `phase-0-discovery.md`:

> **E. Explicit Over Magic**
> - Principle: Prefer explicit code over convention-based discovery
> - Rationale: Avoids System.Reflection, improves startup performance, better debugging
> - Examples:
>   - ✅ Explicit service registration: `services.AddScoped<IFooHandler, FooHandler>()`
>   - ✅ Source generators for boilerplate (considered acceptable)
>   - ❌ Assembly scanning for handlers
>   - ❌ Attribute-based magic registration

### The Question

The maintainer typically uses **Autofac** for advanced IoC features like:
- Assembly scanning
- Decorator registration
- Named/keyed services
- Module pattern
- Advanced lifetime scopes

**Should the Idevs framework require or recommend Autofac?**

---

## Decision

**Use Microsoft.Extensions.DependencyInjection (MEDI) as the primary DI container, with NO required dependency on Autofac.**

The framework will:
1. ✅ Provide extension methods for `IServiceCollection` (standard .NET)
2. ✅ Support explicit service registration
3. ✅ Optionally provide an `Idevs.DependencyInjection.Autofac` package for those who want Autofac features
4. ❌ NOT require Autofac for core functionality

---

## Rationale

### 1. **Framework Nature: Library vs Application**

**Idevs is a library framework**, not an application:

| Aspect | Library (Idevs) | Application (Your Apps) |
|--------|-----------------|-------------------------|
| **Control** | Consumer controls DI | You control everything |
| **Flexibility** | Must support various DI containers | Can mandate Autofac |
| **Distribution** | NuGet package | Deployed binary |
| **Adoption** | Low friction = more users | Can enforce preferences |

**Analysis**: Forcing Autofac on consumers creates **high friction** and limits adoption.

---

### 2. **Design Principle Alignment: "Explicit Over Magic"**

Your framework explicitly states:
- ❌ **Avoid**: Assembly scanning, attribute-based registration
- ✅ **Prefer**: Explicit registration

**Autofac's strengths** (assembly scanning, module auto-discovery) **conflict** with this principle.

#### Example: Autofac Module (Magic)

```csharp
// ❌ Conflicts with "Explicit Over Magic"
builder.RegisterAssemblyModules(typeof(IdevsModule).Assembly);
builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
    .Where(t => t.Name.EndsWith("Handler"))
    .AsImplementedInterfaces();
```

#### Example: MEDI (Explicit)

```csharp
// ✅ Aligns with "Explicit Over Magic"
services.AddScoped<ICreateOrderHandler, CreateOrderHandler>();
services.AddScoped<IGetOrderHandler, GetOrderHandler>();
services.Decorate<ICommandHandler<CreateOrder>, LoggingCommandHandler<CreateOrder>>();
```

---

### 3. **Ecosystem Compatibility**

#### .NET Ecosystem Standard

- **ASP.NET Core**: Uses MEDI by default
- **Minimal APIs**: Uses MEDI
- **Blazor**: Uses MEDI
- **Azure Functions (isolated)**: Uses MEDI
- **.NET Generic Host**: Uses MEDI
- **Microsoft.Extensions.*** packages**: All use MEDI

**Autofac is an outlier** in the modern .NET ecosystem.

#### Consumer Friction

If Idevs requires Autofac:

```csharp
// ❌ Consumer must replace built-in DI
var builder = WebApplication.CreateBuilder(args);

// Remove standard DI
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Configure Autofac (unfamiliar to many developers)
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule<IdevsAutofacModule>();
});
```

If Idevs uses MEDI:

```csharp
// ✅ Natural integration
var builder = WebApplication.CreateBuilder(args);

// Just works
builder.Services.AddIdevs(options => { /* config */ });
```

---

### 4. **Decorator Pattern: MEDI vs Autofac vs Scrutor**

Multiple options exist for decorator registration. Let's compare them:

#### Option A: MEDI Native (Manual Factory)

```csharp
services.AddScoped<ICommandHandler<CreateOrder>>(sp =>
{
    var handler = ActivatorUtilities.CreateInstance<CreateOrderHandler>(sp);
    var validated = new ValidationCommandHandler<CreateOrder>(handler, sp.GetRequiredService<IValidator<CreateOrder>>());
    var logged = new LoggingCommandHandler<CreateOrder>(validated, sp.GetRequiredService<ILogger>());
    return logged;
});
```

**Pros**: Zero dependencies, explicit, debuggable  
**Cons**: Verbose (mitigated by helper methods)

#### Option B: Scrutor

```csharp
services.AddScoped<ICommandHandler<CreateOrder>, CreateOrderHandler>();
services.Decorate<ICommandHandler<CreateOrder>, ValidationCommandHandler<CreateOrder>>();
services.Decorate<ICommandHandler<CreateOrder>, LoggingCommandHandler<CreateOrder>>();
```

**Pros**: Clean syntax  
**Cons**: **Includes assembly scanning**, external dependency

**Critical Issue with Scrutor**:

Scrutor's README prominently features assembly scanning:

```csharp
// From Scrutor docs - this is a core feature
collection.Scan(scan => scan
    .FromAssemblyOf<ITransientService>()
    .AddClasses(classes => classes.AssignableTo<ITransientService>())
    .AsImplementedInterfaces()
    .WithTransientLifetime());
```

While you can use Scrutor *only* for decorators, the package:
1. **Advertises assembly scanning as a primary feature**
2. **Includes the scanning API in the dependency tree**
3. **Conflicts with our "Explicit Over Magic" principle**

From our Phase 0 principles:
> **E. Explicit Over Magic**
> - ❌ Assembly scanning for handlers

Having a package with assembly scanning capabilities goes against this principle, even if we don't use that feature.

#### Option C: .NET 8 Keyed Services

```csharp
services.AddKeyedScoped<ICommandHandler<CreateOrder>, CreateOrderHandler>("base");
services.AddScoped<ICommandHandler<CreateOrder>>(sp =>
{
    var baseHandler = sp.GetRequiredKeyedService<ICommandHandler<CreateOrder>>("base");
    var validated = new ValidationCommandHandler<CreateOrder>(baseHandler, ...);
    var logged = new LoggingCommandHandler<CreateOrder>(validated, ...);
    return logged;
});
```

**Pros**: Built-in (.NET 8+), zero dependencies  
**Cons**: Slightly verbose

#### Option D: Autofac

```csharp
builder.RegisterType<CreateOrderHandler>().As<ICommandHandler<CreateOrder>>();
builder.RegisterDecorator<ValidatingCommandHandler<CreateOrder>, ICommandHandler<CreateOrder>>();
builder.RegisterDecorator<LoggingCommandHandler<CreateOrder>, ICommandHandler<CreateOrder>>();
```

**Pros**: Clean syntax, powerful features  
**Cons**: Requires full container replacement, has assembly scanning

#### Decision: Option A (Manual Factory) + Helper Methods

**Why not Scrutor?**

1. **Principle Conflict**: Has assembly scanning capabilities
2. **Not Needed**: We can achieve clean syntax with helper methods
3. **External Dependency**: One more package to audit
4. **Philosophy**: If we're avoiding assembly scanning, why have it in our dependency tree?

**Our Approach**:

```csharp
// Helper method wraps the verbosity
services.AddCommandHandler<CreateOrder, CreateOrderHandler>();

// Internally does the manual factory registration
// Zero external dependencies, explicit, debuggable
```

**Verdict**: Manual factory with helpers provides clean syntax without external dependencies or assembly scanning.

---

### 5. **Licensing & Maintenance**

| Aspect | MEDI | Autofac |
|--------|------|---------|
| **License** | MIT (built-in .NET) | MIT |
| **Maintenance** | Microsoft-backed | Community-maintained |
| **Breaking Changes** | Rare | Occasional |
| **Future-proof** | Guaranteed (core .NET) | Depends on community |
| **Security Updates** | Microsoft Security Response Center | Community effort |

**Risk**: Autofac is community-maintained. If it becomes unmaintained (like MediatR discussions), consumers are stuck.

---

### 6. **Performance**

#### Startup Performance

| Container | Startup Time | Reason |
|-----------|-------------|--------|
| **MEDI** | Fast | Minimal reflection, explicit registration |
| **Autofac** | Slower | Assembly scanning, dynamic proxies |

From your design principles:
> Avoids System.Reflection, improves startup performance, better debugging

**Autofac uses heavy reflection** for its advanced features.

#### Runtime Performance

Both are similar at runtime. The difference is primarily at startup.

---

### 7. **Real-World Usage Patterns**

#### Your Personal Projects (Autofac)

For **your own applications** where you control everything:
- ✅ Use Autofac if you prefer it
- ✅ Assembly scanning is fine
- ✅ Modules are convenient

#### Idevs Framework (Library for Others)

For a **library consumed by others**:
- ✅ Use standard .NET patterns
- ✅ Low friction for adoption
- ✅ Works with any .NET host

---

## Consequences

### ✅ Positive Consequences

1. **Lower Adoption Barrier**
   - Works with any .NET application out of the box
   - No need to replace default DI container
   - Familiar to most .NET developers

2. **Ecosystem Alignment**
   - Follows .NET conventions
   - Works seamlessly with ASP.NET Core, Minimal APIs, Azure Functions
   - Compatible with all `Microsoft.Extensions.*` packages

3. **Future-Proof**
   - Microsoft guarantees MEDI support
   - No risk of third-party abandonment
   - Consistent with .NET evolution

4. **Design Principle Alignment**
   - Enforces "Explicit Over Magic"
   - Discourages assembly scanning
   - Better startup performance

5. **Simplicity**
   - Less cognitive load for consumers
   - Easier debugging (no dynamic proxies)
   - Clear dependency graph

### ⚠️ Negative Consequences (Mitigations)

1. **Less Convenient for Complex Registration**
   - **Mitigation**: Provide `AddIdevs()` extension methods that encapsulate complexity
   - **Example**:
     ```csharp
     services.AddIdevs(options =>
     {
         options.AddHandlers(typeof(CreateOrderHandler).Assembly);
         options.AddDecorators();
         options.AddTenantContext();
     });
     ```

2. **No Built-in Assembly Scanning**
   - **Mitigation**: Provide optional source generator for handler registration
   - **Alternative**: Explicit registration is better anyway (design principle)

3. **Decorator Registration Verbosity**
   - **Mitigation**: Use Scrutor (MIT) or built-in .NET 7+ decorators
   - **Example**:
     ```csharp
     services.AddIdevsDecorators(); // Adds logging, validation, caching decorators
     ```

---

## Optional: Autofac Adapter Package

For consumers who **must** use Autofac (e.g., your existing apps), provide:

**Package**: `Idevs.DependencyInjection.Autofac` (optional, separate NuGet)

```csharp
// Optional Autofac integration
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule<IdevsAutofacModule>(); // Pre-built module
});
```

**Benefits**:
- Core framework doesn't require Autofac
- Consumers who want Autofac can opt-in
- Best of both worlds

---

## Implementation Strategy

### Phase 1: Core Registration (MEDI)

```csharp
// Idevs/IServiceCollectionExtensions.cs
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddIdevs(
        this IServiceCollection services, 
        Action<IdevsOptions>? configure = null)
    {
        var options = new IdevsOptions();
        configure?.Invoke(options);
        
        // Register core services
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register handlers (explicit - no scanning)
        if (options.RegisterHandlers)
        {
            RegisterHandlers(services);
        }
        
        // Register decorators
        if (options.UseDecorators)
        {
            RegisterDecorators(services);
        }
        
        return services;
    }
    
    private static void RegisterHandlers(IServiceCollection services)
    {
        // Explicit registration (or use source generator)
        services.AddScoped<ICommandHandler<CreateOrder>, CreateOrderHandler>();
        services.AddScoped<IQueryHandler<GetOrder, Order>, GetOrderHandler>();
        // ... more handlers
    }
    
    private static void RegisterDecorators(IServiceCollection services)
    {
        // Option 1: Use Scrutor
        services.Decorate(typeof(ICommandHandler<>), typeof(LoggingCommandHandler<>));
        services.Decorate(typeof(ICommandHandler<>), typeof(ValidationCommandHandler<>));
        
        // Option 2: Manual registration
        // services.AddScoped<ICommandHandler<CreateOrder>>(sp =>
        // {
        //     var inner = ActivatorUtilities.CreateInstance<CreateOrderHandler>(sp);
        //     var logged = new LoggingCommandHandler<CreateOrder>(inner);
        //     return new ValidationCommandHandler<CreateOrder>(logged);
        // });
    }
}
```

### Phase 2 (Optional): Autofac Adapter

```csharp
// Idevs.DependencyInjection.Autofac/IdevsAutofacModule.cs
public class IdevsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Assembly scanning for convenience
        builder.RegisterAssemblyTypes(typeof(ICommandHandler<>).Assembly)
            .Where(t => t.Name.EndsWith("Handler"))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        
        // Decorators
        builder.RegisterDecorator<LoggingCommandHandler<>, ICommandHandler<>>();
        builder.RegisterDecorator<ValidationCommandHandler<>, ICommandHandler<>>();
    }
}
```

---

## Alternatives Considered

### Alternative 1: Require Autofac

**Pros**:
- More convenient for complex scenarios
- Assembly scanning out of the box
- Module pattern

**Cons**:
- ❌ Forces consumers to replace default DI
- ❌ Conflicts with "Explicit Over Magic" principle
- ❌ Higher adoption barrier
- ❌ Not standard .NET practice
- ❌ Requires full container replacement

**Verdict**: ❌ Rejected

---

### Alternative 2: Use MEDI + Scrutor

**Package**: `Scrutor` (https://github.com/khellang/Scrutor)  
**License**: MIT

**Pros**:
- Clean decorator syntax
- Works with `IServiceCollection` (no container replacement)
- MIT licensed
- Well-maintained (4.2k+ stars)

**Cons**:
- ❌ **Includes assembly scanning** - Scrutor's main feature alongside decorators:
  ```csharp
  services.Scan(scan => scan
      .FromAssemblyOf<ITransientService>()
      .AddClasses(classes => classes.AssignableTo<ITransientService>())
      .AsImplementedInterfaces());
  ```
- ❌ **Conflicts with "Explicit Over Magic" principle** - Even if we don't use the scanning feature, having it available goes against our design philosophy
- ❌ **External dependency** - One more package to audit and maintain
- ❌ **Not needed** - .NET 8.0 provides everything we need (keyed services, factory pattern)

**Why this conflicts with our principles**:

From Phase 0 Design Principles:
> **E. Explicit Over Magic**
> - ❌ Assembly scanning for handlers
> - ❌ Attribute-based magic registration

Scrutor's primary value proposition is assembly scanning + decorators. If we only use decorators, we're:
1. Adding a dependency for 50% of its functionality
2. Having assembly scanning capabilities in the dependency tree (even if unused)
3. Missing the opportunity to enforce explicit registration

**Verdict**: ❌ Rejected (has assembly scanning, conflicts with principles)

---

### Alternative 3: Abstract DI Container

Create `Idevs.Abstractions.DI` with `IServiceRegistrar` interface.

**Pros**:
- Ultimate flexibility
- Support any container

**Cons**:
- ❌ Over-engineering
- ❌ Additional abstraction layer
- ❌ Maintenance burden
- ❌ YAGNI (You Ain't Gonna Need It)

**Verdict**: ❌ Rejected (over-engineering)

---

### Alternative 4: Manual Factory Registration (Chosen)

Use MEDI with manual factory registration for decorators.

**Pros**:
- ✅ Standard .NET (built-in)
- ✅ Zero external dependencies
- ✅ Decorator support via factory pattern
- ✅ Explicit registration (aligns with principles)
- ✅ No assembly scanning capabilities
- ✅ Better startup performance
- ✅ Easier debugging

**Cons**:
- More verbose than Scrutor (mitigated by helper methods)

**Verdict**: ✅ **Selected**

---

## Recommendation for Your Personal Apps

### For Idevs Framework (Library)
- ✅ Use MEDI + Scrutor
- ✅ Provide optional Autofac adapter package

### For Your Applications (Retail, Billing, Finance)
- ✅ Use Autofac if you prefer it
- ✅ Use the `Idevs.DependencyInjection.Autofac` adapter package
- ✅ Enjoy assembly scanning and modules

**Best of Both Worlds**:
- Framework is accessible to everyone (MEDI)
- Your apps can still use Autofac via adapter

---

## Decision Matrix

| Criteria | MEDI (Manual) | MEDI + Scrutor | Autofac | Weight | Winner |
|----------|---------------|----------------|---------|--------|--------|
| Adoption Barrier | Low | Low | High | 🔥🔥🔥 | MEDI |
| Ecosystem Fit | Perfect | Perfect | Requires swap | 🔥🔥🔥 | MEDI/Scrutor |
| Design Principles | ✅ Explicit | ⚠️ Has scanning | ⚠️ Magic | 🔥🔥🔥 | **MEDI** |
| Performance | Fast | Fast | Slower | 🔥 | MEDI/Scrutor |
| External Dependencies | Zero | 1 package | 2+ packages | 🔥🔥 | **MEDI** |
| Feature Richness | Basic | Good | Advanced | 🔥 | Autofac |
| Future-Proof | Guaranteed | Community | Community | 🔥🔥 | **MEDI** |
| License Safety | Built-in | MIT (safe) | MIT (safe) | 🔥 | Tie |
| Assembly Scanning | ❌ None | ⚠️ Available | ⚠️ Core feature | 🔥🔥🔥 | **MEDI** |

**Weighted Score**: MEDI (Manual) wins

---

## References

- [Microsoft.Extensions.DependencyInjection Docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Scrutor (Decorator Support)](https://github.com/khellang/Scrutor)
- [Autofac Documentation](https://autofac.readthedocs.io/)
- [ADR Template](https://github.com/joelparkerhenderson/architecture-decision-record)

---

## Approval

- [ ] Architecture Team
- [ ] Platform Lead
- [ ] Security Review (if applicable)

---

## Summary

### Core Framework Decision

✅ **Use Microsoft.Extensions.DependencyInjection (MEDI) as the primary DI container**  
✅ **Use manual factory registration for decorators (zero external dependencies)**  
❌ **Do NOT use Autofac** (optional adapter package for consumers only)  
❌ **Do NOT use Scrutor** (has assembly scanning, conflicts with principles)  
❌ **Do NOT use any assembly scanning packages**  

### Why This Decision?

1. **Aligns with "Explicit Over Magic"**: No assembly scanning, explicit registration
2. **Zero External Dependencies**: Only built-in .NET APIs
3. **Lower Adoption Barrier**: Works with any .NET application
4. **Future-Proof**: Microsoft guarantees MEDI support
5. **No Licensing Concerns**: Everything is MIT (built-in)

### Packages NOT Used

| Package | Reason for Exclusion |
|---------|---------------------|
| **Autofac** | Requires full container replacement, includes assembly scanning |
| **Scrutor** | Includes assembly scanning (conflicts with design principles) |
| **App.Metrics** | Project archived/unmaintained |
| **Any assembly scanner** | Violates "Explicit Over Magic" principle |

### Consumer Flexibility

✅ **Your personal apps can still use Autofac via the adapter**  
✅ **Framework stays accessible to all .NET developers**  
✅ **Consumers can choose their DI container**  
✅ **Core framework remains unopinionated**  

**Tagline**: *"Build for everyone, optimize for you"*
