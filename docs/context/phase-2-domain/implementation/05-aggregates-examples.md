# 05b: Aggregates & Entities - Examples

> **Navigation:** [Index](README.md) • [Part 1: Base](05-aggregates-base.md) • [Part 2: Examples](05-aggregates-examples.md)

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 05b of 09  
> **Estimated Time:** 1-2 hours

## Overview

Build a complete Customer aggregate demonstrating factory methods, encapsulation, domain events, and comprehensive testing patterns.

## Prerequisites

- Part 1 complete (Entity, AggregateRoot, Guard classes)
- Understanding of aggregate patterns
- Value objects (Email) from Guide 02

## Implementation Steps

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

- [ ] Example Customer aggregate with factory method
- [ ] Domain events for Customer state changes
- [ ] Private setters on all aggregate properties
- [ ] ≥15 unit tests covering entities and aggregates
- [ ] Tests verify domain event raising
- [ ] Tests verify invariant enforcement

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

**[← Part 1: Base Classes](05-aggregates-base.md)** | **[Back to Phase 2](../phase-2-domain.md)** | **[Next: Repository & UoW →](07-repository-uow.md)**
