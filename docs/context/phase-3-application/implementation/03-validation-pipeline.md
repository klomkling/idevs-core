# Guide 3: Validation Pipeline

**Phase**: 3 - Application Layer  
**Component**: FluentValidation Integration & Validation Behavior  
**Prerequisites**: Guide 2 (MediatR Integration)  
**Estimated Time**: 2-3 hours

---

## Table of Contents

1. [Overview](#overview)
2. [Design Rationale](#design-rationale)
3. [Installation & Setup](#installation--setup)
4. [FluentValidation Basics](#fluentvalidation-basics)
5. [Validation Behavior Implementation](#validation-behavior-implementation)
6. [Advanced Validation Patterns](#advanced-validation-patterns)
7. [Usage Examples](#usage-examples)
8. [Unit Testing](#unit-testing)
9. [Best Practices](#best-practices)
10. [Common Pitfalls](#common-pitfalls)
11. [Next Steps](#next-steps)

---

## Overview

The Validation Pipeline enforces input validation **before** commands/queries reach handlers. Using FluentValidation with MediatR's pipeline behaviors provides:

- **Fail-fast validation**: Catch errors early
- **Declarative rules**: Readable, maintainable validation logic
- **Composability**: Reuse validators across commands
- **Async support**: Database lookups during validation
- **Comprehensive error messages**: Multiple validation failures returned at once

### Key Components

| Component | Purpose |
|-----------|---------|
| `AbstractValidator<T>` | Defines validation rules for a request |
| `ValidationBehavior<TRequest, TResponse>` | MediatR behavior that runs validators |
| `ValidationError` | Value object holding field + error message |
| `Error.Validation()` | Creates Result<T> failure with validation errors |

---

## Design Rationale

### Why FluentValidation?

1. **Separation of concerns**: Validation lives outside handlers
2. **Testability**: Validators are easily unit tested
3. **Rich DSL**: Intuitive rule syntax
4. **Built-in rules**: Email, URL, range, regex, etc.
5. **Custom validators**: Easy to extend
6. **Async validation**: For database checks (unique email, etc.)

### Why Validate in Pipeline?

```
Request → ValidationBehavior → Handler
              ↑ Validation fails
              ↓ Return error, handler never executes
```

**Benefits**:
- Handlers assume valid input
- Single place to enforce rules
- Consistent error format
- No validation logic duplication

---

## Installation & Setup

### 1. Install NuGet Package

```xml
<ItemGroup>
  <PackageReference Include="FluentValidation" Version="11.9.0" />
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
</ItemGroup>
```

### 2. Register Validators

```csharp
namespace Idevs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        Assembly assembly)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Auto-register all validators from assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

---

## FluentValidation Basics

### Simple Validator

```csharp
namespace Idevs.Application.Products.Commands.Create;

public sealed class CreateProductCommandValidator 
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(200)
            .WithMessage("Product name must not exceed 200 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than zero");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category ID is required");
    }
}
```

### Common Rules

```csharp
// String validation
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress();

RuleFor(x => x.Url)
    .Must(BeValidUrl)
    .WithMessage("Invalid URL format");

// Numeric validation
RuleFor(x => x.Quantity)
    .InclusiveBetween(1, 1000);

// Collection validation
RuleFor(x => x.Tags)
    .NotEmpty()
    .WithMessage("At least one tag is required");

RuleForEach(x => x.Items)
    .SetValidator(new OrderItemValidator());

// Conditional validation
RuleFor(x => x.ShippingAddress)
    .NotNull()
    .When(x => x.RequiresShipping);

// Complex validation
RuleFor(x => x)
    .Must(HaveValidDiscountLogic)
    .WithMessage("Discount cannot exceed original price");
```

---

## Validation Behavior Implementation

### Core Validation Behavior

```csharp
namespace Idevs.Application.Behaviors;

/// <summary>
/// Validates requests using FluentValidation before executing handlers.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip if no validators registered
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        // Run all validators in parallel
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect all failures
        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .GroupBy(e => e.PropertyName)
            .Select(g => new ValidationError(
                g.Key,
                g.First().ErrorMessage))
            .ToArray();

        if (failures.Length == 0)
            return await next();

        // Return validation error result
        var error = Error.Validation(
            "Validation.Error",
            "One or more validation errors occurred.",
            failures);

        // Cast to Result<T>
        // Assumes TResponse is Result<T> or implements IResult
        return CreateValidationResult<TResponse>(error);
    }

    private static TResponse CreateValidationResult<T>(Error error)
    {
        // Get the generic type argument from Result<T>
        var resultType = typeof(T);

        if (resultType.IsGenericType && 
            resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = resultType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure))!
                .MakeGenericMethod(valueType);

            return (T)failureMethod.Invoke(null, new object[] { error })!;
        }

        throw new InvalidOperationException(
            $"TResponse must be Result<T>, but was {resultType}");
    }
}
```

---

### Enhanced Validation Behavior with Logging

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var requestName = typeof(TRequest).Name;

        _logger.LogDebug(
            "Validating {RequestName} with {ValidatorCount} validator(s)",
            requestName,
            _validators.Count());

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count == 0)
        {
            _logger.LogDebug("{RequestName} validation passed", requestName);
            return await next();
        }

        _logger.LogWarning(
            "{RequestName} validation failed with {FailureCount} error(s): {@Failures}",
            requestName,
            failures.Count,
            failures.Select(f => new { f.PropertyName, f.ErrorMessage }));

        var validationErrors = failures
            .GroupBy(e => e.PropertyName)
            .Select(g => new ValidationError(g.Key, g.First().ErrorMessage))
            .ToArray();

        var error = Error.Validation(
            "Validation.Error",
            "One or more validation errors occurred.",
            validationErrors);

        return CreateValidationResult<TResponse>(error);
    }

    private static TResponse CreateValidationResult<T>(Error error)
    {
        var resultType = typeof(T);

        if (resultType.IsGenericType && 
            resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = resultType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure))!
                .MakeGenericMethod(valueType);

            return (T)failureMethod.Invoke(null, new object[] { error })!;
        }

        throw new InvalidOperationException(
            $"TResponse must be Result<T>, but was {resultType}");
    }
}
```

---

## Advanced Validation Patterns

### Pattern 1: Async Validation with Repository

```csharp
public sealed class CreateUserCommandValidator 
    : AbstractValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email is already registered");
    }

    private async Task<bool> BeUniqueEmail(
        string email,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository
            .GetByEmailAsync(email, cancellationToken);

        return existingUser is null;
    }
}
```

---

### Pattern 2: Cross-Field Validation

```csharp
public sealed class CreateDiscountCommandValidator 
    : AbstractValidator<CreateDiscountCommand>
{
    public CreateDiscountCommandValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .LessThan(x => x.EndDate)
            .WithMessage("Start date must be before end date");

        RuleFor(x => x.DiscountAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(x => x.OriginalPrice)
            .WithMessage("Discount cannot exceed original price");

        RuleFor(x => x)
            .Must(HaveValidDates)
            .WithMessage("Date range cannot exceed 1 year");
    }

    private bool HaveValidDates(CreateDiscountCommand command)
    {
        var duration = command.EndDate - command.StartDate;
        return duration.TotalDays <= 365;
    }
}
```

---

### Pattern 3: Conditional Validation

```csharp
public sealed class CreateOrderCommandValidator 
    : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotNull()
            .When(x => x.DeliveryMethod == DeliveryMethod.HomeDelivery)
            .WithMessage("Shipping address is required for home delivery");

        RuleFor(x => x.PickupStoreId)
            .NotEmpty()
            .When(x => x.DeliveryMethod == DeliveryMethod.StorePickup)
            .WithMessage("Store selection is required for pickup");

        RuleFor(x => x.GiftMessage)
            .MaximumLength(500)
            .When(x => x.IsGift);
    }
}
```

---

### Pattern 4: Collection Validation

```csharp
public sealed class CreateOrderCommand : ICommand<Guid>
{
    public List<OrderItemDto> Items { get; init; } = [];
    public Guid CustomerId { get; init; }
}

public sealed class CreateOrderCommandValidator 
    : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemDtoValidator());

        RuleFor(x => x.Items)
            .Must(HaveValidTotalQuantity)
            .WithMessage("Total quantity cannot exceed 100 items");
    }

    private bool HaveValidTotalQuantity(List<OrderItemDto> items)
    {
        return items.Sum(i => i.Quantity) <= 100;
    }
}

public sealed class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
{
    public OrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(50);

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0);
    }
}
```

---

### Pattern 5: Custom Validators

```csharp
public sealed class PhoneNumberValidator : PropertyValidator<string>
{
    public override string Name => "PhoneNumberValidator";

    public override bool IsValid(ValidationContext<string> context, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true; // Use NotEmpty() for required check

        // Simple regex for demonstration
        return System.Text.RegularExpressions.Regex.IsMatch(
            value,
            @"^\+?[1-9]\d{1,14}$");
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
        => "'{PropertyName}' is not a valid phone number.";
}

// Usage
public sealed class CreateCustomerCommandValidator 
    : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .SetValidator(new PhoneNumberValidator());
    }
}
```

---

## Usage Examples

### Example 1: Basic Command Validation

```csharp
// Command
public sealed record CreateProductCommand(
    string Name,
    decimal Price,
    Guid CategoryId
) : ICommand<Guid>;

// Validator
public sealed class CreateProductCommandValidator 
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name too long");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be positive");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category required");
    }
}

// Controller usage
[HttpPost]
public async Task<IActionResult> Create(CreateProductRequest request)
{
    var command = new CreateProductCommand(
        request.Name,
        request.Price,
        request.CategoryId);

    var result = await _mediator.Send(command);

    return result.IsSuccess
        ? Ok(new { productId = result.Value })
        : BadRequest(new
        {
            errors = result.Error.ValidationErrors
                .Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
        });
}
```

---

### Example 2: Async Validation

```csharp
public sealed class RegisterUserCommandValidator 
    : AbstractValidator<RegisterUserCommand>
{
    private readonly IUserRepository _userRepository;

    public RegisterUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email already registered");

        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 50)
            .MustAsync(BeUniqueUsername)
            .WithMessage("Username already taken");
    }

    private async Task<bool> BeUniqueEmail(
        string email,
        CancellationToken ct)
    {
        return await _userRepository.IsEmailUniqueAsync(email, ct);
    }

    private async Task<bool> BeUniqueUsername(
        string username,
        CancellationToken ct)
    {
        return await _userRepository.IsUsernameUniqueAsync(username, ct);
    }
}
```

---

### Example 3: Multi-Stage Validation

```csharp
public sealed class UpdateUserProfileCommandValidator 
    : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator(IUserRepository userRepository)
    {
        // Stage 1: Basic format validation (fast)
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        // Stage 2: Business rules (medium)
        RuleFor(x => x.BirthDate)
            .Must(BeAtLeast18YearsOld)
            .WithMessage("User must be at least 18 years old");

        // Stage 3: Database lookups (slow) - only if previous rules pass
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) =>
            {
                var user = await userRepository.GetByEmailAsync(email, ct);
                return user is null || user.Id == x.UserId;
            })
            .WithMessage("Email already in use")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }

    private bool BeAtLeast18YearsOld(DateTime birthDate)
    {
        var age = DateTime.UtcNow.Year - birthDate.Year;
        if (birthDate > DateTime.UtcNow.AddYears(-age))
            age--;

        return age >= 18;
    }
}
```

---

## Unit Testing

### Test 1: Validator Unit Test

```csharp
namespace Idevs.Application.Tests.Products.Commands;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator;

    public CreateProductCommandValidatorTests()
    {
        _validator = new CreateProductCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Valid Product",
            99.99m,
            Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyName_FailsValidation()
    {
        // Arrange
        var command = new CreateProductCommand(
            "",
            99.99m,
            Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => 
            e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void Validate_NegativePrice_FailsValidation()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Product",
            -10m,
            Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => 
            e.PropertyName == nameof(CreateProductCommand.Price));
        result.Errors.First(e => e.PropertyName == nameof(CreateProductCommand.Price))
            .ErrorMessage.ShouldBe("Price must be positive");
    }

    [Fact]
    public void Validate_EmptyCategoryId_FailsValidation()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Product",
            99.99m,
            Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => 
            e.PropertyName == nameof(CreateProductCommand.CategoryId));
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllFailures()
    {
        // Arrange
        var command = new CreateProductCommand("", -10m, Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(3);
    }
}
```

---

### Test 2: Async Validator Test

```csharp
public sealed class RegisterUserCommandValidatorTests
{
    [Fact]
    public async Task Validate_UniqueEmail_PassesValidation()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .IsEmailUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var validator = new RegisterUserCommandValidator(userRepository);
        var command = new RegisterUserCommand(
            "test@example.com",
            "username",
            "password123");

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        await userRepository.Received(1).IsEmailUniqueAsync(
            "test@example.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_DuplicateEmail_FailsValidation()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .IsEmailUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var validator = new RegisterUserCommandValidator(userRepository);
        var command = new RegisterUserCommand(
            "existing@example.com",
            "username",
            "password123");

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => 
            e.PropertyName == nameof(RegisterUserCommand.Email) &&
            e.ErrorMessage.Contains("already registered"));
    }
}
```

---

### Test 3: ValidationBehavior Integration Test

```csharp
public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidCommand_CallsNextDelegate()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>>
        {
            new TestCommandValidator()
        };

        var logger = Substitute.For<ILogger<ValidationBehavior<TestCommand, Result<string>>>>();
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators, logger);

        var command = new TestCommand { Value = "Valid" };
        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("Success"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_InvalidCommand_DoesNotCallNext()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>>
        {
            new TestCommandValidator()
        };

        var logger = Substitute.For<ILogger<ValidationBehavior<TestCommand, Result<string>>>>();
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators, logger);

        var command = new TestCommand { Value = "" }; // Invalid
        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("Success"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    private sealed record TestCommand : ICommand<string>
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }
}
```

---

## Best Practices

### ✅ DO

1. **Create one validator per command/query**
   ```csharp
   public sealed class CreateProductCommandValidator 
       : AbstractValidator<CreateProductCommand>
   ```

2. **Use meaningful error messages**
   ```csharp
   RuleFor(x => x.Email)
       .NotEmpty().WithMessage("Email address is required")
       .EmailAddress().WithMessage("Email address format is invalid");
   ```

3. **Group related rules**
   ```csharp
   // Group all Name validations together
   RuleFor(x => x.Name)
       .NotEmpty()
       .MaximumLength(200)
       .Must(NotContainSpecialCharacters);
   ```

4. **Use async validation sparingly**
   ```csharp
   // Only for database lookups that are truly necessary
   RuleFor(x => x.Email)
       .MustAsync(BeUniqueEmail)
       .When(x => !string.IsNullOrEmpty(x.Email)); // Guard clause
   ```

5. **Test validators independently**
   ```csharp
   var validator = new CreateProductCommandValidator();
   var result = validator.Validate(command);
   result.IsValid.ShouldBeTrue();
   ```

---

### ❌ DON'T

1. **Don't put domain logic in validators**
   ```csharp
   // ❌ Bad: Domain logic
   RuleFor(x => x)
       .Must(command => command.Price * command.Quantity < 10000);

   // ✅ Good: Simple input validation
   RuleFor(x => x.Price).GreaterThan(0);
   RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
   ```

2. **Don't validate in handlers**
   ```csharp
   // ❌ Bad
   public async Task<Result<Guid>> Handle(...)
   {
       if (string.IsNullOrEmpty(command.Name))
           return Result.Failure<Guid>(Error.Validation(...));
   }

   // ✅ Good: Use validator
   public sealed class CreateProductCommandValidator : AbstractValidator<...>
   ```

3. **Don't ignore async validation cancellation**
   ```csharp
   // ❌ Bad
   private async Task<bool> BeUnique(string email)
   {
       return await _repo.IsUniqueAsync(email); // Missing CT
   }

   // ✅ Good
   private async Task<bool> BeUnique(string email, CancellationToken ct)
   {
       return await _repo.IsUniqueAsync(email, ct);
   }
   ```

4. **Don't create validators with mutable state**
   ```csharp
   // ❌ Bad
   public sealed class MyValidator : AbstractValidator<MyCommand>
   {
       private int _validationCount; // Mutable state!
   }

   // ✅ Good: Stateless
   public sealed class MyValidator : AbstractValidator<MyCommand>
   {
       private readonly IService _service; // Injected dependency only
   }
   ```

5. **Don't over-validate**
   ```csharp
   // ❌ Bad: Unnecessary validation
   RuleFor(x => x.Id)
       .Must(BeValidGuid); // Guid is already type-safe!

   // ✅ Good: Only validate business rules
   RuleFor(x => x.Id)
       .NotEmpty(); // Check if provided
   ```

---

## Common Pitfalls

### Pitfall 1: Async Validation Performance

**Problem**: Running slow async validations unnecessarily.

```csharp
// ❌ Bad: Checks database even if email format is invalid
RuleFor(x => x.Email)
    .MustAsync(BeUniqueEmail);
```

**Solution**: Validate format first, then uniqueness.

```csharp
// ✅ Good: Fast-fail on format, then check database
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress()
    .MustAsync(BeUniqueEmail)
    .When(x => !string.IsNullOrEmpty(x.Email));
```

---

### Pitfall 2: Validator Dependencies Not Registered

**Problem**: Validator constructor dependencies not resolved.

```csharp
// ❌ Bad: IUserRepository not registered
public sealed class MyValidator : AbstractValidator<MyCommand>
{
    public MyValidator(IUserRepository repo) // DI fails!
    {
        _repo = repo;
    }
}
```

**Solution**: Ensure all dependencies are registered.

```csharp
// ✅ Good: Register repository
services.AddScoped<IUserRepository, UserRepository>();
services.AddValidatorsFromAssembly(assembly); // Auto-registers validators
```

---

### Pitfall 3: Incorrect Error Response Shape

**Problem**: Returning validation errors in inconsistent format.

```csharp
// ❌ Bad: Custom error handling in behavior
if (failures.Any())
    return (TResponse)(object)new { errors = failures }; // Wrong type!
```

**Solution**: Use Error.Validation with Result<T>.

```csharp
// ✅ Good
var error = Error.Validation(
    "Validation.Error",
    "Validation failed",
    failures.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage)).ToArray());

return CreateValidationResult<TResponse>(error);
```

---

### Pitfall 4: Not Grouping Duplicate Errors

**Problem**: Multiple validators return same property error.

```csharp
// Multiple errors for "Email": required, format, unique
// Without grouping: Email appears 3 times in response
```

**Solution**: Group by property name.

```csharp
// ✅ Good
var validationErrors = failures
    .GroupBy(e => e.PropertyName)
    .Select(g => new ValidationError(g.Key, g.First().ErrorMessage))
    .ToArray();
```

---

## Next Steps

1. **Read Guide 4**: Authorization (`04-AUTHORIZATION.md`)
2. **Implement ValidationBehavior**: Add to MediatR pipeline
3. **Create Sample Validators**: Write validators for 2-3 commands
4. **Test Validators**: Unit test validation rules
5. **Integrate with Controllers**: Return validation errors as 400 Bad Request

---

**Summary**: The Validation Pipeline uses FluentValidation to enforce input rules before handlers execute. Validators are declarative, testable, and support async validation for database checks. The ValidationBehavior ensures consistent error handling across all commands and queries.

---

**Implementation Checklist**:

- [ ] Install FluentValidation NuGet packages
- [ ] Register validators with `AddValidatorsFromAssembly()`
- [ ] Implement `ValidationBehavior<TRequest, TResponse>`
- [ ] Add ValidationBehavior to MediatR pipeline
- [ ] Create validators for existing commands/queries
- [ ] Write unit tests for validators
- [ ] Test ValidationBehavior with integration test
- [ ] Update API error responses to include validation errors
- [ ] Document common validation patterns for team

---

**Last Updated**: 2025-01-08  
**Next Guide**: 04-AUTHORIZATION.md
