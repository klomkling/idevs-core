# 09: Specifications - Part 2

> **Navigation:** [Index](README.md) • [Part 1](09-specifications-pattern.md) • [Part 2](09-specifications-examples.md)

    public sealed class ByTenant : Specification<Customer>
    {
        private readonly Guid _tenantId;

        public ByTenant(Guid tenantId)
        {
            _tenantId = tenantId;
        }

        public override Expression<Func<Customer, bool>> ToExpression()
        {
            return customer => customer.TenantId == _tenantId;
        }
    }
}

```

**Design Rationale:**
- Static class with nested specification classes
- Business-meaningful names
- Immutable specifications (thread-safe)
- Composable via And/Or

### 8. Write Comprehensive Tests

**File:** `tests/Idevs.Tests/Domain/SpecificationTests.cs`

```csharp path=null start=null
using Idevs.Domain.Entities.Examples;
using Idevs.Domain.Specifications;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Domain;

public class SpecificationTests
{
    [Fact]
    public void CustomerByEmail_IsSatisfiedBy_MatchesEmail()
    {
        // Arrange
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        var spec = new CustomerSpecifications.ByEmail("john@example.com");

        // Act & Assert
        spec.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void CustomerByEmail_IsSatisfiedBy_CaseInsensitive()
    {
        // Arrange
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        var spec = new CustomerSpecifications.ByEmail("JOHN@EXAMPLE.COM");

        // Act & Assert
        spec.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void CustomerIsActive_IsSatisfiedBy_ActiveCustomer()
    {
        // Arrange
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        var spec = new CustomerSpecifications.IsActive();

        // Act & Assert
        spec.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void AndSpecification_CombinesTwoSpecs()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var customer = Customer.Create(
            tenantId,
            "John",
            "Doe",
            "john@example.com").Value;

        var tenantSpec = new CustomerSpecifications.ByTenant(tenantId);
        var activeSpec = new CustomerSpecifications.IsActive();

        var combined = tenantSpec.And(activeSpec);

        // Act & Assert
        combined.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void OrSpecification_MatchesEitherCondition()
    {
        // Arrange
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();

        var customer = Customer.Create(
            tenantId1,
            "John",
            "Doe",
            "john@example.com").Value;

        var spec1 = new CustomerSpecifications.ByTenant(tenantId1);
        var spec2 = new CustomerSpecifications.ByTenant(tenantId2);

        var combined = spec1.Or(spec2);

        // Act & Assert
        combined.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void NotSpecification_NegatesCondition()
    {
        // Arrange
        var customer = Customer.Create(
            Guid.NewGuid(),
            "John",
            "Doe",
            "john@example.com").Value;

        customer.Deactivate();

        var activeSpec = new CustomerSpecifications.IsActive();
        var notActiveSpec = activeSpec.Not();

        // Act & Assert
        notActiveSpec.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void ToExpression_ReturnsCorrectExpression()
    {
        // Arrange
        var spec = new CustomerSpecifications.IsActive();
        var customers = new List<Customer>
        {
            Customer.Create(Guid.NewGuid(), "Active", "User", "active@test.com").Value,
            Customer.Create(Guid.NewGuid(), "Inactive", "User", "inactive@test.com").Value
        };

        customers[1].Deactivate();

        // Act
        var expression = spec.ToExpression();
        var result = customers.AsQueryable().Where(expression).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].FirstName.ShouldBe("Active");
    }
}
```

### 9. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~SpecificationTests"
```

Expected: ✅ All 7+ tests passing

### 10. Document Usage Patterns

**File:** `src/Idevs/Domain/Specifications/README.md`

```markdown
# Specification Pattern

## Basic Usage

### Creating Specifications
```csharp
// Simple specification
var activeSpec = new CustomerSpecifications.IsActive();

// Specification with parameter
var emailSpec = new CustomerSpecifications.ByEmail("test@example.com");

// Combine specifications
var activeCustomersInTenant = 
    new CustomerSpecifications.ByTenant(tenantId)
        .And(new CustomerSpecifications.IsActive());
```

### Using in Repositories

```csharp
public async Task<IReadOnlyList<Customer>> GetActiveCustomersAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default)
{
    var spec = new CustomerSpecifications.ByTenant(tenantId)
        .And(new CustomerSpecifications.IsActive());

    return await _repository.FindAsync(spec, cancellationToken);
}
```

### In-Memory Evaluation

```csharp
var customer = await _repository.GetByIdAsync(customerId);
var spec = new CustomerSpecifications.IsActive();

if (spec.IsSatisfiedBy(customer))
{
    // Customer is active
}
```

## Composition

### AND Logic

```csharp
var spec = new CustomerSpecifications.ByTenant(tenantId)
    .And(new CustomerSpecifications.IsActive())
    .And(new CustomerSpecifications.ByNamePattern("john"));

var customers = await _repository.FindAsync(spec);
```

### OR Logic

```csharp
var spec = new CustomerSpecifications.ByEmail("test@example.com")
    .Or(new CustomerSpecifications.ByEmail("admin@example.com"));

var customers = await _repository.FindAsync(spec);
```

### NOT Logic

```csharp
var activeSpec = new CustomerSpecifications.IsActive();
var inactiveSpec = activeSpec.Not();

var inactiveCustomers = await _repository.FindAsync(inactiveSpec);
```

### Complex Combinations

```csharp
// (Active AND InTenant) OR HasVIPStatus
var spec = new CustomerSpecifications.IsActive()
    .And(new CustomerSpecifications.ByTenant(tenantId))
    .Or(new CustomerSpecifications.HasVIPStatus());
```

## Repository Extension Method

```csharp
public static class RepositoryExtensions
{
    public static Task<IReadOnlyList<TEntity>> FindAsync<TEntity, TKey>(
        this IRepository<TEntity, TKey> repository,
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        return repository.FindAsync(specification.ToExpression(), cancellationToken);
    }
}
```

## Best Practices

### ✅ DO

- Create one specification class per business rule
- Make specifications immutable (no state changes)
- Use meaningful names (IsActive, not ActiveFilter)
- Test specifications independently
- Compose specifications for complex queries

### ❌ DON'T

- Include repository or database access in specifications
- Make specifications mutable
- Use specifications for commands (only for queries)
- Include navigation property filtering (breaks EF Core translation)
- Create overly generic specifications

## Testing

```csharp
[Fact]
public void ActiveCustomerSpec_MatchesActiveCustomer()
{
    var customer = CreateActiveCustomer();
    var spec = new CustomerSpecifications.IsActive();

    spec.IsSatisfiedBy(customer).ShouldBeTrue();
}

[Fact]
public void CombinedSpec_MatchesAllConditions()
{
    var customer = CreateCustomerInTenant(tenantId);
    var spec = new CustomerSpecifications.ByTenant(tenantId)
        .And(new CustomerSpecifications.IsActive());

    spec.IsSatisfiedBy(customer).ShouldBeTrue();
}
```

```

## Verification Checklist

- [ ] ISpecification<T> interface
- [ ] Specification<T> abstract base class
- [ ] AndSpecification, OrSpecification, NotSpecification
- [ ] ReplaceExpressionVisitor helper
- [ ] Common specifications (Tenant, ActiveOnly)
- [ ] Customer-specific specifications
- [ ] ≥7 unit tests covering composition
- [ ] README with usage patterns

## Common Pitfalls

### ❌ Not Handling Case Sensitivity
```csharp
// BAD - case-sensitive comparison
public override Expression<Func<Customer, bool>> ToExpression()
{
    return customer => customer.Email.Value == _email;
}

// GOOD - case-insensitive
public ByEmail(string email)
{
    _email = email.ToLowerInvariant();
}

public override Expression<Func<Customer, bool>> ToExpression()
{
    return customer => customer.Email.Value == _email;
}
```

### ❌ Including Navigation Properties

```csharp
// BAD - EF Core may not translate
public override Expression<Func<Order, bool>> ToExpression()
{
    return order => order.OrderLines.Any(ol => ol.Quantity > 10);
}

// GOOD - use specific repository method
public Task<IReadOnlyList<Order>> GetOrdersWithLargeQuantitiesAsync()
{
    // Custom query in repository
}
```

### ❌ Mutable Specifications

```csharp
// BAD - mutable state
public class ByTenantSpec : Specification<Customer>
{
    public Guid TenantId { get; set; } // Mutable!
}

// GOOD - immutable
public class ByTenantSpec : Specification<Customer>
{
    private readonly Guid _tenantId;
    
    public ByTenantSpec(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}
```

## Next Steps

**Phase 2 Complete!** Continue to:

- **[Phase 3: Application Layer](../../phase-3-application/phase-3-application.md)** - Command/query handlers, validation
- **[Phase 5: Infrastructure](../../phase-5-infrastructure/phase-5-infrastructure.md)** - EF Core implementation

## References

- [Specification Pattern](https://en.wikipedia.org/wiki/Specification_pattern)
- [Eric Evans DDD](https://www.domainlanguage.com/ddd/)
- [Vladimir Khorikov on Specifications](https://enterprisecraftsmanship.com/posts/specification-pattern-c-implementation/)

---

**[← Back to Phase 2](../phase-2-domain-NEW.md)** | **[Phase 3: Application Layer →](../../phase-3-application/phase-3-application.md)**

---

**[← Part 1](09-specifications-pattern.md)** | **[Back to Phase 2](../phase-2-domain.md)**
