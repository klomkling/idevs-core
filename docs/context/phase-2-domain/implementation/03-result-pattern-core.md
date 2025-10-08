# 03: Result Patterns - Part 1

> **Navigation:** [Index](README.md) • [Part 1](03-result-pattern-core.md) • [Part 2](03-result-pattern-extensions.md)


> **Phase:** 2 - Domain & Contracts  
> **Guide:** 03 of 09  
> **Estimated Time:** 3-4 hours

## Overview

Implement the Result<T> pattern to replace exception-driven error handling with explicit, composable, and testable result objects across domain and application layers.

## Prerequisites

- Phase 1 complete (solution structure)
- Understanding of railway-oriented programming concepts
- Familiarity with generic types and pattern matching in C#

## Objectives

### What You'll Build

1. **Result<T>** - Generic result type for success/failure states
2. **Error** - Structured error representation
3. **PagedResult<T>** - Pagination metadata wrapper
4. **ValidationResult** - Specialized result for validation failures
5. **Result extension methods** - Map, Bind, Match for functional composition

### Why This Matters

- Eliminates exceptions for expected failure paths (404, validation errors)
- Makes success/failure explicit in method signatures
- Enables functional error composition (map, bind, match)
- Improves testability and performance (no stack unwinding)
- Provides consistent error structure for API responses

## Implementation Steps

### 1. Create Results Folder

```bash
mkdir -p src/Idevs/Results
```

### 2. Define Error Class

**File:** `src/Idevs/Results/Error.cs`

```csharp path=null start=null
namespace Idevs.Results;

/// <summary>
/// Represents a structured error with code, message, and optional metadata
/// </summary>
public sealed class Error : IEquatable<Error>
{
    /// <summary>
    /// Sentinel value representing no error
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Sentinel value representing a null value error
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided");

    public Error(string code, string message, Dictionary<string, object>? metadata = null)
    {
        Code = code;
        Message = message;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Unique error code for categorization (e.g., "Validation.Required", "Domain.NotFound")
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Additional contextual metadata (field names, constraints, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    public bool Equals(Error? other)
    {
        if (other is null) return false;
        return Code == other.Code && Message == other.Message;
    }

    public override bool Equals(object? obj) => obj is Error error && Equals(error);
    public override int GetHashCode() => HashCode.Combine(Code, Message);
    
    public static bool operator ==(Error? left, Error? right) => Equals(left, right);
    public static bool operator !=(Error? left, Error? right) => !Equals(left, right);

    public override string ToString() => $"[{Code}] {Message}";
}
```

**Design Rationale:**
- Immutable by design (init-only properties)
- Metadata for structured error context (e.g., field names in validation)
- Sentinel values (None, NullValue) avoid null checks
- Value equality for error comparison in tests

### 3. Define Result<T> Class

**File:** `src/Idevs/Results/Result.cs`

```csharp path=null start=null
namespace Idevs.Results;

/// <summary>
/// Represents an operation result that can be either successful with a value or failed with an error
/// </summary>
/// <typeparam name="T">Type of the success value</typeparam>
public class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    protected Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    protected Result(Error error)
    {
        if (error == Error.None)
            throw new ArgumentException("Cannot create failed result with Error.None", nameof(error));

        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Indicates whether the operation succeeded
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Throws if result is failed.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of failed result");

    /// <summary>
    /// Gets the error. Throws if result is successful.
    /// </summary>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access error of successful result");

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Implicit conversion from value to Result<T>
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion from Error to Result<T>
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}
```

**Design Rationale:**
- Private fields with explicit access control prevent misuse
- Throws on invalid access (helps catch bugs early)
- Implicit operators enable clean syntax: `return customer;` or `return error;`
- Protected constructors + factory methods enforce valid states

### 4. Add Result Extension Methods

**File:** `src/Idevs/Results/ResultExtensions.cs`

```csharp path=null start=null
namespace Idevs.Results;

public static class ResultExtensions
{
    /// <summary>
    /// Transforms the success value if result is successful
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
    {
        return result.IsSuccess 
            ? Result<TOut>.Success(map(result.Value))
            : Result<TOut>.Failure(result.Error);
    }

    /// <summary>
    /// Chains another result-returning operation if current result is successful
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind)
    {
        return result.IsSuccess 
            ? bind(result.Value)
            : Result<TOut>.Failure(result.Error);
    }

    /// <summary>
    /// Pattern matches on success/failure
    /// </summary>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        return result.IsSuccess 
            ? onSuccess(result.Value)
            : onFailure(result.Error);
    }

    /// <summary>
    /// Executes action if result is successful, returns original result
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        
        return result;
    }
}
```

**Design Rationale:**
- `Map` transforms value (keeps error path unchanged)
- `Bind` enables chaining result-returning operations
- `Match` forces handling both success and failure
- `Tap` for side effects (logging, events) without breaking the chain

### 5. Define PagedResult<T>

**File:** `src/Idevs/Results/PagedResult.cs`

```csharp path=null start=null
namespace Idevs.Results;

/// <summary>
/// Represents a paginated result with metadata
/// </summary>
public sealed class PagedResult<T> : Result<IReadOnlyList<T>>
{
    private PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount) : base(items)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    private PagedResult(Error error) : base(error)
    {
    }

    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Success(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        return new PagedResult<T>(items, page, pageSize, totalCount);
    }

    public new static PagedResult<T> Failure(Error error) => new(error);
}
```

**Design Rationale:**
- Inherits from Result<IReadOnlyList<T>> for consistency
- Computed properties (TotalPages, HasNextPage) reduce client logic
- Immutable pagination metadata
- Standard pagination calculations (ceiling division)

### 6. Define Common Domain Errors

**File:** `src/Idevs/Results/DomainErrors.cs`

```csharp path=null start=null
namespace Idevs.Results;

/// <summary>

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 →](03-result-pattern-extensions.md)**
