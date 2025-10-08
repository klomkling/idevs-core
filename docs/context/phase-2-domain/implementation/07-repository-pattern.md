# 07: Repository & Unit of Work - Part 1

> **Navigation:** [Index](README.md) • [Part 1](07-repository-pattern.md) • [Part 2](07-unit-of-work.md)


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

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 →](07-unit-of-work.md)**
