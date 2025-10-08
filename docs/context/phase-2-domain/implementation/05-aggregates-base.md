# 05a: Aggregates & Entities - Base Classes

> **Navigation:** [Index](README.md) • [Part 1: Base](05-aggregates-base.md) • [Part 2: Examples](05-aggregates-examples.md)

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 05a of 09  
> **Estimated Time:** 2 hours

## Overview

Implement Entity and AggregateRoot base classes that enforce domain invariants, manage domain events, and provide consistent identity and equality semantics for domain models.

## Prerequisites

- Phase 1 complete (solution structure)
- Guide 01 complete (entity interfaces)
- Guide 08 complete (domain events) - or implement in parallel
- Understanding of DDD aggregates and consistency boundaries
- Familiarity with domain-driven design patterns

## Objectives

### What You'll Build

1. **Entity<TKey>** - Base entity class with identity and equality
2. **AggregateRoot<TKey>** - Root entity managing consistency boundaries
3. **Domain event collection** - Event raising and retrieval
4. **Invariant enforcement** - Guard clauses and validation patterns

### Why This Matters

- Enforces aggregate boundaries and consistency rules
- Centralizes domain logic in one place (no anemic models)
- Manages domain events at aggregate level
- Provides identity-based equality (not reference equality)
- Prevents invalid state through encapsulation

## Implementation Steps

### 1. Create Domain Folder

```bash
mkdir -p src/Idevs/Domain/Entities
```

### 2. Define Entity<TKey> Base Class

**File:** `src/Idevs/Domain/Entities/Entity.cs`

```csharp path=null start=null
using Idevs.Contracts.Entities;

namespace Idevs.Domain.Entities;

/// <summary>
/// Base class for all entities with identity-based equality
/// </summary>
public abstract class Entity<TKey> : IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    private int? _cachedHashCode;

    public virtual TKey Id { get; set; } = default!;

    /// <summary>
    /// Checks if entity is transient (not yet persisted)
    /// </summary>
    public bool IsTransient() => EqualityComparer<TKey>.Default.Equals(Id, default);

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetRealType() != other.GetRealType())
            return false;

        // Transient entities are only equal if same reference
        if (IsTransient() || other.IsTransient())
            return false;

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        // Cache hash code for entities (immutable ID)
        if (_cachedHashCode.HasValue)
            return _cachedHashCode.Value;

        if (IsTransient())
            return base.GetHashCode();

        _cachedHashCode = HashCode.Combine(GetRealType(), Id);
        return _cachedHashCode.Value;
    }

    /// <summary>
    /// Gets the unproxied entity type (for EF Core proxies)
    /// </summary>
    private Type GetRealType()
    {
        var type = GetType();

        // Handle EF Core proxies
        if (type.Name.Contains("Proxy", StringComparison.OrdinalIgnoreCase))
            return type.BaseType!;

        return type;
    }

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
    {
        return !(left == right);
    }
}
```

**Design Rationale:**

- Identity-based equality (not reference equality)
- Transient entities (no ID yet) only equal if same reference
- Hash code caching for performance (IDs are immutable)
- Handles EF Core proxies correctly
- Generic constraint ensures IDs are equatable

### 3. Define AggregateRoot<TKey> Base Class

**File:** `src/Idevs/Domain/Entities/AggregateRoot.cs`

```csharp path=null start=null
using Idevs.Domain.Events;

namespace Idevs.Domain.Entities;

/// <summary>
/// Base class for aggregate roots that manage domain events
/// </summary>
public abstract class AggregateRoot<TKey> : Entity<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the domain events raised by this aggregate
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to be dispatched after persistence
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events (called after dispatch)
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

**Design Rationale:**

- Only aggregate roots raise domain events (not child entities)
- Protected `RaiseDomainEvent` for use by aggregate methods
- Public `ClearDomainEvents` called by infrastructure after dispatch
- Events are read-only outside the aggregate

### 4. Create Guard Clause Helpers

**File:** `src/Idevs/Domain/Guards/Guard.cs`

```csharp path=null start=null
using Idevs.Results;

namespace Idevs.Domain.Guards;

/// <summary>
/// Guard clauses for domain invariant validation
/// </summary>
public static class Guard
{
    public static Result<T> Against<T>(
        bool condition,
        Error error)
    {
        return condition
            ? Result<T>.Failure(error)
            : Result<T>.Success(default!);
    }

    public static void AgainstNull<T>(
        T? value,
        string parameterName)
        where T : class
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);
    }

    public static void AgainstNullOrEmpty(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                $"{parameterName} cannot be null or empty",
                parameterName);
    }

    public static void AgainstNegativeOrZero(
        int value,
        string parameterName)
    {
        if (value <= 0)
            throw new ArgumentException(
                $"{parameterName} must be positive",
                parameterName);
    }

    public static void AgainstNegative(
        decimal value,
        string parameterName)
    {
        if (value < 0)
            throw new ArgumentException(
                $"{parameterName} cannot be negative",
                parameterName);
    }

    public static void AgainstOutOfRange(
        int value,
        int min,
        int max,
        string parameterName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"{parameterName} must be between {min} and {max}");
    }
}
```

**Design Rationale:**

- Centralized invariant validation
- Throws exceptions for constructor validation (fail-fast)
- Returns `Result<T>` for business rule validation
- Descriptive error messages

## Verification Checklist

- [ ] Entity<TKey> base class with identity equality
- [ ] AggregateRoot<TKey> with domain event management
- [ ] Guard clauses for invariant validation
- [ ] Entity equality tests pass
- [ ] AggregateRoot event management tests pass

## Next Steps

Continue to **[Part 2: Examples](05-aggregates-examples.md)** for Customer aggregate implementation and comprehensive tests.

## References

- [DDD Aggregates](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [Domain Events](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 Examples →](05-aggregates-examples.md)**
