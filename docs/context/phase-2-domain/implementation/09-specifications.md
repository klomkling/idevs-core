# 09: Specification Pattern

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 09 of 09  
> **Estimated Time:** 2-3 hours

## Overview

Implement the Specification pattern for composable, reusable query logic that encapsulates business rules, enabling clean separation between domain logic and data access while maintaining type safety.

## Prerequisites

- Phase 1 complete (solution structure)
- Guide 07 complete (repositories)
- Understanding of specification pattern and expression trees
- Familiarity with LINQ and lambda expressions

## Objectives

### What You'll Build

1. **ISpecification<T>** - Base specification interface
2. **Specification<T>** - Abstract base class
3. **Logical operators** - And, Or, Not combinators
4. **Common specifications** - Paging, sorting, filtering
5. **Example specifications** - Customer business rules

### Why This Matters

- Encapsulates complex query logic in reusable objects
- Enables composition of business rules (AND, OR, NOT)
- Keeps repositories clean and focused
- Supports unit testing of query logic
- Prevents duplication of filtering conditions

## Implementation Steps

### 1. Create Specifications Folder

```bash
mkdir -p src/Idevs/Domain/Specifications
```

### 2. Define ISpecification<T> Interface

**File:** `src/Idevs/Domain/Specifications/ISpecification.cs`

```csharp path=null start=null
using System.Linq.Expressions;

namespace Idevs.Domain.Specifications;

/// <summary>
/// Specification pattern interface
/// </summary>
public interface ISpecification<T>
{
    /// <summary>
    /// Converts specification to expression tree
    /// </summary>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Evaluates specification against entity
    /// </summary>
    bool IsSatisfiedBy(T entity);
}
```

**Design Rationale:**
- Expression<Func<T, bool>> for use with IQueryable (EF Core)
- IsSatisfiedBy for in-memory evaluation
- Generic T for any entity type

### 3. Create Specification Base Class

**File:** `src/Idevs/Domain/Specifications/Specification.cs`

```csharp path=null start=null
using System.Linq.Expressions;

namespace Idevs.Domain.Specifications;

/// <summary>
/// Abstract base class for specifications
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    /// <summary>
    /// Combines specifications with AND logic
    /// </summary>
    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Combines specifications with OR logic
    /// </summary>
    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Negates the specification
    /// </summary>
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }

    /// <summary>
    /// Implicit conversion to expression
    /// </summary>
    public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
    {
        return specification.ToExpression();
    }
}
```

**Design Rationale:**
- Abstract class provides composition methods
- IsSatisfiedBy compiles expression for in-memory use
- Fluent API (And, Or, Not) for readability
- Implicit operator for seamless repository integration

### 4. Create Logical Combinator Specifications

**File:** `src/Idevs/Domain/Specifications/AndSpecification.cs`

```csharp path=null start=null
using System.Linq.Expressions;

namespace Idevs.Domain.Specifications;

/// <summary>
/// Combines two specifications with AND logic
/// </summary>
internal sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        // Combine parameters
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(leftExpression.Parameters[0], parameter);
        var left = leftVisitor.Visit(leftExpression.Body);

        var rightVisitor = new ReplaceExpressionVisitor(rightExpression.Parameters[0], parameter);
        var right = rightVisitor.Visit(rightExpression.Body);

        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(left!, right!),
            parameter);
    }
}
```

**File:** `src/Idevs/Domain/Specifications/OrSpecification.cs`

```csharp path=null start=null
using System.Linq.Expressions;

namespace Idevs.Domain.Specifications;

/// <summary>
/// Combines two specifications with OR logic
/// </summary>
internal sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(leftExpression.Parameters[0], parameter);
        var left = leftVisitor.Visit(leftExpression.Body);

        var rightVisitor = new ReplaceExpressionVisitor(rightExpression.Parameters[0], parameter);
        var right = rightVisitor.Visit(rightExpression.Body);

        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(left!, right!),
            parameter);
    }
}
```

**File:** `src/Idevs/Domain/Specifications/NotSpecification.cs`

```csharp path=null start=null
using System.Linq.Expressions;

namespace Idevs.Domain.Specifications;

/// <summary>
/// Negates a specification
/// </summary>
internal sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expression = _specification.ToExpression();
        var negated = Expression.Not(expression.Body);

        return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
    }
}
```

### 5. Create Expression Visitor Helper

**File:** `src/Idevs/Domain/Specifications/ReplaceExpressionVisitor.cs`

```csharp path=null start=null
using System.Linq.Expressions;

namespace Idevs.Domain.Specifications;

/// <summary>
/// Replaces expression parameters for composition
/// </summary>
internal sealed class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _oldValue;
    private readonly Expression _newValue;

    public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public override Expression? Visit(Expression? node)
    {
        return node == _oldValue ? _newValue : base.Visit(node);
    }
}
```

**Design Rationale:**
- Replaces parameter instances in combined expressions
- Required for And/Or to work with expression trees
- Ensures EF Core can translate to SQL

### 6. Create Common Specifications

**File:** `src/Idevs/Domain/Specifications/Common/TenantSpecification.cs`

```csharp path=null start=null
using System.Linq.Expressions;
using Idevs.Contracts.Entities;

namespace Idevs.Domain.Specifications.Common;

/// <summary>
/// Filters entities by tenant ID
/// </summary>
public sealed class TenantSpecification<T> : Specification<T>
    where T : ITenant
{
    private readonly Guid _tenantId;

    public TenantSpecification(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        return entity => entity.TenantId == _tenantId;
    }
}
```

**File:** `src/Idevs/Domain/Specifications/Common/ActiveOnlySpecification.cs`

```csharp path=null start=null
using System.Linq.Expressions;
using Idevs.Contracts.Entities;

namespace Idevs.Domain.Specifications.Common;

/// <summary>
/// Filters out soft-deleted entities
/// </summary>
public sealed class ActiveOnlySpecification<T> : Specification<T>
    where T : ISoftDeletable
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        return entity => !entity.IsDeleted;
    }
}
```

### 7. Create Customer-Specific Specifications

**File:** `src/Idevs/Domain/Specifications/CustomerSpecifications.cs`

```csharp path=null start=null
using System.Linq.Expressions;
using Idevs.Domain.Entities.Examples;

namespace Idevs.Domain.Specifications;

public static class CustomerSpecifications
{
    /// <summary>
    /// Customers with matching email
    /// </summary>
    public sealed class ByEmail : Specification<Customer>
    {
        private readonly string _email;

        public ByEmail(string email)
        {
            _email = email.ToLowerInvariant();
        }

        public override Expression<Func<Customer, bool>> ToExpression()
        {
            return customer => customer.Email.Value == _email;
        }
    }

    /// <summary>
    /// Active customers only
    /// </summary>
    public sealed class IsActive : Specification<Customer>
    {
        public override Expression<Func<Customer, bool>> ToExpression()
        {
            return customer => customer.IsActive;
        }
    }

    /// <summary>
    /// Customers matching name pattern
    /// </summary>
    public sealed class ByNamePattern : Specification<Customer>
    {
        private readonly string _pattern;

        public ByNamePattern(string pattern)
        {
            _pattern = pattern.ToLowerInvariant();
        }

        public override Expression<Func<Customer, bool>> ToExpression()
        {
            return customer =>
                customer.FirstName.ToLower().Contains(_pattern) ||
                customer.LastName.ToLower().Contains(_pattern);
        }
    }

    /// <summary>
    /// Customers for specific tenant
    /// </summary>
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

**[← Back to Phase 2](../phase-2-domain.md)** | **[Phase 3: Application Layer →](../../phase-3-application/phase-3-application.md)**
