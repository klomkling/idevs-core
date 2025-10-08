# 02: Value Objects

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 02 of 09  
> **Estimated Time:** 2-3 hours

## Overview

Implement the Value Object base class and common domain value objects (Email, Money, Address) that enforce domain invariants through immutability and structural equality.

## Prerequisites

- Phase 1 complete (solution structure)
- Guide 01 complete (entity interfaces)
- Understanding of value object concepts in DDD
- Familiarity with C# records and equality comparison

## Objectives

### What You'll Build

1. **ValueObject base class** - Structural equality and immutability patterns
2. **Email** - Email address value object with validation
3. **Money** - Currency amount with arithmetic operations
4. **Address** - Multi-field value object example
5. **DateRange** - Temporal range with overlap detection

### Why This Matters

- Encapsulates domain rules and validation
- Prevents primitive obsession (passing raw strings/decimals)
- Enables type-safe APIs (can't accidentally swap email and username)
- Immutability prevents bugs from unintended mutations
- Value equality simplifies testing and comparisons

## Implementation Steps

### 1. Create Domain Folder

```bash
mkdir -p src/Idevs/Domain/ValueObjects
```

### 2. Define ValueObject Base Class

**File:** `src/Idevs/Domain/ValueObjects/ValueObject.cs`

```csharp path=null start=null
namespace Idevs.Domain.ValueObjects;

/// <summary>
/// Base class for all value objects. Implements structural equality.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns atomic values used for equality comparison
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && Equals(other);
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(default(int), (hash, component) =>
            {
                unchecked
                {
                    return (hash * 397) ^ (component?.GetHashCode() ?? 0);
                }
            });
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
```

**Design Rationale:**
- Abstract `GetEqualityComponents()` forces subclasses to define equality
- `SequenceEqual` compares component order (important for multi-field VOs)
- Unchecked hash aggregation prevents overflow exceptions
- Null-safe equality operators

### 3. Implement Email Value Object

**File:** `src/Idevs/Domain/ValueObjects/Email.cs`

```csharp path=null start=null
using System.Text.RegularExpressions;
using Idevs.Results;

namespace Idevs.Domain.ValueObjects;

/// <summary>
/// Represents a validated email address
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>
    /// Creates an Email value object with validation
    /// </summary>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Validation.Required(nameof(Email));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 254) // RFC 5321 max length
            return new Error("Email.TooLong", "Email address cannot exceed 254 characters");

        if (!EmailRegex.IsMatch(normalized))
            return DomainErrors.Validation.InvalidFormat(nameof(Email));

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    // Implicit conversion for convenience
    public static implicit operator string(Email email) => email.Value;
}
```

**Design Rationale:**
- Private constructor + static factory enforces validation
- Returns `Result<Email>` for composability
- Normalizes emails (lowercase, trim) for consistent comparison
- RFC 5321 compliant length check
- Compiled regex with timeout prevents ReDoS attacks
- Immutable by design (no setters)

### 4. Implement Money Value Object

**File:** `src/Idevs/Domain/ValueObjects/Money.cs`

```csharp path=null start=null
using Idevs.Results;

namespace Idevs.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with currency
/// </summary>
public sealed class Money : ValueObject
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return new Error("Money.CurrencyRequired", "Currency code is required");

        if (currency.Length != 3)
            return new Error("Money.InvalidCurrency", "Currency must be a 3-letter ISO code");

        if (amount < 0)
            return new Error("Money.NegativeAmount", "Amount cannot be negative");

        return new Money(Math.Round(amount, 2), currency.ToUpperInvariant());
    }

    public static Money Zero(string currency) => new(0, currency);

    // Arithmetic operations
    public Result<Money> Add(Money other)
    {
        if (Currency != other.Currency)
            return new Error("Money.CurrencyMismatch", 
                $"Cannot add {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Result<Money> Subtract(Money other)
    {
        if (Currency != other.Currency)
            return new Error("Money.CurrencyMismatch", 
                $"Cannot subtract {Currency} and {other.Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal multiplier)
    {
        return new Money(Math.Round(Amount * multiplier, 2), Currency);
    }

    // Comparison operators
    public static bool operator >(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot compare different currencies");
        
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot compare different currencies");
        
        return left.Amount < right.Amount;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
```

**Design Rationale:**
- Enforces ISO 4217 currency codes (3 letters)
- Rounds to 2 decimal places (prevents floating-point errors)
- Arithmetic operations return `Result<Money>` for currency mismatch errors
- Comparison operators throw (not common operations, should be explicit)
- Zero factory method for initial values

### 5. Implement Address Value Object

**File:** `src/Idevs/Domain/ValueObjects/Address.cs`

```csharp path=null start=null
using Idevs.Results;

namespace Idevs.Domain.ValueObjects;

/// <summary>
/// Represents a physical address
/// </summary>
public sealed class Address : ValueObject
{
    private Address(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public static Result<Address> Create(
        string? street,
        string? city,
        string? state,
        string? postalCode,
        string? country)
    {
        if (string.IsNullOrWhiteSpace(street))
            return DomainErrors.Validation.Required(nameof(Street));

        if (string.IsNullOrWhiteSpace(city))
            return DomainErrors.Validation.Required(nameof(City));

        if (string.IsNullOrWhiteSpace(state))
            return DomainErrors.Validation.Required(nameof(State));

        if (string.IsNullOrWhiteSpace(postalCode))
            return DomainErrors.Validation.Required(nameof(PostalCode));

        if (string.IsNullOrWhiteSpace(country))
            return DomainErrors.Validation.Required(nameof(Country));

        if (country.Length != 2)
            return new Error("Address.InvalidCountry", "Country must be a 2-letter ISO code");

        return new Address(
            street.Trim(),
            city.Trim(),
            state.Trim(),
            postalCode.Trim().ToUpperInvariant(),
            country.Trim().ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() => 
        $"{Street}, {City}, {State} {PostalCode}, {Country}";
}
```

**Design Rationale:**
- All fields required (nullable parameters make validation explicit)
- Normalizes postal code and country (uppercase)
- ISO 3166-1 alpha-2 country codes
- Component order matters for equality (order of yield return)

### 6. Implement DateRange Value Object

**File:** `src/Idevs/Domain/ValueObjects/DateRange.cs`

```csharp path=null start=null
using Idevs.Results;

namespace Idevs.Domain.ValueObjects;

/// <summary>
/// Represents a date range with validation
/// </summary>
public sealed class DateRange : ValueObject
{
    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public DateOnly Start { get; }
    public DateOnly End { get; }
    public int Days => End.DayNumber - Start.DayNumber + 1;

    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (start > end)
            return new Error("DateRange.InvalidRange", "Start date must be before or equal to end date");

        return new DateRange(start, end);
    }

    public bool Contains(DateOnly date)
    {
        return date >= Start && date <= End;
    }

    public bool Overlaps(DateRange other)
    {
        return Start <= other.End && End >= other.Start;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
}
```

**Design Rationale:**
- Uses `DateOnly` (no time zone issues)
- Computed `Days` property (inclusive range)
- Overlap detection for scheduling logic
- Validates start <= end invariant

### 7. Write Comprehensive Tests

**File:** `tests/Idevs.Tests/Domain/ValueObjectTests.cs`

```csharp path=null start=null
using Idevs.Domain.ValueObjects;
using Idevs.Results;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Domain;

public class ValueObjectTests
{
    private class TestValueObject : ValueObject
    {
        public string Value { get; }
        public TestValueObject(string value) => Value = value;
        
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    [Fact]
    public void ValueObject_EqualityByValue()
    {
        var vo1 = new TestValueObject("test");
        var vo2 = new TestValueObject("test");
        var vo3 = new TestValueObject("different");

        vo1.ShouldBe(vo2);
        vo1.ShouldNotBe(vo3);
        (vo1 == vo2).ShouldBeTrue();
        (vo1 != vo3).ShouldBeTrue();
    }

    [Fact]
    public void Email_Create_ValidatesFormat()
    {
        var validResult = Email.Create("user@example.com");
        var invalidResult = Email.Create("invalid-email");

        validResult.IsSuccess.ShouldBeTrue();
        validResult.Value.Value.ShouldBe("user@example.com");
        
        invalidResult.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Email_Create_NormalizesValue()
    {
        var result = Email.Create("  User@Example.COM  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("user@example.com");
    }

    [Fact]
    public void Email_Equality_CaseInsensitive()
    {
        var email1 = Email.Create("user@example.com").Value;
        var email2 = Email.Create("USER@EXAMPLE.COM").Value;

        email1.ShouldBe(email2);
    }

    [Fact]
    public void Money_Create_ValidatesCurrency()
    {
        var validResult = Money.Create(100.50m, "USD");
        var invalidResult = Money.Create(100, "US"); // Not 3 letters

        validResult.IsSuccess.ShouldBeTrue();
        invalidResult.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Money_Add_SameCurrency()
    {
        var money1 = Money.Create(100, "USD").Value;
        var money2 = Money.Create(50, "USD").Value;

        var result = money1.Add(money2);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(150);
        result.Value.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Money_Add_DifferentCurrency_Fails()
    {
        var usd = Money.Create(100, "USD").Value;
        var eur = Money.Create(50, "EUR").Value;

        var result = usd.Add(eur);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("CurrencyMismatch");
    }

    [Fact]
    public void Money_RoundsToTwoDecimals()
    {
        var result = Money.Create(10.12345m, "USD");

        result.Value.Amount.ShouldBe(10.12m);
    }

    [Fact]
    public void Address_Create_RequiresAllFields()
    {
        var result = Address.Create(
            "123 Main St",
            "Springfield",
            "IL",
            "62701",
            "US");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ToString().ShouldContain("Springfield");
    }

    [Fact]
    public void Address_Create_ValidatesCountryCode()
    {
        var result = Address.Create(
            "123 Main St",
            "City",
            "State",
            "12345",
            "USA"); // Should be 2 letters

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Create_ValidatesStartBeforeEnd()
    {
        var valid = DateRange.Create(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        var invalid = DateRange.Create(
            new DateOnly(2024, 12, 31),
            new DateOnly(2024, 1, 1));

        valid.IsSuccess.ShouldBeTrue();
        invalid.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Overlaps_DetectsOverlap()
    {
        var range1 = DateRange.Create(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 10)).Value;

        var range2 = DateRange.Create(
            new DateOnly(2024, 1, 5),
            new DateOnly(2024, 1, 15)).Value;

        var range3 = DateRange.Create(
            new DateOnly(2024, 2, 1),
            new DateOnly(2024, 2, 10)).Value;

        range1.Overlaps(range2).ShouldBeTrue();
        range1.Overlaps(range3).ShouldBeFalse();
    }

    [Fact]
    public void DateRange_CalculatesDaysCorrectly()
    {
        var range = DateRange.Create(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 10)).Value;

        range.Days.ShouldBe(10); // Inclusive
    }
}
```

### 8. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~ValueObjectTests"
```

Expected: ✅ All 16+ tests passing

## Usage Examples

### Entity with Value Objects

```csharp path=null start=null
public class Customer : Entity<Guid>
{
    public Name Name { get; private set; }
    public Email Email { get; private set; }
    public Address? ShippingAddress { get; private set; }

    public static Result<Customer> Create(string firstName, string lastName, string email)
    {
        var nameResult = Name.Create(firstName, lastName);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
            return emailResult.Error;

        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = nameResult.Value,
            Email = emailResult.Value
        };
    }

    public Result<Unit> UpdateShippingAddress(Address address)
    {
        ShippingAddress = address;
        return Unit.Value;
    }
}
```

### Combining Result Validation

```csharp path=null start=null
public Result<Order> CreateOrder(decimal amount, string currency, string customerEmail)
{
    var moneyResult = Money.Create(amount, currency);
    var emailResult = Email.Create(customerEmail);

    // Collect all validation errors
    var errors = new List<Error>();
    if (moneyResult.IsFailure) errors.Add(moneyResult.Error);
    if (emailResult.IsFailure) errors.Add(emailResult.Error);

    if (errors.Any())
        return new Error("Order.ValidationFailed", 
            "Order validation failed", 
            new Dictionary<string, object> { ["Errors"] = errors });

    return new Order(moneyResult.Value, emailResult.Value);
}
```

## Verification Checklist

- [ ] ValueObject base class with structural equality
- [ ] Email value object with validation and normalization
- [ ] Money value object with currency and arithmetic
- [ ] Address value object with multi-field validation
- [ ] DateRange with overlap detection
- [ ] All value objects immutable (no setters)
- [ ] Factory methods return Result<T>
- [ ] ≥15 unit tests covering validation and equality

## Common Pitfalls

### ❌ Mutable Value Objects
```csharp
// BAD - mutable value object
public class Email : ValueObject
{
    public string Value { get; set; } // Setter breaks immutability!
}

// GOOD - immutable
public class Email : ValueObject
{
    public string Value { get; } // Init-only
    private Email(string value) => Value = value;
}
```

### ❌ Not Validating in Factory Method
```csharp
// BAD - allows invalid state
public static Email Create(string value)
{
    return new Email(value); // No validation!
}

// GOOD - validates before construction
public static Result<Email> Create(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return DomainErrors.Validation.Required(nameof(Email));
    
    if (!IsValid(value))
        return DomainErrors.Validation.InvalidFormat(nameof(Email));
    
    return new Email(value);
}
```

### ❌ Forgetting Equality Components
```csharp
// BAD - incomplete equality
protected override IEnumerable<object?> GetEqualityComponents()
{
    yield return Street; // Forgot City, State, etc.!
}

// GOOD - all fields included
protected override IEnumerable<object?> GetEqualityComponents()
{
    yield return Street;
    yield return City;
    yield return State;
    yield return PostalCode;
    yield return Country;
}
```

## Next Steps

- **[03-result-patterns.md](03-result-patterns.md)** - Result<T> error handling
- **[04-cqrs-contracts.md](04-cqrs-contracts.md)** - Command/Query interfaces

## References

- [DDD Value Objects](https://martinfowler.com/bliki/ValueObject.html)
- [Immutability in C#](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/immutability)

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Result Patterns →](03-result-patterns.md)**
