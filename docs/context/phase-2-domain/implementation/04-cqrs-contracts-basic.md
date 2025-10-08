# 04: CQRS Contracts - Part 1

> **Navigation:** [Index](README.md) • [Part 1](04-cqrs-contracts-basic.md) • [Part 2](04-cqrs-contracts-advanced.md)


> **Phase:** 2 - Domain & Contracts  
> **Guide:** 04 of 09  
> **Estimated Time:** 2-3 hours

## Overview

Define Command Query Responsibility Segregation (CQRS) contracts that separate read and write operations, enabling decorators for validation, logging, caching, and transaction management.

## Prerequisites

- Phase 1 complete (solution structure)
- Guide 03 complete (Result patterns)
- Understanding of CQRS and mediator patterns
- Familiarity with C# generics and async/await

## Objectives

### What You'll Build

1. **ICommand / ICommand<TResponse>** - Command interfaces
2. **IQuery<TResponse>** - Query interface
3. **ICommandHandler / IQueryHandler** - Handler interfaces
4. **Unit type** - Void replacement for commands without return values
5. **Handler registration** - Discovery patterns for DI

### Why This Matters

- Clear separation between reads (queries) and writes (commands)
- Enables decorator pattern for cross-cutting concerns
- Simplifies testing (mock handlers, not services)
- Supports eventual consistency and CQRS at scale
- Provides single entry point for request processing

## Implementation Steps

### 1. Create CQRS Folder

```bash
mkdir -p src/Idevs/CQRS/{Commands,Queries}
```

### 2. Define Unit Type

**File:** `src/Idevs/CQRS/Unit.cs`

```csharp path=null start=null
namespace Idevs.CQRS;

/// <summary>
/// Represents a void return value for commands that don't produce results
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// Singleton instance of Unit
    /// </summary>
    public static readonly Unit Value = new();

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public override string ToString() => "()";

    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}
```

**Design Rationale:**
- Replaces `void` in generic contexts (can't use `Result<void>`)
- Value type (struct) with zero memory overhead
- Singleton pattern via `Unit.Value`
- All instances equal (no state)

### 3. Define Command Interfaces

**File:** `src/Idevs/CQRS/Commands/ICommand.cs`

```csharp path=null start=null
namespace Idevs.CQRS.Commands;

/// <summary>
/// Marker interface for all commands
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Represents a command that doesn't return a value
/// </summary>
public interface ICommand<out TResponse> : ICommand
{
}
```

**Design Rationale:**
- Non-generic `ICommand` for runtime discovery
- Generic `ICommand<TResponse>` for type-safe responses
- Covariant `out TResponse` allows contravariant handler assignments
- No members (pure marker interface)

### 4. Define Command Handler Interface

**File:** `src/Idevs/CQRS/Commands/ICommandHandler.cs`

```csharp path=null start=null
using Idevs.Results;

namespace Idevs.CQRS.Commands;

/// <summary>
/// Handler for a command that produces a result
/// </summary>
/// <typeparam name="TCommand">Type of command to handle</typeparam>
/// <typeparam name="TResponse">Type of response</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the command and returns a result
    /// </summary>
    Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler for a command that doesn't produce a response
/// </summary>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand<Unit>
{
}
```

**Design Rationale:**
- Contravariant `in TCommand` for flexible handler registration
- Returns `Task<Result<TResponse>>` for async + railway pattern
- `CancellationToken` default parameter for convenience
- Non-generic handler inherits from generic for consistency

### 5. Define Query Interface

**File:** `src/Idevs/CQRS/Queries/IQuery.cs`

```csharp path=null start=null
namespace Idevs.CQRS.Queries;

/// <summary>
/// Marker interface for all queries
/// </summary>
public interface IQuery
{
}

/// <summary>
/// Represents a query that returns data
/// </summary>
/// <typeparam name="TResponse">Type of data returned</typeparam>
public interface IQuery<out TResponse> : IQuery
{
}
```

**Design Rationale:**
- Identical structure to commands (consistency)
- Semantic separation (intent is different)
- Enables different middleware pipelines

### 6. Define Query Handler Interface

**File:** `src/Idevs/CQRS/Queries/IQueryHandler.cs`

```csharp path=null start=null
using Idevs.Results;

namespace Idevs.CQRS.Queries;

/// <summary>
/// Handler for a query that returns data
/// </summary>
/// <typeparam name="TQuery">Type of query to handle</typeparam>
/// <typeparam name="TResponse">Type of response</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the query and returns a result
    /// </summary>
    Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default);
}
```

**Design Rationale:**
- Symmetric with command handler interface
- Queries always return data (no parameterless version)
- CancellationToken for long-running queries

### 7. Create Example Command/Handler

**File:** `src/Idevs/CQRS/Commands/Examples/CreateCustomerCommand.cs`

```csharp path=null start=null
namespace Idevs.CQRS.Commands.Examples;

/// <summary>
/// Example command for creating a customer
/// </summary>
public sealed record CreateCustomerCommand(
    string Name,
    string Email) : ICommand<Guid>;
```

**File:** `src/Idevs/CQRS/Commands/Examples/CreateCustomerCommandHandler.cs`

```csharp path=null start=null
using Idevs.Results;

namespace Idevs.CQRS.Commands.Examples;

/// <summary>
/// Example handler for CreateCustomerCommand
/// </summary>
public sealed class CreateCustomerCommandHandler 
    : ICommandHandler<CreateCustomerCommand, Guid>
{
    public Task<Result<Guid>> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(command.Name))
            return Task.FromResult(
                Result<Guid>.Failure(DomainErrors.Validation.Required(nameof(command.Name))));

        // Business logic
        var customerId = Guid.NewGuid();
        
        // In real implementation: save to repository
        
        return Task.FromResult(Result<Guid>.Success(customerId));
    }
}
```

### 8. Create Example Query/Handler

**File:** `src/Idevs/CQRS/Queries/Examples/GetCustomerByIdQuery.cs`

```csharp path=null start=null
namespace Idevs.CQRS.Queries.Examples;

public sealed record CustomerDto(Guid Id, string Name, string Email);

/// <summary>
/// Example query for retrieving a customer by ID
/// </summary>
public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDto>;
```

**File:** `src/Idevs/CQRS/Queries/Examples/GetCustomerByIdQueryHandler.cs`

```csharp path=null start=null
using Idevs.Results;

namespace Idevs.CQRS.Queries.Examples;

/// <summary>
/// Example handler for GetCustomerByIdQuery
/// </summary>
public sealed class GetCustomerByIdQueryHandler 
    : IQueryHandler<GetCustomerByIdQuery, CustomerDto>
{
    public Task<Result<CustomerDto>> HandleAsync(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        // In real implementation: query from read model/database
        
        if (query.CustomerId == Guid.Empty)
            return Task.FromResult(
                Result<CustomerDto>.Failure(
                    DomainErrors.Entity.NotFound(nameof(Customer), query.CustomerId)));

        var customer = new CustomerDto(
            query.CustomerId,
            "John Doe",
            "john@example.com");

        return Task.FromResult(Result<CustomerDto>.Success(customer));
    }
}
```

### 9. Write Unit Tests

**File:** `tests/Idevs.Tests/CQRS/CqrsContractsTests.cs`

```csharp path=null start=null
using Idevs.CQRS;
using Idevs.CQRS.Commands;
using Idevs.CQRS.Commands.Examples;
using Idevs.CQRS.Queries;
using Idevs.CQRS.Queries.Examples;
using Shouldly;

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 →](04-cqrs-contracts-advanced.md)**
