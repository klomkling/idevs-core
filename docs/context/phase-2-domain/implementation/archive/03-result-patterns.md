# 03: Result Patterns

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
/// Common domain error codes and factory methods
/// </summary>
public static class DomainErrors
{
    public static class Validation
    {
        public static Error Required(string field) =>
            new($"Validation.{field}.Required", $"{field} is required");

        public static Error InvalidFormat(string field) =>
            new($"Validation.{field}.InvalidFormat", $"{field} has invalid format");

        public static Error MaxLength(string field, int maxLength) =>
            new(
                $"Validation.{field}.MaxLength",
                $"{field} must not exceed {maxLength} characters",
                new Dictionary<string, object> { ["MaxLength"] = maxLength });
    }

    public static class Entity
    {
        public static Error NotFound(string entityName, object id) =>
            new(
                $"Entity.{entityName}.NotFound",
                $"{entityName} with ID {id} was not found",
                new Dictionary<string, object> { ["EntityId"] = id });

        public static Error AlreadyExists(string entityName, string field, object value) =>
            new(
                $"Entity.{entityName}.AlreadyExists",
                $"{entityName} with {field} = {value} already exists",
                new Dictionary<string, object> { ["Field"] = field, ["Value"] = value });
    }

    public static class Authorization
    {
        public static readonly Error Unauthorized = new(
            "Authorization.Unauthorized",
            "User is not authenticated");

        public static readonly Error Forbidden = new(
            "Authorization.Forbidden",
            "User lacks permission to perform this action");

        public static Error InsufficientPermissions(string resource, string action) =>
            new(
                "Authorization.InsufficientPermissions",
                $"User cannot perform {action} on {resource}");
    }
}
```

### 7. Write Comprehensive Tests

**File:** `tests/Idevs.Tests/Results/ResultTests.cs`

```csharp path=null start=null
using Idevs.Results;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var error = new Error("Test.Error", "Test message");
        var result = Result<int>.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Value_ThrowsOnFailedResult()
    {
        var result = Result<int>.Failure(new Error("Test", "Error"));

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Map_TransformsSuccessValue()
    {
        var result = Result<int>.Success(10);

        var mapped = result.Map(x => x * 2);

        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(20);
    }

    [Fact]
    public void Map_PreservesError()
    {
        var error = new Error("Test", "Error");
        var result = Result<int>.Failure(error);

        var mapped = result.Map(x => x * 2);

        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe(error);
    }

    [Fact]
    public void Bind_ChainsSuccessfulOperations()
    {
        var result = Result<int>.Success(10);

        var bound = result.Bind(x => Result<string>.Success(x.ToString()));

        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe("10");
    }

    [Fact]
    public void Bind_ShortCircuitsOnFailure()
    {
        var error = new Error("Test", "Error");
        var result = Result<int>.Failure(error);

        var bound = result.Bind(x => Result<string>.Success(x.ToString()));

        bound.IsFailure.ShouldBeTrue();
        bound.Error.ShouldBe(error);
    }

    [Fact]
    public void Match_HandlesSuccessPath()
    {
        var result = Result<int>.Success(42);

        var output = result.Match(
            onSuccess: x => $"Value: {x}",
            onFailure: e => $"Error: {e.Code}");

        output.ShouldBe("Value: 42");
    }

    [Fact]
    public void ImplicitConversion_FromValue()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void ImplicitConversion_FromError()
    {
        var error = new Error("Test", "Message");
        Result<int> result = error;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }
}
```

### 8. Test Paged Results

**File:** `tests/Idevs.Tests/Results/PagedResultTests.cs`

```csharp path=null start=null
using Idevs.Results;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Results;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_CalculatesTotalPages()
    {
        var items = new List<int> { 1, 2, 3 };
        var result = PagedResult<int>.Success(items, page: 1, pageSize: 10, totalCount: 25);

        result.TotalPages.ShouldBe(3);
    }

    [Fact]
    public void HasNextPage_ReturnsTrueWhenMorePagesExist()
    {
        var result = PagedResult<int>.Success(new List<int>(), page: 1, pageSize: 10, totalCount: 25);

        result.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public void HasPreviousPage_ReturnsFalseForFirstPage()
    {
        var result = PagedResult<int>.Success(new List<int>(), page: 1, pageSize: 10, totalCount: 25);

        result.HasPreviousPage.ShouldBeFalse();
    }
}
```

### 9. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~ResultTests"
```

Expected: ✅ All 13+ tests passing

## Usage Examples

### Domain Method Returning Result

```csharp path=null start=null
public Result<Customer> CreateCustomer(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        return DomainErrors.Validation.Required(nameof(name));

    if (!IsValidEmail(email))
        return DomainErrors.Validation.InvalidFormat(nameof(email));

    var customer = new Customer { Name = name, Email = email };
    return Result<Customer>.Success(customer);
}
```

### Chaining Operations

```csharp path=null start=null
var result = GetCustomerById(id)
    .Bind(customer => ValidateCustomer(customer))
    .Map(customer => new CustomerDto(customer.Name, customer.Email))
    .Tap(dto => _logger.LogInformation("Customer retrieved: {Id}", id));

return result.Match(
    onSuccess: dto => Ok(dto),
    onFailure: error => error.Code switch
    {
        "Entity.Customer.NotFound" => NotFound(error.Message),
        _ => BadRequest(error.Message)
    });
```

## Verification Checklist

- [ ] Result<T> class with Success/Failure factory methods
- [ ] Error class with Code, Message, Metadata
- [ ] Extension methods: Map, Bind, Match, Tap
- [ ] PagedResult<T> with pagination metadata
- [ ] DomainErrors static class with common errors
- [ ] Implicit operators for clean syntax
- [ ] ≥10 unit tests covering all scenarios
- [ ] Tests verify error propagation in chains

## Common Pitfalls

### ❌ Throwing Exceptions for Expected Errors
```csharp
// BAD - exceptions for flow control
public Customer GetCustomer(Guid id)
{
    var customer = _repo.Find(id);
    if (customer is null)
        throw new NotFoundException($"Customer {id} not found");
    return customer;
}

// GOOD - result pattern
public Result<Customer> GetCustomer(Guid id)
{
    var customer = _repo.Find(id);
    return customer is null
        ? DomainErrors.Entity.NotFound(nameof(Customer), id)
        : customer;
}
```

### ❌ Not Handling Failure Path
```csharp
// BAD - assumes success
var result = GetCustomer(id);
var name = result.Value.Name; // throws if failed!

// GOOD - explicit handling
var name = GetCustomer(id).Match(
    onSuccess: c => c.Name,
    onFailure: _ => "Unknown");
```

## Next Steps

- **[04-cqrs-contracts.md](04-cqrs-contracts.md)** - Command/Query interfaces
- **[05-aggregates-entities.md](05-aggregates-entities.md)** - Entity base classes

## References

- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [C# Result Pattern](https://github.com/vkhorikov/CSharpFunctionalExtensions)

---

**[← Back to Phase 2](../phase-2-domain-NEW.md)** | **[Next: CQRS Contracts →](04-cqrs-contracts.md)**
