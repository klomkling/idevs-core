# 07: Repository & Unit of Work

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 07 of 09  
> **Estimated Time:** 3-4 hours

## Overview

Define repository and Unit of Work patterns that abstract data persistence, coordinate transactions, and dispatch domain events, enabling testable data access without coupling to specific ORMs.

## Prerequisites

- Phase 1 complete (solution structure)
- Guide 05 complete (aggregates)
- Guide 08 complete (domain events)
- Understanding of repository and UoW patterns
- Familiarity with async/await and transactions

## Objectives

### What You'll Build

1. **IRepository<TEntity, TKey>** - Generic repository interface
2. **IUnitOfWork** - Transaction coordination
3. **Repository base class** - Common CRUD operations
4. **Specialized repositories** - Type-specific methods
5. **Event dispatching** - Automatic after commit

### Why This Matters

- Abstracts persistence technology (swap EF Core for Dapper)
- Enables unit testing without database
- Coordinates transactions across aggregates
- Dispatches domain events after successful commit
- Provides consistent query interface

## Implementation Steps

### 1. Create Data Folder

```bash
mkdir -p src/Idevs/Data/{Repositories,UnitOfWork}
```

### 2. Define IRepository<TEntity, TKey> Interface

**File:** `src/Idevs/Data/Repositories/IRepository.cs`

```csharp path=null start=null
using Idevs.Contracts.Entities;
using Idevs.Results;
using System.Linq.Expressions;

namespace Idevs.Data.Repositories;

/// <summary>
/// Generic repository interface for aggregate roots
/// </summary>
public interface IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets entity by ID
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities (use with caution)
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching predicate
    /// </summary>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets single entity matching predicate
    /// </summary>
    Task<TEntity?> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if entity exists
    /// </summary>
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds new entity
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates existing entity
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Removes entity (hard delete or marks as deleted based on ISoftDeletable)
    /// </summary>
    void Remove(TEntity entity);

    /// <summary>
    /// Removes multiple entities
    /// </summary>
    void RemoveRange(IEnumerable<TEntity> entities);
}
```

**Design Rationale:**
- Generic interface for all aggregate roots
- Async methods for I/O operations
- Expression<Func<>> for type-safe queries
- Separate Add (async) vs Update (sync) - EF Core convention
- CancellationToken on all I/O operations

### 3. Define IUnitOfWork Interface

**File:** `src/Idevs/Data/UnitOfWork/IUnitOfWork.cs`

```csharp path=null start=null
namespace Idevs.Data.UnitOfWork;

/// <summary>
/// Coordinates transactions and dispatches domain events
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all changes and dispatches domain events
    /// </summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

**Design Rationale:**
- Single commit point for all repositories
- Explicit transaction management when needed
- Dispatches events after successful commit
- IDisposable for transaction cleanup

### 4. Create Read-Only Repository Interface

**File:** `src/Idevs/Data/Repositories/IReadOnlyRepository.cs`

```csharp path=null start=null
using Idevs.Contracts.Entities;
using Idevs.Results;
using System.Linq.Expressions;

namespace Idevs.Data.Repositories;

/// <summary>
/// Read-only repository for query operations
/// </summary>
public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}
```

**Design Rationale:**
- Separate interface for CQRS query side
- Includes pagination support
- Count operations for UIs
- No write operations

### 5. Create Specialized Repository Interfaces

**File:** `src/Idevs/Data/Repositories/ICustomerRepository.cs`

```csharp path=null start=null
using Idevs.Domain.Entities.Examples;

namespace Idevs.Data.Repositories;

/// <summary>
/// Repository for Customer aggregate
/// </summary>
public interface ICustomerRepository : IRepository<Customer, Guid>
{
    /// <summary>
    /// Gets customer by email
    /// </summary>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active customers for tenant
    /// </summary>
    Task<IReadOnlyList<Customer>> GetActiveCustomersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if email is already used
    /// </summary>
    Task<bool> IsEmailUniqueAsync(
        string email,
        Guid? excludeCustomerId = null,
        CancellationToken cancellationToken = default);
}
```

**Design Rationale:**
- Extends generic repository with domain-specific methods
- Business-meaningful method names
- Query methods return domain entities (not DTOs)
- Implementation deferred to Phase 5

### 6. Create In-Memory Repository (For Testing)

**File:** `src/Idevs/Data/Repositories/InMemoryRepository.cs`

```csharp path=null start=null
using Idevs.Contracts.Entities;
using System.Linq.Expressions;

namespace Idevs.Data.Repositories;

/// <summary>
/// In-memory repository for testing
/// </summary>
public class InMemoryRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    protected readonly List<TEntity> _entities = new();

    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = _entities.FirstOrDefault(e => e.Id.Equals(id));
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TEntity>>(_entities.ToList());
    }

    public Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        var results = _entities.Where(compiled).ToList();
        return Task.FromResult<IReadOnlyList<TEntity>>(results);
    }

    public Task<TEntity?> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        var entity = _entities.SingleOrDefault(compiled);
        return Task.FromResult(entity);
    }

    public Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var exists = _entities.Any(e => e.Id.Equals(id));
        return Task.FromResult(exists);
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(TEntity entity)
    {
        // In-memory: no-op (entity already in collection)
    }

    public void Remove(TEntity entity)
    {
        _entities.Remove(entity);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            _entities.Remove(entity);
        }
    }
}
```

**Design Rationale:**
- Enables unit testing without database
- In-memory List<T> as storage
- Expression.Compile() for LINQ queries
- Update is no-op (entities already tracked)

### 7. Write Comprehensive Tests

**File:** `tests/Idevs.Tests/Data/RepositoryTests.cs`

```csharp path=null start=null
using Idevs.Data.Repositories;
using Idevs.Domain.Entities;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Data;

public class RepositoryTests
{
    private class TestEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task InMemoryRepository_AddAsync_StoresEntity()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity, Guid>();
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        await repository.AddAsync(entity);

        // Assert
        var retrieved = await repository.GetByIdAsync(entity.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Test");
    }

    [Fact]
    public async Task InMemoryRepository_GetByIdAsync_ReturnsNullWhenNotFound()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity, Guid>();

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task InMemoryRepository_FindAsync_FiltersCorrectly()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity, Guid>();
        await repository.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Alice" });
        await repository.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Bob" });
        await repository.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Alice" });

        // Act
        var results = await repository.FindAsync(e => e.Name == "Alice");

        // Assert
        results.Count.ShouldBe(2);
        results.All(e => e.Name == "Alice").ShouldBeTrue();
    }

    [Fact]
    public async Task InMemoryRepository_Remove_DeletesEntity()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity, Guid>();
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        repository.Remove(entity);

        // Assert
        var retrieved = await repository.GetByIdAsync(entity.Id);
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task InMemoryRepository_ExistsAsync_ReturnsTrueWhenEntityExists()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity, Guid>();
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        var exists = await repository.ExistsAsync(entity.Id);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemoryRepository_AddRangeAsync_StoresMultipleEntities()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity, Guid>();
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity3" }
        };

        // Act
        await repository.AddRangeAsync(entities);

        // Assert
        var all = await repository.GetAllAsync();
        all.Count.ShouldBe(3);
    }
}
```

### 8. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~RepositoryTests"
```

Expected: ✅ All 6+ tests passing

### 9. Document Usage Patterns

**File:** `src/Idevs/Data/README.md`

```markdown
# Repository & Unit of Work

## Repository Pattern

### Basic Usage
```csharp
public class CreateCustomerCommandHandler
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> HandleAsync(CreateCustomerCommand command)
    {
        // Create aggregate
        var customer = Customer.Create(
            command.TenantId,
            command.FirstName,
            command.LastName,
            command.Email);

        if (customer.IsFailure)
            return customer.Error;

        // Add to repository
        await _repository.AddAsync(customer.Value);

        // Commit (also dispatches domain events)
        await _unitOfWork.CommitAsync();

        return customer.Value.Id;
    }
}
```

### Query Operations
```csharp
// Get by ID
var customer = await _repository.GetByIdAsync(customerId);

// Find by predicate
var activeCustomers = await _repository.FindAsync(c => c.IsActive);

// Check existence
var exists = await _repository.ExistsAsync(customerId);

// Custom repository method
var customer = await _customerRepository.GetByEmailAsync("test@example.com");
```

## Unit of Work Pattern

### Automatic Transaction
```csharp
// Repositories share same UnitOfWork
await _customerRepository.AddAsync(customer);
await _orderRepository.AddAsync(order);

// Single commit saves both
await _unitOfWork.CommitAsync(); // Also dispatches events
```

### Explicit Transaction
```csharp
try
{
    await _unitOfWork.BeginTransactionAsync();

    await _customerRepository.AddAsync(customer);
    await _orderRepository.AddAsync(order);

    await _unitOfWork.CommitTransactionAsync();
}
catch
{
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}
```

## Testing with In-Memory Repository

```csharp
[Fact]
public async Task CreateCustomer_AddsToRepository()
{
    // Arrange
    var repository = new InMemoryRepository<Customer, Guid>();
    var handler = new CreateCustomerCommandHandler(repository, ...);

    // Act
    var result = await handler.HandleAsync(command);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    var customer = await repository.GetByIdAsync(result.Value);
    customer.ShouldNotBeNull();
}
```

## Specialized Repository Methods

```csharp
public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken ct = default);
    Task<PagedResult<Order>> GetOrdersByCustomerAsync(Guid customerId, int page, int pageSize);
    Task<decimal> GetTotalRevenueAsync(Guid tenantId, DateRange dateRange);
}
```

## Best Practices

### ✅ DO
- One repository per aggregate root
- Return domain entities from repositories
- Use UnitOfWork for transaction coordination
- Implement specialized repository interfaces for complex queries
- Test with InMemoryRepository

### ❌ DON'T
- Create repositories for child entities (access through aggregate root)
- Return IQueryable (leaks persistence concerns)
- Mix data access code in command handlers (use repositories)
- Forget to call UnitOfWork.CommitAsync()
- Modify entities after getting them from repository (they're tracked)

## EF Core Implementation (Phase 5)

EF Core implementation will:
- Use DbContext as UnitOfWork
- Implement repositories with DbSet<T>
- Apply global query filters (tenant, soft-delete)
- Dispatch domain events in SaveChangesAsync
- Add audit interceptors (CreatedAt, UpdatedAt)
```

## Verification Checklist

- [ ] IRepository<TEntity, TKey> interface
- [ ] IUnitOfWork interface
- [ ] IReadOnlyRepository<TEntity, TKey> interface
- [ ] InMemoryRepository implementation
- [ ] ICustomerRepository example
- [ ] ≥6 unit tests for in-memory repository
- [ ] README with usage patterns

## Common Pitfalls

### ❌ Not Calling CommitAsync
```csharp
// BAD - changes not saved!
await _repository.AddAsync(customer);
return customer.Id; // Never committed!

// GOOD - explicitly commit
await _repository.AddAsync(customer);
await _unitOfWork.CommitAsync();
return customer.Id;
```

### ❌ Creating Repositories for Child Entities
```csharp
// BAD - breaks aggregate boundary
public interface IOrderLineRepository : IRepository<OrderLine, Guid> { }

// GOOD - access through aggregate
var order = await _orderRepository.GetByIdAsync(orderId);
var orderLine = order.OrderLines.First(ol => ol.Id == lineId);
```

### ❌ Returning IQueryable
```csharp
// BAD - leaks EF Core details
public IQueryable<Customer> Query() => _dbContext.Customers;

// GOOD - return materialized results
public Task<IReadOnlyList<Customer>> FindAsync(Expression<Func<Customer, bool>> predicate)
```

## Next Steps

- **[08-domain-events.md](08-domain-events.md)** - Domain event infrastructure
- **[09-specifications.md](09-specifications.md)** - Specification pattern

## References

- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Specifications →](09-specifications.md)**
