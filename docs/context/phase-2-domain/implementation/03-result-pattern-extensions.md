# 03: Result Patterns - Part 2

> **Navigation:** [Index](README.md) • [Part 1](03-result-pattern-core.md) • [Part 2](03-result-pattern-extensions.md)

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

---

**[← Part 1](03-result-pattern-core.md)** | **[Back to Phase 2](../phase-2-domain.md)**
