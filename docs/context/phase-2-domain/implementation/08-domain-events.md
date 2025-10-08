# 08: Domain Events

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 08 of 09  
> **Estimated Time:** 2-3 hours

## Overview

Define domain event contracts and infrastructure for publishing events after aggregate state changes, enabling eventual consistency, decoupled business logic, and event-driven architecture patterns.

## Prerequisites

- Phase 1 complete (solution structure)
- Guide 05 complete (aggregates with event raising)
- Understanding of domain events and event-driven architecture
- Familiarity with pub/sub patterns

## Objectives

### What You'll Build

1. **IDomainEvent** - Marker interface for all domain events
2. **IDomainEventHandler<TEvent>** - Handler interface for event processing
3. **DomainEventDispatcher** - Dispatches events after persistence
4. **Event metadata** - Timestamp, correlation ID, tenant context
5. **Example handlers** - Event logging, notifications

### Why This Matters

- Decouples aggregate logic from side effects
- Enables eventual consistency across aggregates
- Supports integration with external systems
- Provides audit trail of domain changes
- Facilitates event sourcing patterns

## Implementation Steps

### 1. Create Events Folder

```bash
mkdir -p src/Idevs/Domain/Events
```

### 2. Define IDomainEvent Interface

**File:** `src/Idevs/Domain/Events/IDomainEvent.cs`

```csharp path=null start=null
namespace Idevs.Domain.Events;

/// <summary>
/// Marker interface for all domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// UTC timestamp when the event occurred
    /// </summary>
    DateTimeOffset OccurredAt => DateTimeOffset.UtcNow;

    /// <summary>
    /// Correlation ID for tracing related events
    /// </summary>
    string? CorrelationId => null;
}
```

**Design Rationale:**
- Marker interface for runtime discovery
- Default implementation for OccurredAt (auto-populated)
- Optional CorrelationId for distributed tracing
- Records (not classes) recommended for immutability

### 3. Define IDomainEventHandler Interface

**File:** `src/Idevs/Domain/Events/IDomainEventHandler.cs`

```csharp path=null start=null
namespace Idevs.Domain.Events;

/// <summary>
/// Handler for processing domain events
/// </summary>
/// <typeparam name="TEvent">Type of domain event to handle</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
```

**Design Rationale:**
- Contravariant `in TEvent` for flexible handler registration
- Async by default (most handlers do I/O)
- CancellationToken for long-running handlers
- Multiple handlers can process same event

### 4. Create Base Domain Event

**File:** `src/Idevs/Domain/Events/DomainEventBase.cs`

```csharp path=null start=null
namespace Idevs.Domain.Events;

/// <summary>
/// Base record for domain events with metadata
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    protected DomainEventBase()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// UTC timestamp when the event occurred
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Correlation ID for tracing related events
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Tenant ID if event is tenant-scoped
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// User ID who triggered the event
    /// </summary>
    public string? UserId { get; init; }
}
```

**Design Rationale:**
- Record type for immutability and value equality
- Auto-populated EventId and OccurredAt
- Optional metadata (TenantId, UserId) for tracing
- Protected constructor for derived types only

### 5. Create Event Dispatcher Interface

**File:** `src/Idevs/Domain/Events/IDomainEventDispatcher.cs`

```csharp path=null start=null
namespace Idevs.Domain.Events;

/// <summary>
/// Dispatches domain events to registered handlers
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a single domain event to all registered handlers
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches multiple domain events to all registered handlers
    /// </summary>
    Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
```

**Design Rationale:**
- Abstract interface (implementation in Phase 3)
- Batch dispatch for performance
- CancellationToken support
- Will be injected into UnitOfWork

### 6. Create In-Memory Dispatcher (For Testing)

**File:** `src/Idevs/Domain/Events/InMemoryDomainEventDispatcher.cs`

```csharp path=null start=null
using Microsoft.Extensions.DependencyInjection;

namespace Idevs.Domain.Events;

/// <summary>
/// Simple in-memory domain event dispatcher
/// </summary>
public sealed class InMemoryDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryDomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        // Get all handlers for this event type
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
            if (handleMethod != null)
            {
                var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                await task;
            }
        }
    }

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
```

**Design Rationale:**
- Uses reflection for dynamic handler discovery
- Dispatches sequentially (consider parallel in production)
- Supports multiple handlers per event
- Phase 3 will add better implementation (MediatR)

### 7. Create Example Event Handlers

**File:** `src/Idevs/Domain/Events/Handlers/DomainEventLoggingHandler.cs`

```csharp path=null start=null
using Microsoft.Extensions.Logging;

namespace Idevs.Domain.Events.Handlers;

/// <summary>
/// Logs all domain events for debugging/audit
/// </summary>
public sealed class DomainEventLoggingHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    private readonly ILogger<DomainEventLoggingHandler<TEvent>> _logger;

    public DomainEventLoggingHandler(ILogger<DomainEventLoggingHandler<TEvent>> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Domain Event: {EventType} | Time: {OccurredAt} | CorrelationId: {CorrelationId}",
            typeof(TEvent).Name,
            domainEvent.OccurredAt,
            domainEvent.CorrelationId);

        return Task.CompletedTask;
    }
}
```

**File:** `src/Idevs/Domain/Events/Handlers/CustomerCreatedEventHandler.cs`

```csharp path=null start=null
using Idevs.Domain.Events.Examples;
using Microsoft.Extensions.Logging;

namespace Idevs.Domain.Events.Handlers;

/// <summary>
/// Example handler for CustomerCreatedEvent
/// </summary>
public sealed class CustomerCreatedEventHandler : IDomainEventHandler<CustomerCreatedEvent>
{
    private readonly ILogger<CustomerCreatedEventHandler> _logger;

    public CustomerCreatedEventHandler(ILogger<CustomerCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        CustomerCreatedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "New customer created: {CustomerId} - {Email} - {FullName}",
            domainEvent.CustomerId,
            domainEvent.Email,
            domainEvent.FullName);

        // In real implementation:
        // - Send welcome email
        // - Create default preferences
        // - Notify sales team
        // - Update analytics

        return Task.CompletedTask;
    }
}
```

**Design Rationale:**
- Handlers are independent (one failure doesn't affect others)
- Logging for observability
- Side effects isolated from domain logic
- Can add transactional outbox pattern later

### 8. Write Unit Tests

**File:** `tests/Idevs.Tests/Domain/DomainEventTests.cs`

```csharp path=null start=null
using Idevs.Domain.Events;
using Idevs.Domain.Events.Examples;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Domain;

public class DomainEventTests
{
    private record TestEvent(string Data) : IDomainEvent;

    [Fact]
    public void IDomainEvent_HasDefaultOccurredAt()
    {
        var beforeEvent = DateTimeOffset.UtcNow;
        var domainEvent = new TestEvent("test");
        var afterEvent = DateTimeOffset.UtcNow;

        domainEvent.OccurredAt.ShouldBeGreaterThanOrEqualTo(beforeEvent);
        domainEvent.OccurredAt.ShouldBeLessThanOrEqualTo(afterEvent);
    }

    [Fact]
    public void DomainEventBase_AutoPopulatesMetadata()
    {
        var eventBase = new CustomerCreatedEvent(
            Guid.NewGuid(),
            "test@example.com",
            "Test User");

        // eventBase.EventId.ShouldNotBe(Guid.Empty);
        eventBase.OccurredAt.ShouldNotBe(default);
    }

    [Fact]
    public void DomainEventBase_SupportsCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString();
        var domainEvent = new CustomerCreatedEvent(
            Guid.NewGuid(),
            "test@example.com",
            "Test User");

        // Records allow with expressions
        var eventWithCorrelation = domainEvent with { CorrelationId = correlationId };

        eventWithCorrelation.CorrelationId.ShouldBe(correlationId);
    }
}
```

**File:** `tests/Idevs.Tests/Domain/DomainEventDispatcherTests.cs`

```csharp path=null start=null
using Idevs.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Domain;

public class DomainEventDispatcherTests
{
    private record TestEvent(string Data) : IDomainEvent;

    private class TestEventHandler : IDomainEventHandler<TestEvent>
    {
        public static int CallCount { get; set; }

        public Task HandleAsync(TestEvent domainEvent, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task InMemoryDispatcher_DispatchesSingleEvent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IDomainEventHandler<TestEvent>, TestEventHandler>();
        var provider = services.BuildServiceProvider();

        var dispatcher = new InMemoryDomainEventDispatcher(provider);
        TestEventHandler.CallCount = 0;

        // Act
        await dispatcher.DispatchAsync(new TestEvent("test"));

        // Assert
        TestEventHandler.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task InMemoryDispatcher_DispatchesMultipleEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IDomainEventHandler<TestEvent>, TestEventHandler>();
        var provider = services.BuildServiceProvider();

        var dispatcher = new InMemoryDomainEventDispatcher(provider);
        TestEventHandler.CallCount = 0;

        var events = new[]
        {
            new TestEvent("test1"),
            new TestEvent("test2"),
            new TestEvent("test3")
        };

        // Act
        await dispatcher.DispatchAsync(events);

        // Assert
        TestEventHandler.CallCount.ShouldBe(3);
    }

    [Fact]
    public async Task InMemoryDispatcher_SupportsMultipleHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IDomainEventHandler<TestEvent>, TestEventHandler>();
        services.AddTransient<IDomainEventHandler<TestEvent>, TestEventHandler>();
        var provider = services.BuildServiceProvider();

        var dispatcher = new InMemoryDomainEventDispatcher(provider);
        TestEventHandler.CallCount = 0;

        // Act
        await dispatcher.DispatchAsync(new TestEvent("test"));

        // Assert
        TestEventHandler.CallCount.ShouldBe(2); // Both handlers called
    }
}
```

### 9. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~DomainEvent"
```

Expected: ✅ All 6+ tests passing

### 10. Document Event Patterns

**File:** `src/Idevs/Domain/Events/README.md`

```markdown
# Domain Events

## Event Naming Conventions

Events should be named in past tense (e.g., `CustomerCreated`, not `CreateCustomer`):
```csharp
public record CustomerCreatedEvent(Guid CustomerId, string Email) : IDomainEvent;
public record OrderPlacedEvent(Guid OrderId, Guid CustomerId, decimal Total) : IDomainEvent;
public record PaymentReceivedEvent(Guid PaymentId, decimal Amount) : IDomainEvent;
```

## Raising Events in Aggregates

```csharp
public class Order : AggregateRoot<Guid>
{
    public Result<Order> PlaceOrder()
    {
        // Business logic
        Status = OrderStatus.Placed;
        PlacedAt = DateTimeOffset.UtcNow;

        // Raise event
        RaiseDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount));

        return this;
    }
}
```

## Implementing Event Handlers

```csharp
public class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IInventoryService _inventoryService;

    public async Task HandleAsync(
        OrderPlacedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        // Send confirmation email
        await _emailService.SendOrderConfirmationAsync(
            domainEvent.CustomerId,
            domainEvent.OrderId);

        // Reserve inventory
        await _inventoryService.ReserveItemsAsync(
            domainEvent.OrderId,
            cancellationToken);
    }
}
```

## Registering Handlers (DI)

### Manual Registration
```csharp
services.AddTransient<IDomainEventHandler<OrderPlacedEvent>, OrderPlacedEventHandler>();
services.AddTransient<IDomainEventHandler<OrderPlacedEvent>, InventoryUpdateHandler>();
```

### Assembly Scanning (Phase 3)
```csharp
services.AddDomainEventHandlers(typeof(OrderPlacedEvent).Assembly);
```

## Event Dispatch Flow

1. **Aggregate raises event** → Stored in `DomainEvents` collection
2. **Repository persists aggregate** → Events still in memory
3. **UnitOfWork commits transaction** → Events dispatched after successful commit
4. **Dispatcher finds handlers** → All registered handlers invoked
5. **Aggregate events cleared** → Ready for next operation

## Best Practices

### ✅ DO
- Use past tense event names
- Keep events immutable (records)
- Include all relevant data in event
- Handle events idempotently (may be called multiple times)
- Log event processing for observability

### ❌ DON'T
- Access database in event constructor
- Modify aggregate state in event handler
- Throw exceptions from handlers (use logging instead)
- Use events for synchronous validation
- Include complex objects (use IDs instead)

## Testing Event Handlers

```csharp
[Fact]
public async Task OrderPlacedHandler_SendsConfirmationEmail()
{
    // Arrange
    var emailService = Substitute.For<IEmailService>();
    var handler = new OrderPlacedEventHandler(emailService);
    var domainEvent = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 100m);

    // Act
    await handler.HandleAsync(domainEvent);

    // Assert
    await emailService.Received(1).SendOrderConfirmationAsync(
        Arg.Is<Guid>(id => id == domainEvent.CustomerId),
        Arg.Is<Guid>(id => id == domainEvent.OrderId));
}
```

## Advanced Patterns (Future)

- **Transactional Outbox:** Persist events in database, dispatch later
- **Event Sourcing:** Store events as primary data source
- **Event Store:** Dedicated persistence for events
- **Event Replay:** Re-process historical events
- **Saga Pattern:** Coordinate distributed transactions
```

## Verification Checklist

- [ ] IDomainEvent marker interface
- [ ] IDomainEventHandler<TEvent> interface
- [ ] DomainEventBase with metadata
- [ ] IDomainEventDispatcher interface
- [ ] InMemoryDomainEventDispatcher implementation
- [ ] Example event handlers (logging, customer)
- [ ] ≥6 unit tests for dispatcher
- [ ] README with patterns and best practices

## Common Pitfalls

### ❌ Dispatching Events Before Commit
```csharp
// BAD - events dispatched before persistence
await _repository.AddAsync(customer);
await _eventDispatcher.DispatchAsync(customer.DomainEvents); // Too early!
await _unitOfWork.CommitAsync();

// GOOD - events dispatched after commit
await _repository.AddAsync(customer);
await _unitOfWork.CommitAsync(); // UnitOfWork dispatches events
```

### ❌ Modifying Aggregate in Event Handler
```csharp
// BAD - event handler modifies aggregate
public class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
{
    public async Task HandleAsync(OrderPlacedEvent e)
    {
        var order = await _repo.GetByIdAsync(e.OrderId);
        order.MarkAsProcessed(); // NO! Creates infinite loop
        await _repo.UpdateAsync(order);
    }
}

// GOOD - event handler updates different aggregate/system
public class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
{
    public async Task HandleAsync(OrderPlacedEvent e)
    {
        await _emailService.SendConfirmationAsync(e.CustomerId);
        await _analyticsService.TrackOrderAsync(e.OrderId);
    }
}
```

### ❌ Not Handling Failures Gracefully
```csharp
// BAD - exception breaks entire flow
public async Task HandleAsync(OrderPlacedEvent e)
{
    await _emailService.SendAsync(...); // Throws if email fails!
}

// GOOD - log and continue
public async Task HandleAsync(OrderPlacedEvent e)
{
    try
    {
        await _emailService.SendAsync(...);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send email for order {OrderId}", e.OrderId);
        // Consider: Add to retry queue
    }
}
```

## Next Steps

- **[07-repository-uow.md](07-repository-uow.md)** - Repository & Unit of Work (dispatches events)
- **[09-specifications.md](09-specifications.md)** - Specification pattern

## References

- [Domain Events Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)

---

**[← Back to Phase 2](../phase-2-domain-NEW.md)** | **[Next: Specifications →](09-specifications.md)**
