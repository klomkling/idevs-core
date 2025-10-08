# 07: Repository & Unit of Work - Part 2

> **Navigation:** [Index](README.md) • [Part 1](07-repository-pattern.md) • [Part 2](07-unit-of-work.md)

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

**[← Back to Phase 2](../phase-2-domain-NEW.md)** | **[Next: Specifications →](09-specifications.md)**

---

**[← Part 1](07-repository-pattern.md)** | **[Back to Phase 2](../phase-2-domain.md)**
