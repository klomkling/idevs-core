# 08: Domain Events - Part 1

> **Navigation:** [Index](README.md) • [Part 1](08-domain-events-contracts.md) • [Part 2](08-domain-events-impl.md)


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

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 →](08-domain-events-impl.md)**
