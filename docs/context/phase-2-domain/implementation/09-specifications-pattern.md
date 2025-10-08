# 09: Specifications - Part 1

> **Navigation:** [Index](README.md) • [Part 1](09-specifications-pattern.md) • [Part 2](09-specifications-examples.md)


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

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 →](09-specifications-examples.md)**
