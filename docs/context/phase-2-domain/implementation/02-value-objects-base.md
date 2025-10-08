# 02: Value Objects - Part 1

> **Navigation:** [Index](README.md) • [Part 1](02-value-objects-base.md) • [Part 2](02-value-objects-examples.md)

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

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 →](02-value-objects-examples.md)**
