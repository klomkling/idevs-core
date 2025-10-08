# 08: Domain Events - Part 2

> **Navigation:** [Index](README.md) • [Part 1](08-domain-events-contracts.md) • [Part 2](08-domain-events-impl.md)

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

---

**[← Part 1](08-domain-events-contracts.md)** | **[Back to Phase 2](../phase-2-domain.md)**
