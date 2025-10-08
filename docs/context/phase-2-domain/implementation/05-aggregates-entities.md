# 05: Aggregates & Entities

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 05 of 09  
> **Estimated Time:** 3-4 hours

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
5. **Example aggregates** - Customer, Order for demonstration

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

### 5. Create Example Aggregate: Customer

**File:** `src/Idevs/Domain/Entities/Examples/Customer.cs`

```csharp path=null start=null
using Idevs.Contracts.Entities;
using Idevs.Domain.Events.Examples;
using Idevs.Domain.Guards;
using Idevs.Domain.ValueObjects;
using Idevs.Results;

namespace Idevs.Domain.Entities.Examples;

/// <summary>
/// Example aggregate root: Customer
/// </summary>
public sealed class Customer : AggregateRoot<Guid>, ITenantEntity<Guid>
{
    // Private setters enforce invariants
    private Customer() { }

    public Email Email { get; private set; } = null!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    // ITenantEntity implementation
    public Guid TenantId { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Factory method for creating a new customer
    /// </summary>
    public static Result<Customer> Create(
        Guid tenantId,
        string firstName,
        string lastName,
        string email)
    {
        // Validate
        if (tenantId == Guid.Empty)
            return new Error("Customer.InvalidTenant", "Tenant ID is required");

        Guard.AgainstNullOrEmpty(firstName, nameof(firstName));
        Guard.AgainstNullOrEmpty(lastName, nameof(lastName));

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
            return emailResult.Error;

        // Create
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = emailResult.Value,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Raise domain event
        customer.RaiseDomainEvent(new CustomerCreatedEvent(
            customer.Id,
            customer.Email.Value,
            customer.FullName));

        return customer;
    }

    /// <summary>
    /// Updates customer email
    /// </summary>
    public Result<Customer> UpdateEmail(string newEmail)
    {
        var emailResult = Email.Create(newEmail);
        if (emailResult.IsFailure)
            return emailResult.Error;

        if (Email.Equals(emailResult.Value))
            return this; // No change

        var oldEmail = Email.Value;
        Email = emailResult.Value;

        RaiseDomainEvent(new CustomerEmailChangedEvent(
            Id,
            oldEmail,
            Email.Value));

        return this;
    }

    /// <summary>
    /// Deactivates the customer
    /// </summary>
    public Result<Customer> Deactivate()
    {
        if (!IsActive)
            return new Error("Customer.AlreadyInactive", "Customer is already inactive");

        IsActive = false;

        RaiseDomainEvent(new CustomerDeactivatedEvent(Id, Email.Value));

        return this;
    }

    /// <summary>
    /// Reactivates the customer
    /// </summary>
    public Result<Customer> Activate()
    {
        if (IsActive)
            return new Error("Customer.AlreadyActive", "Customer is already active");

        IsActive = true;

        RaiseDomainEvent(new CustomerActivatedEvent(Id, Email.Value));

        return this;
    }
}
```

**Design Rationale:**
- Private constructor + factory method enforces valid construction
- Private setters prevent external mutation
- All state changes through public methods (encapsulation)
- Methods return `Result<Customer>` for fluent API
- Domain events raised on state changes
- Value objects (Email) for type safety

### 6. Create Domain Events for Customer

**File:** `src/Idevs/Domain/Events/Examples/CustomerEvents.cs`

```csharp path=null start=null
namespace Idevs.Domain.Events.Examples;

public sealed record CustomerCreatedEvent(
    Guid CustomerId,
    string Email,
    string FullName) : IDomainEvent;

public sealed record CustomerEmailChangedEvent(
    Guid CustomerId,
    string OldEmail,
    string NewEmail) : IDomainEvent;

public sealed record CustomerDeactivatedEvent(
    Guid CustomerId,
    string Email) : IDomainEvent;

public sealed record CustomerActivatedEvent(
    Guid CustomerId,
    string Email) : IDomainEvent;
```

### 7. Write Comprehensive Tests

**File:** `tests/Idevs.Tests/Domain/EntityTests.cs`

```csharp path=null start=null
using Idevs.Domain.Entities;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Domain;

public class EntityTests
{
    private class TestEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Entity_EqualityBasedOnId()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity { Id = id, Name = "Test1" };
        var entity2 = new TestEntity { Id = id, Name = "Test2" };

        entity1.ShouldBe(entity2); // Same ID
        (entity1 == entity2).ShouldBeTrue();
    }

    [Fact]
    public void Entity_DifferentIds_NotEqual()
    {
        var entity1 = new TestEntity { Id = Guid.NewGuid() };
        var entity2 = new TestEntity { Id = Guid.NewGuid() };

        entity1.ShouldNotBe(entity2);
        (entity1 != entity2).ShouldBeTrue();
    }

    [Fact]
    public void Entity_TransientEntities_OnlyEqualIfSameReference()
    {
        var entity1 = new TestEntity(); // No ID
        var entity2 = new TestEntity(); // No ID

        entity1.ShouldNotBe(entity2);
        entity1.ShouldBe(entity1); // Same reference
    }

    [Fact]
    public void Entity_IsTransient_ReturnsTrueForDefaultId()
    {
        var entity = new TestEntity();

        entity.IsTransient().ShouldBeTrue();

        entity.Id = Guid.NewGuid();
        entity.IsTransient().ShouldBeFalse();
    }

    [Fact]
    public void Entity_HashCode_ConsistentForSameId()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity { Id = id };

        var hash1 = entity.GetHashCode();
        var hash2 = entity.GetHashCode();

        hash1.ShouldBe(hash2);
    }
}
```

**File:** `tests/Idevs.Tests/Domain/AggregateRootTests.cs`

```csharp path=null start=null
using Idevs.Domain.Entities;
using Idevs.Domain.Events;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Domain;

public class AggregateRootTests
{
    private record TestEvent(string Data) : IDomainEvent;

    private class TestAggregate : AggregateRoot<Guid>
    {
        public void DoSomething()
        {
            RaiseDomainEvent(new TestEvent("Test"));
        }
    }

    [Fact]
    public void AggregateRoot_InitiallyHasNoEvents()
    {
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AggregateRoot_RaiseDomainEvent_AddsEvent()
    {
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };

        aggregate.DoSomething();

        aggregate.DomainEvents.Count.ShouldBe(1);
        aggregate.DomainEvents.First().ShouldBeOfType<TestEvent>();
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };
        aggregate.DoSomething();

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AggregateRoot_DomainEvents_IsReadOnly()
    {
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };

        aggregate.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }
}
```

**File:** `tests/Idevs.Tests/Domain/CustomerTests.cs`

```csharp path=null start=null
using Idevs.Domain.Entities.Examples;
using Idevs.Domain.Events.Examples;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Domain;

public class CustomerTests
{
    [Fact]
    public void Customer_Create_ReturnsValidCustomer()
    {
        var tenantId = Guid.NewGuid();
        var result = Customer.Create(
            tenantId,
            "John",
            "Doe",
            "john@example.com");

        result.IsSuccess.ShouldBeTrue();
        result.Value.FirstName.ShouldBe("John");
        result.Value.LastName.ShouldBe("Doe");
        result.Value.Email.Value.ShouldBe("john@example.com");
        result.Value.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Customer_Create_RaisesCustomerCreatedEvent()
    {
        var result = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com");

        result.Value.DomainEvents.Count.ShouldBe(1);
        result.Value.DomainEvents.First().ShouldBeOfType<CustomerCreatedEvent>();
    }

    [Fact]
    public void Customer_Create_ValidatesEmail()
    {
        var result = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "invalid-email");

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Customer_UpdateEmail_ChangesEmail()
    {
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        customer.ClearDomainEvents(); // Clear creation event

        var result = customer.UpdateEmail("newemail@example.com");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.Value.ShouldBe("newemail@example.com");
        result.Value.DomainEvents.Count.ShouldBe(1);
        result.Value.DomainEvents.First().ShouldBeOfType<CustomerEmailChangedEvent>();
    }

    [Fact]
    public void Customer_UpdateEmail_SameEmail_NoChange()
    {
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        customer.ClearDomainEvents();

        var result = customer.UpdateEmail("john@example.com");

        result.IsSuccess.ShouldBeTrue();
        result.Value.DomainEvents.ShouldBeEmpty(); // No change
    }

    [Fact]
    public void Customer_Deactivate_SetsInactive()
    {
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        customer.ClearDomainEvents();

        var result = customer.Deactivate();

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeFalse();
        result.Value.DomainEvents.First().ShouldBeOfType<CustomerDeactivatedEvent>();
    }

    [Fact]
    public void Customer_Deactivate_AlreadyInactive_Fails()
    {
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        customer.Deactivate();

        var result = customer.Deactivate();

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("AlreadyInactive");
    }

    [Fact]
    public void Customer_Activate_SetsActive()
    {
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        customer.Deactivate();
        customer.ClearDomainEvents();

        var result = customer.Activate();

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeTrue();
        result.Value.DomainEvents.First().ShouldBeOfType<CustomerActivatedEvent>();
    }
}
```

### 8. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~Domain"
```

Expected: ✅ All 15+ tests passing

## Usage Examples

### Creating and Modifying Aggregates

```csharp path=null start=null
// Create customer
var customerResult = Customer.Create(
    tenantId: tenantId,
    firstName: "John",
    lastName: "Doe",
    email: "john@example.com");

if (customerResult.IsFailure)
    return customerResult.Error;

var customer = customerResult.Value;

// Modify customer
var emailResult = customer.UpdateEmail("newemail@example.com");
if (emailResult.IsFailure)
    return emailResult.Error;

// Save with events
await _repository.AddAsync(customer);
await _unitOfWork.CommitAsync(); // Dispatches domain events
```

### In Command Handler

```csharp path=null start=null
public class CreateCustomerCommandHandler 
    : ICommandHandler<CreateCustomerCommand, Guid>
{
    private readonly ICustomerRepository _repository;
    private readonly ITenantContext _tenantContext;

    public async Task<Result<Guid>> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        // Create aggregate
        var customerResult = Customer.Create(
            _tenantContext.TenantId!.Value,
            command.FirstName,
            command.LastName,
            command.Email);

        if (customerResult.IsFailure)
            return customerResult.Error;

        // Persist
        await _repository.AddAsync(customerResult.Value, cancellationToken);

        return customerResult.Value.Id;
    }
}
```

## Verification Checklist

- [ ] Entity<TKey> base class with identity equality
- [ ] AggregateRoot<TKey> with domain event management
- [ ] Guard clauses for invariant validation
- [ ] Example Customer aggregate with factory method
- [ ] Domain events for Customer state changes
- [ ] Private setters on all aggregate properties
- [ ] ≥15 unit tests covering entities and aggregates
- [ ] Tests verify domain event raising

## Common Pitfalls

### ❌ Public Setters on Aggregates
```csharp
// BAD - allows bypassing invariants
public class Customer : AggregateRoot<Guid>
{
    public string Email { get; set; } // Anyone can set invalid email!
}

// GOOD - encapsulated with validation
public class Customer : AggregateRoot<Guid>
{
    public Email Email { get; private set; }
    
    public Result<Customer> UpdateEmail(string newEmail)
    {
        var result = Email.Create(newEmail);
        if (result.IsFailure) return result.Error;
        
        Email = result.Value;
        return this;
    }
}
```

### ❌ Not Using Factory Methods
```csharp
// BAD - allows invalid construction
var customer = new Customer 
{ 
    Id = Guid.NewGuid(),
    Email = null! // Oops!
};

// GOOD - validated construction
var result = Customer.Create(tenantId, firstName, lastName, email);
if (result.IsFailure) return result.Error;
```

### ❌ Forgetting to Raise Domain Events
```csharp
// BAD - no event for important state change
public void Deactivate()
{
    IsActive = false; // Other systems won't know!
}

// GOOD - raises event
public Result<Customer> Deactivate()
{
    IsActive = false;
    RaiseDomainEvent(new CustomerDeactivatedEvent(Id, Email));
    return this;
}
```

## Next Steps

- **[07-repository-uow.md](07-repository-uow.md)** - Repository & Unit of Work
- **[08-domain-events.md](08-domain-events.md)** - Domain event infrastructure

## References

- [DDD Aggregates](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [Domain Events](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Repository & UoW →](07-repository-uow.md)**
