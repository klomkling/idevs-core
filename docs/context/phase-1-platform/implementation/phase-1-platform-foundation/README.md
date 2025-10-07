# Phase 1 Platform Foundation - Implementation Guide

This document provides a comprehensive guide to the Phase 1 platform foundation implementation of the Idevs framework.

## Overview

Phase 1 delivers a solid, production-ready foundation for building modern .NET applications with:
- **Result Pattern**: Type-safe error handling without exceptions
- **CQRS Abstractions**: Command/Query separation with decorator support
- **Decorator Infrastructure**: Composable cross-cutting concerns (logging, validation, metrics)
- **Core Services**: Tenant context, current user, unit of work, metrics
- **Dependency Injection**: Clean, explicit registration with zero reflection

## Quick Start

### Installation

```bash
dotnet add package Idevs --version 0.1.0
```

> **Note**: For feature branches, the version includes a pre-release label like `0.1.0-feat-phase-1-platform-foundation`. After merging to `main`, the stable version `0.1.0` will be released.

### Basic Setup

```csharp
using Idevs.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register framework services with default configuration
services.AddIdevs();

// Register your command handlers
services.AddCommandHandler<CreateUserCommand, CreateUserCommandHandler>();

var provider = services.BuildServiceProvider();
```

### Advanced Configuration

```csharp
services.AddIdevs(options =>
{
    // Enable decorators globally
    options.Decorators.EnableLogging = true;
    options.Decorators.EnableValidation = true;
    options.Decorators.EnableMetrics = true;
});

// Per-handler configuration
services.AddCommandHandler<CreateUserCommand, CreateUserCommandHandler>(options =>
{
    // Override global settings for this handler
    options.EnableValidation = false;
});
```

## Core Concepts

### Result Pattern

The Result pattern provides type-safe error handling without throwing exceptions:

```csharp
using Idevs.Common;

// Create a success result
var successResult = Result.Success();

// Create a success result with a value
var userResult = Result<User>.Success(newUser);

// Create an error result
var errorResult = Result.Failure(
    Error.NotFound("USER_404", "User not found")
);

// Check and use the result
if (userResult.IsSuccess)
{
    var user = userResult.Value;
    // Use the user...
}
else
{
    var error = userResult.Error;
    // Handle the error...
}

// Combine multiple results
var combinedResult = Result.Combine(result1, result2, result3);
```

### Error Types

The framework provides semantic error types:

```csharp
// Validation error
var validationError = Error.Validation(
    "VAL_001",
    "Validation failed",
    new Dictionary<string, IReadOnlyList<string>>
    {
        ["Email"] = new[] { "Email is required", "Email format is invalid" }
    }
);

// Not found error
var notFoundError = Error.NotFound("USER_404", "User not found");

// Conflict error
var conflictError = Error.Conflict("USER_CONFLICT", "User already exists");

// Unauthorized error
var unauthorizedError = Error.Unauthorized("AUTH_401", "Access denied");

// Forbidden error
var forbiddenError = Error.Forbidden("AUTH_403", "Insufficient permissions");

// Business failure error
var failureError = Error.Failure("BIZ_001", "Operation failed");

// Unexpected error (internal server error)
var unexpectedError = Error.Unexpected("UNEXP_500", "Unexpected error occurred");
```

### CQRS Pattern

#### Commands

Commands represent write operations:

```csharp
using Idevs.Abstractions;
using Idevs.Common;

// Define a command (no response)
public sealed record CreateUserCommand(string Email, string Name) : ICommand;

// Define a command with response
public sealed record GetUserIdCommand(string Email) : ICommand<Guid>;

// Implement the handler
public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure(
                Error.Validation("VAL_001", "Email is required")
            );
        }

        // Execute business logic
        // ...

        return Result.Success();
    }
}

// Implement handler with response
public sealed class GetUserIdCommandHandler 
    : ICommandHandler<GetUserIdCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        GetUserIdCommand command,
        CancellationToken cancellationToken)
    {
        // Query logic
        var userId = await FindUserIdByEmail(command.Email);
        
        if (userId == Guid.Empty)
        {
            return Result<Guid>.Failure(
                Error.NotFound("USER_404", "User not found")
            );
        }

        return Result<Guid>.Success(userId);
    }
}
```

#### Queries

Queries represent read operations:

```csharp
using Idevs.Abstractions;
using Idevs.Common;

// Define a query
public sealed record GetUserQuery(Guid UserId) : IQuery<UserDto>;

// Implement the handler
public sealed class GetUserQueryHandler : IQueryHandler<GetUserQuery, UserDto>
{
    public async Task<Result<UserDto>> HandleAsync(
        GetUserQuery query,
        CancellationToken cancellationToken)
    {
        // Fetch data
        var user = await userRepository.GetByIdAsync(query.UserId);

        if (user == null)
        {
            return Result<UserDto>.Failure(
                Error.NotFound("USER_404", "User not found")
            );
        }

        return Result<UserDto>.Success(MapToDto(user));
    }
}
```

### Validation with FluentValidation

```csharp
using FluentValidation;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}

// Register the validator
services.AddSingleton<IValidator<CreateUserCommand>, CreateUserCommandValidator>();

// Enable validation decorator
services.AddIdevs(options =>
{
    options.Decorators.EnableValidation = true;
});
```

### Decorators

Decorators add cross-cutting concerns without modifying handler code:

#### Logging Decorator

Logs command/query execution:

```csharp
services.AddIdevs(options =>
{
    options.Decorators.EnableLogging = true;
});

// Logs:
// Executing command CreateUserCommand...
// Executed command CreateUserCommand in 245ms
```

#### Validation Decorator

Validates commands/queries using FluentValidation:

```csharp
services.AddIdevs(options =>
{
    options.Decorators.EnableValidation = true;
});

// Automatically validates before execution
// Returns validation errors if validation fails
```

#### Metrics Decorator

Records performance metrics:

```csharp
services.AddIdevs(options =>
{
    options.Decorators.EnableMetrics = true;
});

// Records:
// - Command execution count
// - Command execution duration
```

### Core Services

#### Tenant Context

```csharp
using Idevs.Services;

public sealed class MyService
{
    private readonly ITenantContext<Guid> _tenantContext;

    public MyService(ITenantContext<Guid> tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public void DoSomething()
    {
        var tenantId = _tenantContext.TenantId;
        // Use tenant ID...
    }
}
```

#### Current User

```csharp
using Idevs.Services;

public sealed class MyService
{
    private readonly ICurrentUser<Guid> _currentUser;

    public MyService(ICurrentUser<Guid> currentUser)
    {
        _currentUser = currentUser;
    }

    public void DoSomething()
    {
        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.Id;
            var userName = _currentUser.Name;
            var isAdmin = _currentUser.IsInRole("Admin");
            // ...
        }
    }
}
```

#### Unit of Work

```csharp
using Idevs.Services;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public async Task<Result> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = new User(command.Email, command.Name);
        await _userRepository.AddAsync(user);

        // Commit the transaction
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
```

#### Metrics

```csharp
using Idevs.Services;

public sealed class MyService
{
    private readonly IMetrics _metrics;

    public void DoSomething()
    {
        _metrics.IncrementCounter("user.created");
        _metrics.RecordGauge("users.active", 150);

        using (_metrics.StartTimer("operation.duration"))
        {
            // Timed operation
        }

        _metrics.ObserveHistogram("request.size", 1024);
    }
}
```

## Testing

### Unit Testing Commands

```csharp
using Xunit;
using Shouldly;

public sealed class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesUser()
    {
        // Arrange
        var handler = new CreateUserCommandHandler();
        var command = new CreateUserCommand("test@example.com", "Test User");

        // Act
        var result = await handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_InvalidEmail_ReturnsValidationError()
    {
        // Arrange
        var handler = new CreateUserCommandHandler();
        var command = new CreateUserCommand("", "Test User");

        // Act
        var result = await handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
    }
}
```

### Integration Testing

```csharp
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;

public sealed class CommandPipelineTests
{
    [Fact]
    public async Task CommandWithDecorators_ExecutesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdevs(options =>
        {
            options.Decorators.EnableLogging = true;
            options.Decorators.EnableValidation = true;
            options.Decorators.EnableMetrics = true;
        });
        services.AddCommandHandler<CreateUserCommand, CreateUserCommandHandler>();
        services.AddSingleton<IValidator<CreateUserCommand>, CreateUserCommandValidator>();

        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<CreateUserCommand>>();

        var command = new CreateUserCommand("test@example.com", "Test User");

        // Act
        var result = await handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
```

## Best Practices

### 1. Use Records for Commands and Queries

```csharp
// ✅ Good
public sealed record CreateUserCommand(string Email, string Name) : ICommand;

// ❌ Avoid
public class CreateUserCommand : ICommand
{
    public string Email { get; set; }
    public string Name { get; set; }
}
```

### 2. Keep Handlers Focused

```csharp
// ✅ Good - Single responsibility
public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> HandleAsync(CreateUserCommand command, ...)
    {
        // Only user creation logic
    }
}

// ❌ Avoid - Multiple responsibilities
public sealed class UserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> HandleAsync(CreateUserCommand command, ...)
    {
        // Handles create, update, delete all in one handler
    }
}
```

### 3. Use Specific Error Types

```csharp
// ✅ Good
return Result.Failure(Error.NotFound("USER_404", "User not found"));

// ❌ Avoid
return Result.Failure(Error.Failure("ERR_001", "Error"));
```

### 4. Validate Early

```csharp
// ✅ Good
public async Task<Result> HandleAsync(CreateUserCommand command, ...)
{
    if (string.IsNullOrWhiteSpace(command.Email))
    {
        return Result.Failure(Error.Validation("VAL_001", "Email is required"));
    }

    // Continue with business logic
}
```

### 5. Use Decorators for Cross-Cutting Concerns

```csharp
// ✅ Good - Enable decorators
services.AddIdevs(options =>
{
    options.Decorators.EnableLogging = true;
    options.Decorators.EnableValidation = true;
});

// ❌ Avoid - Manual logging in every handler
public async Task<Result> HandleAsync(CreateUserCommand command, ...)
{
    _logger.LogInformation("Executing command...");
    // Business logic
    _logger.LogInformation("Command executed");
}
```

## Architecture Decisions

See [decisions.md](./decisions.md) for detailed architecture decisions and rationale.

## Testing Strategy

See [testing-notes.md](./testing-notes.md) for testing guidelines and patterns.

## Future Enhancements

Phase 2 will add:
- Validation decorators for commands with responses and queries
- Additional decorator types (retry, circuit breaker, caching)
- Entity Framework Core integration
- Multi-tenancy support
- Authentication/authorization decorators
- Advanced metrics and observability

## Contributing

See the root repository guidelines for contribution standards and practices.

## License

See LICENSE file in the repository root.
