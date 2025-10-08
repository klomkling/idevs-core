# 01: Core Entity Interfaces

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 01 of 09  
> **Estimated Time:** 2-3 hours

## Overview

Define base entity interfaces that establish contracts for identity, auditing, soft deletion, and multi-tenancy across all domain entities in the Idevs framework.

## Prerequisites

- Phase 1 complete (solution structure ready)
- Understanding of ADR-0002 (Audit Trails) and ADR-0003 (Soft Delete)
- Familiarity with generic constraints in C#

## Objectives

### What You'll Build

1. **IEntity<TKey>** - Base identity contract
2. **IAuditable** - Created/updated timestamps and actors
3. **ISoftDeletable** - Soft delete support
4. **ITenant** - Multi-tenant isolation
5. **IEntity (non-generic)** - Marker interface for entity discovery

### Why This Matters

- Ensures consistent entity structure across all aggregates
- Enables framework-level features (auditing, soft-delete, tenant filters)
- Supports EF Core conventions and query filters
- Prevents accidental hard deletes in production

## Implementation Steps

### 1. Create Contracts Folder Structure

```bash
mkdir -p src/Idevs/Contracts/Entities
```

### 2. Define IEntity<TKey> Interface

**File:** `src/Idevs/Contracts/Entities/IEntity.cs`

```csharp path=null start=null
namespace Idevs.Contracts.Entities;

/// <summary>
/// Marker interface for all entities
/// </summary>
public interface IEntity
{
}

/// <summary>
/// Base entity contract with strongly-typed identifier
/// </summary>
/// <typeparam name="TKey">Type of the entity's unique identifier</typeparam>
public interface IEntity<TKey> : IEntity where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    TKey Id { get; set; }
}
```

**Design Rationale:**
- Non-generic `IEntity` enables runtime type discovery without knowing TKey
- Generic constraint `IEquatable<TKey>` ensures IDs support equality comparison
- Supports Guid, long, int, or custom strongly-typed IDs

### 3. Define IAuditable Interface

**File:** `src/Idevs/Contracts/Entities/IAuditable.cs`

```csharp path=null start=null
namespace Idevs.Contracts.Entities;

/// <summary>
/// Entities implementing this interface track creation and modification metadata
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// UTC timestamp when entity was created
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Identifier of the user who created the entity
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// UTC timestamp of the last modification
    /// </summary>
    DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Identifier of the user who last modified the entity
    /// </summary>
    string? UpdatedBy { get; set; }
}
```

**Design Rationale:**
- `DateTimeOffset` for timezone-aware timestamps
- `CreatedAt` required, `UpdatedAt` nullable (not set on creation)
- `string?` for user IDs (supports various identity providers)
- Auto-populated by EF interceptors (implemented in Phase 5)

### 4. Define ISoftDeletable Interface

**File:** `src/Idevs/Contracts/Entities/ISoftDeletable.cs`

```csharp path=null start=null
namespace Idevs.Contracts.Entities;

/// <summary>
/// Entities implementing this interface support soft deletion
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft-deleted
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when entity was soft-deleted
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Identifier of the user who deleted the entity
    /// </summary>
    string? DeletedBy { get; set; }
}
```

**Design Rationale:**
- Boolean flag for quick filtering (indexed in DB)
- Timestamp and actor for audit trail
- EF Core global query filter will exclude `IsDeleted == true` by default
- Supports GDPR-compliant hard deletes later

### 5. Define ITenant Interface

**File:** `src/Idevs/Contracts/Entities/ITenant.cs`

```csharp path=null start=null
namespace Idevs.Contracts.Entities;

/// <summary>
/// Entities implementing this interface are scoped to a specific tenant
/// </summary>
public interface ITenant
{
    /// <summary>
    /// Unique identifier of the tenant this entity belongs to
    /// </summary>
    Guid TenantId { get; set; }
}
```

**Design Rationale:**
- `Guid` for globally unique tenant IDs
- Required property (non-nullable) — every tenant entity MUST have a tenant
- EF Core will enforce tenant filter at query level (Phase 5)

### 6. Create Convenience Combinations

**File:** `src/Idevs/Contracts/Entities/ITenantEntity.cs`

```csharp path=null start=null
namespace Idevs.Contracts.Entities;

/// <summary>
/// Convenience interface for common entity patterns
/// </summary>
public interface ITenantEntity<TKey> : IEntity<TKey>, ITenant, IAuditable, ISoftDeletable
    where TKey : IEquatable<TKey>
{
}
```

**Design Rationale:**
- Most SaaS entities need all four concerns
- Reduces boilerplate in entity declarations
- Example: `public class Customer : Entity<Guid>, ITenantEntity<Guid> { }`

### 7. Write Unit Tests

**File:** `tests/Idevs.Tests/Contracts/EntityInterfacesTests.cs`

```csharp path=null start=null
using Idevs.Contracts.Entities;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Contracts;

public class EntityInterfacesTests
{
    private class TestEntity : IEntity<Guid>, IAuditable, ISoftDeletable, ITenant
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public Guid TenantId { get; set; }
    }

    [Fact]
    public void IEntity_ImplementsMarkerInterface()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        
        (entity is IEntity).ShouldBeTrue();
        entity.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void IAuditable_TracksCreationMetadata()
    {
        var entity = new TestEntity
        {
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "user-123"
        };

        entity.CreatedAt.ShouldNotBe(default);
        entity.CreatedBy.ShouldBe("user-123");
        entity.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void ISoftDeletable_SupportsLogicalDeletion()
    {
        var entity = new TestEntity
        {
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
            DeletedBy = "admin-456"
        };

        entity.IsDeleted.ShouldBeTrue();
        entity.DeletedAt.ShouldNotBeNull();
        entity.DeletedBy.ShouldBe("admin-456");
    }

    [Fact]
    public void ITenant_EnforcesTenantIsolation()
    {
        var tenantId = Guid.NewGuid();
        var entity = new TestEntity { TenantId = tenantId };

        entity.TenantId.ShouldBe(tenantId);
    }
}
```

### 8. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~EntityInterfacesTests"
```

Expected output: ✅ All 4 tests passing

### 9. Document Usage Patterns

**File:** `src/Idevs/Contracts/Entities/README.md`

```markdown
# Entity Contracts

## Usage Examples

### Simple Entity (No Tenancy)
```csharp
public class Category : IEntity<int>, IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // IAuditable
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### Multi-Tenant Entity
```csharp
public class Product : IEntity<Guid>, ITenantEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    // ITenantEntity includes ITenant, IAuditable, ISoftDeletable
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

## Integration with EF Core

These interfaces enable:
- **Audit Interceptors:** Auto-populate CreatedAt/UpdatedAt on SaveChanges
- **Global Query Filters:** Exclude soft-deleted entities automatically
- **Tenant Filters:** Scope queries to current tenant context
- **Convention-Based Config:** Detect interfaces and apply indexes

See Phase 5 (Infrastructure) for implementation details.
```

## Verification Checklist

- [ ] All 5 interfaces defined in `src/Idevs/Contracts/Entities/`
- [ ] `ITenantEntity<TKey>` convenience interface created
- [ ] Unit tests passing (4 tests minimum)
- [ ] README.md with usage examples
- [ ] Interfaces use C# nullable reference types correctly
- [ ] DateTimeOffset used (not DateTime)
- [ ] Generic constraint `IEquatable<TKey>` applied

## Common Pitfalls

### ❌ Using DateTime Instead of DateTimeOffset
```csharp
// BAD - loses timezone information
public DateTime CreatedAt { get; set; }

// GOOD - preserves UTC offset
public DateTimeOffset CreatedAt { get; set; }
```

### ❌ Making TenantId Nullable
```csharp
// BAD - allows orphaned entities
public Guid? TenantId { get; set; }

// GOOD - enforces tenant ownership
public Guid TenantId { get; set; }
```

### ❌ Forgetting Generic Constraint
```csharp
// BAD - IDs can't be compared
public interface IEntity<TKey> { TKey Id { get; set; } }

// GOOD - ensures equality checks work
public interface IEntity<TKey> : IEntity where TKey : IEquatable<TKey>
```

## Next Steps

Continue to:
- **[02-value-objects.md](02-value-objects.md)** - Value object base classes
- **[03-result-patterns.md](03-result-patterns.md)** - Result<T> for error handling

## References

- [ADR-0002: Audit Trails](../../adrs/adr-0002-audit-trails.md)
- [ADR-0003: Soft Delete Strategy](../../adrs/adr-0003-soft-delete.md)
- [EF Core Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)

---

**[← Back to Phase 2 Overview](../phase-2-domain-NEW.md)** | **[Next Guide: Value Objects →](02-value-objects.md)**
