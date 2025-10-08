# Guide 1: Handler Base Classes

**Phase**: 3 - Application Layer  
**Component**: Handler Base Classes and Interfaces  
**Prerequisites**: Phase 2 Result Pattern (`Result<T>`)  
**Estimated Time**: 2-3 hours

---

## Table of Contents

1. [Overview](#overview)
2. [Design Rationale](#design-rationale)
3. [Core Interfaces](#core-interfaces)
4. [Implementation](#implementation)
5. [Usage Examples](#usage-examples)
6. [Unit Testing](#unit-testing)
7. [Best Practices](#best-practices)
8. [Common Pitfalls](#common-pitfalls)
9. [Next Steps](#next-steps)

---

## Overview

Handler base classes define the foundation of the Application Layer's command/query handling infrastructure. They provide:

- **Type-safe contracts** for commands and queries
- **Consistent return types** using `Result<T>` pattern
- **Clear semantics** distinguishing reads from writes
- **Extensibility points** for shared handler logic

### Key Components

| Interface | Purpose | Returns |
|-----------|---------|---------|
| `ICommand<TResponse>` | Marks mutation requests | `Result<TResponse>` |
| `IQuery<TResponse>` | Marks read requests | `Result<TResponse>` |
| `ICommandHandler<TCommand, TResponse>` | Processes commands | `Task<Result<TResponse>>` |
| `IQueryHandler<TQuery, TResponse>` | Processes queries | `Task<Result<TResponse>>` |

---

## Design Rationale

### Why Separate Commands from Queries?

**CQRS Principle**: Commands change state; queries don't. This separation enables:

1. **Different optimizations**: Queries can hit read replicas or caches
2. **Security policies**: Commands require write permissions, queries read-only
3. **Scalability**: Query handlers can scale independently
4. **Code clarity**: Intent is explicit from the interface

### Why Result<T> Return Type?

Returning `Result<T>` instead of throwing exceptions:

1. **Makes failures explicit** in the type signature
2. **Enables Railway-Oriented Programming** for composable error handling
3. **Avoids expensive exception unwinding** for expected failures
4. **Provides consistent error handling** across all handlers

### Why Marker Interfaces?

`ICommand<TResponse>` and `IQuery<TResponse>` are marker interfaces with no methods:

1. **Type discrimination**: MediatR can route to correct handler
2. **Compile-time safety**: Can't mix commands and queries
3. **Convention over configuration**: Clear naming pattern

---

## Core Interfaces

### 1. ICommand<TResponse>

Marker interface for commands that mutate state:

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Marker interface for commands that mutate application state.
/// </summary>
/// <typeparam name="TResponse">The type of the response value on success.</typeparam>
public interface ICommand<TResponse>
{
    // Marker interface - no methods
}
```

**Usage Pattern**:
```csharp
public sealed record CreateProductCommand(
    string Name,
    decimal Price
) : ICommand<Guid>;
```

---

### 2. IQuery<TResponse>

Marker interface for queries that only read data:

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Marker interface for queries that retrieve data without side effects.
/// </summary>
/// <typeparam name="TResponse">The type of the response value on success.</typeparam>
public interface IQuery<TResponse>
{
    // Marker interface - no methods
}
```

**Usage Pattern**:
```csharp
public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;
```

---

### 3. ICommandHandler<TCommand, TResponse>

Handles command execution:

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Defines a handler for a command that mutates state.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type on success.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the command asynchronously.
    /// </summary>
    /// <param name="command">The command instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<TResponse>> Handle(
        TCommand command,
        CancellationToken cancellationToken = default);
}
```

---

### 4. IQueryHandler<TQuery, TResponse>

Handles query execution:

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Defines a handler for a query that retrieves data.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type on success.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the query asynchronously.
    /// </summary>
    /// <param name="query">The query instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the queried data or an error.</returns>
    Task<Result<TResponse>> Handle(
        TQuery query,
        CancellationToken cancellationToken = default);
}
```

---

## Implementation

### Abstract Base Handler (Optional)

For shared logic across handlers, create an abstract base class:

```csharp
namespace Idevs.Application.Handlers;

/// <summary>
/// Base class for command handlers with common functionality.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class CommandHandlerBase<TCommand, TResponse>
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected readonly ILogger<CommandHandlerBase<TCommand, TResponse>> Logger;

    protected CommandHandlerBase(ILogger<CommandHandlerBase<TCommand, TResponse>> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TResponse>> Handle(
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Handling command {CommandType}",
                typeof(TCommand).Name);

            var result = await HandleCore(command, cancellationToken);

            if (result.IsSuccess)
            {
                Logger.LogInformation(
                    "Command {CommandType} handled successfully",
                    typeof(TCommand).Name);
            }
            else
            {
                Logger.LogWarning(
                    "Command {CommandType} failed: {Error}",
                    typeof(TCommand).Name,
                    result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Unexpected error handling command {CommandType}",
                typeof(TCommand).Name);

            return Result.Failure<TResponse>(
                Error.Failure(
                    "Command.UnexpectedError",
                    "An unexpected error occurred while processing the command."));
        }
    }

    /// <summary>
    /// Core handler logic to be implemented by derived classes.
    /// </summary>
    protected abstract Task<Result<TResponse>> HandleCore(
        TCommand command,
        CancellationToken cancellationToken);
}
```

**Rationale**: This base class provides logging and exception handling without polluting individual handlers.

---

### Abstract Query Handler Base

```csharp
namespace Idevs.Application.Handlers;

/// <summary>
/// Base class for query handlers with common functionality.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class QueryHandlerBase<TQuery, TResponse>
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    protected readonly ILogger<QueryHandlerBase<TQuery, TResponse>> Logger;

    protected QueryHandlerBase(ILogger<QueryHandlerBase<TQuery, TResponse>> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TResponse>> Handle(
        TQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug(
                "Handling query {QueryType}",
                typeof(TQuery).Name);

            var result = await HandleCore(query, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Unexpected error handling query {QueryType}",
                typeof(TQuery).Name);

            return Result.Failure<TResponse>(
                Error.Failure(
                    "Query.UnexpectedError",
                    "An unexpected error occurred while processing the query."));
        }
    }

    /// <summary>
    /// Core handler logic to be implemented by derived classes.
    /// </summary>
    protected abstract Task<Result<TResponse>> HandleCore(
        TQuery query,
        CancellationToken cancellationToken);
}
```

---

## Usage Examples

### Example 1: Simple Command Handler

```csharp
namespace Idevs.Application.Products.Commands.Create;

public sealed record CreateProductCommand(
    string Name,
    decimal Price,
    Guid CategoryId
) : ICommand<Guid>;

public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IRepository<Product> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Create domain aggregate
        var productResult = Product.Create(
            request.Name,
            Money.From(request.Price),
            new CategoryId(request.CategoryId));

        if (productResult.IsFailure)
            return Result.Failure<Guid>(productResult.Error);

        var product = productResult.Value;

        // 2. Persist
        await _repository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Return created ID
        return Result.Success(product.Id.Value);
    }
}
```

---

### Example 2: Query Handler with DTO Mapping

```csharp
namespace Idevs.Application.Products.Queries.GetById;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;

public sealed record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string CategoryName);

public sealed class GetProductByIdQueryHandler
    : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IReadRepository<Product> _repository;

    public GetProductByIdQueryHandler(IReadRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        var product = await _repository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
            return Result.Failure<ProductDto>(
                Error.NotFound("Product.NotFound", "Product not found."));

        var dto = new ProductDto(
            product.Id.Value,
            product.Name,
            product.Price.Amount,
            product.Category.Name);

        return Result.Success(dto);
    }
}
```

---

### Example 3: Command Handler Using Base Class

```csharp
namespace Idevs.Application.Products.Commands.UpdatePrice;

public sealed record UpdateProductPriceCommand(
    Guid ProductId,
    decimal NewPrice
) : ICommand<Unit>;

public sealed class UpdateProductPriceCommandHandler
    : CommandHandlerBase<UpdateProductPriceCommand, Unit>
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductPriceCommandHandler(
        IRepository<Product> repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductPriceCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<Result<Unit>> HandleCore(
        UpdateProductPriceCommand command,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(command.ProductId);

        var product = await _repository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<Unit>(
                Error.NotFound("Product.NotFound", "Product not found."));

        var priceResult = product.UpdatePrice(Money.From(command.NewPrice));
        if (priceResult.IsFailure)
            return Result.Failure<Unit>(priceResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}
```

**Note**: `Unit` is a type representing "no meaningful value" (like `void` in async methods).

---

### Example 4: Paginated Query Handler

```csharp
namespace Idevs.Application.Products.Queries.List;

public sealed record ListProductsQuery(
    int PageNumber,
    int PageSize,
    string? SearchTerm
) : IQuery<PagedResult<ProductListItemDto>>;

public sealed record ProductListItemDto(Guid Id, string Name, decimal Price);

public sealed class ListProductsQueryHandler
    : IQueryHandler<ListProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IReadRepository<Product> _repository;

    public ListProductsQueryHandler(IReadRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<ProductListItemDto>>> Handle(
        ListProductsQuery request,
        CancellationToken cancellationToken)
    {
        var specification = new ProductsWithSearchSpecification(request.SearchTerm);

        var products = await _repository.ListAsync(
            specification,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await _repository.CountAsync(specification, cancellationToken);

        var items = products.Select(p => new ProductListItemDto(
            p.Id.Value,
            p.Name,
            p.Price.Amount)).ToList();

        var pagedResult = new PagedResult<ProductListItemDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result.Success(pagedResult);
    }
}
```

---

## Unit Testing

### Test 1: Command Handler Success Case

```csharp
namespace Idevs.Application.Tests.Products.Commands;

public sealed class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithProductId()
    {
        // Arrange
        var repository = Substitute.For<IRepository<Product>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        repository
            .AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateProductCommandHandler(repository, unitOfWork);

        var command = new CreateProductCommand("Test Product", 99.99m, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        await repository.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Name == "Test Product"),
            Arg.Any<CancellationToken>());

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidPrice_ReturnsFailure()
    {
        // Arrange
        var repository = Substitute.For<IRepository<Product>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreateProductCommandHandler(repository, unitOfWork);

        var command = new CreateProductCommand("Test", -10m, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Money.InvalidAmount");

        await repository.DidNotReceive().AddAsync(
            Arg.Any<Product>(),
            Arg.Any<CancellationToken>());
    }
}
```

---

### Test 2: Query Handler Not Found Case

```csharp
namespace Idevs.Application.Tests.Products.Queries;

public sealed class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingProduct_ReturnsProductDto()
    {
        // Arrange
        var repository = Substitute.For<IReadRepository<Product>>();

        var productId = new ProductId(Guid.NewGuid());
        var product = Product.Create(
            "Test Product",
            Money.From(49.99m),
            new CategoryId(Guid.NewGuid())).Value;

        repository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        var handler = new GetProductByIdQueryHandler(repository);
        var query = new GetProductByIdQuery(productId.Value);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(productId.Value);
        result.Value.Name.ShouldBe("Test Product");
        result.Value.Price.ShouldBe(49.99m);
    }

    [Fact]
    public async Task Handle_NonExistingProduct_ReturnsNotFoundError()
    {
        // Arrange
        var repository = Substitute.For<IReadRepository<Product>>();

        repository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var handler = new GetProductByIdQueryHandler(repository);
        var query = new GetProductByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Product.NotFound");
    }
}
```

---

### Test 3: Base Handler Exception Handling

```csharp
public sealed class CommandHandlerBaseTests
{
    private sealed class TestCommand : ICommand<string>
    {
        public bool ShouldThrow { get; init; }
    }

    private sealed class TestCommandHandler : CommandHandlerBase<TestCommand, string>
    {
        public TestCommandHandler(ILogger<CommandHandlerBase<TestCommand, string>> logger)
            : base(logger)
        {
        }

        protected override Task<Result<string>> HandleCore(
            TestCommand command,
            CancellationToken cancellationToken)
        {
            if (command.ShouldThrow)
                throw new InvalidOperationException("Test exception");

            return Task.FromResult(Result.Success("Success"));
        }
    }

    [Fact]
    public async Task Handle_UnexpectedException_ReturnsFailureResult()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CommandHandlerBase<TestCommand, string>>>();
        var handler = new TestCommandHandler(logger);
        var command = new TestCommand { ShouldThrow = true };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Command.UnexpectedError");
        result.Error.Type.ShouldBe(ErrorType.Failure);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
```

---

## Best Practices

### ✅ DO

1. **Use records for commands/queries**: Immutability and value semantics are ideal
   ```csharp
   public sealed record CreateOrderCommand(...) : ICommand<Guid>;
   ```

2. **Keep handlers focused**: One handler per use case
   ```csharp
   // ✅ Good
   CreateProductCommandHandler
   UpdateProductCommandHandler

   // ❌ Bad
   ProductCommandHandler // Handles multiple commands
   ```

3. **Return Result<T>**: Always wrap return values in Result pattern
   ```csharp
   public async Task<Result<Guid>> Handle(...) // ✅
   public async Task<Guid> Handle(...) // ❌
   ```

4. **Validate in validators**: Don't mix validation with handler logic
   ```csharp
   // ✅ Use FluentValidation
   public sealed class CreateProductCommandValidator : AbstractValidator<...>

   // ❌ Don't validate in handler
   if (string.IsNullOrEmpty(command.Name)) throw ...
   ```

5. **Use CancellationToken**: Always pass it through
   ```csharp
   await _repository.GetByIdAsync(id, cancellationToken); // ✅
   await _repository.GetByIdAsync(id, CancellationToken.None); // ❌
   ```

---

### ❌ DON'T

1. **Don't throw exceptions for business failures**
   ```csharp
   // ❌ Bad
   if (product is null)
       throw new NotFoundException("Product not found");

   // ✅ Good
   if (product is null)
       return Result.Failure<ProductDto>(Error.NotFound(...));
   ```

2. **Don't put domain logic in handlers**
   ```csharp
   // ❌ Bad
   product.Price = new Money(command.NewPrice);

   // ✅ Good
   var result = product.UpdatePrice(Money.From(command.NewPrice));
   ```

3. **Don't return domain entities directly**
   ```csharp
   // ❌ Bad
   public sealed record GetProductQuery(...) : IQuery<Product>;

   // ✅ Good
   public sealed record GetProductQuery(...) : IQuery<ProductDto>;
   ```

4. **Don't mix commands and queries**
   ```csharp
   // ❌ Bad: Command returning data
   public sealed record CreateProductCommand(...) : ICommand<Product>;

   // ✅ Good: Command returns ID, separate query retrieves data
   public sealed record CreateProductCommand(...) : ICommand<Guid>;
   ```

5. **Don't reuse handlers**
   ```csharp
   // ❌ Bad
   public sealed class ProductHandler : 
       ICommandHandler<CreateProductCommand, Guid>,
       ICommandHandler<UpdateProductCommand, Unit>

   // ✅ Good: One handler per command
   public sealed class CreateProductCommandHandler : ...
   public sealed class UpdateProductCommandHandler : ...
   ```

---

## Common Pitfalls

### Pitfall 1: Leaking Infrastructure Details

**Problem**: Handler depends on EF Core `DbContext` directly.

```csharp
// ❌ Bad
public sealed class CreateProductCommandHandler
{
    private readonly ApplicationDbContext _context; // EF Core leak

    public async Task<Result<Guid>> Handle(...)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }
}
```

**Solution**: Use repository abstraction.

```csharp
// ✅ Good
public sealed class CreateProductCommandHandler
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(...)
    {
        await _repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

---

### Pitfall 2: Anemic Handlers

**Problem**: Handler just passes data to repository without domain logic.

```csharp
// ❌ Bad
public async Task<Result<Guid>> Handle(...)
{
    var product = new Product
    {
        Name = command.Name,
        Price = command.Price
    };

    await _repository.AddAsync(product);
}
```

**Solution**: Invoke domain factory methods.

```csharp
// ✅ Good
public async Task<Result<Guid>> Handle(...)
{
    var result = Product.Create(
        command.Name,
        Money.From(command.Price));

    if (result.IsFailure)
        return Result.Failure<Guid>(result.Error);

    await _repository.AddAsync(result.Value);
}
```

---

### Pitfall 3: Not Handling Cancellation

**Problem**: Ignoring `CancellationToken` leads to resource leaks.

```csharp
// ❌ Bad
public async Task<Result<Guid>> Handle(
    CreateProductCommand command,
    CancellationToken cancellationToken)
{
    await _repository.AddAsync(product); // Missing token
}
```

**Solution**: Always pass token through.

```csharp
// ✅ Good
await _repository.AddAsync(product, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

---

### Pitfall 4: Inconsistent Error Handling

**Problem**: Mixing exceptions and Result<T>.

```csharp
// ❌ Bad
public async Task<Result<Guid>> Handle(...)
{
    if (product is null)
        throw new NotFoundException(); // Inconsistent

    return Result.Success(product.Id);
}
```

**Solution**: Always return Result<T>.

```csharp
// ✅ Good
if (product is null)
    return Result.Failure<Guid>(Error.NotFound(...));
```

---

## Next Steps

1. **Read Guide 2**: MediatR Integration (`02-MEDIATR-INTEGRATION.md`)
2. **Implement Interfaces**: Create `ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`
3. **Write Sample Handlers**: Create 1-2 command and query handlers
4. **Add Unit Tests**: Test success and failure paths
5. **Review Result Pattern**: Ensure Phase 2 `Result<T>` is implemented

---

**Summary**: Handler base classes provide type-safe, Result-oriented contracts for commands and queries. They enable clear separation of concerns, consistent error handling, and easy testing. Next, we'll wire these handlers into MediatR for automatic routing and pipeline execution.

---

**Implementation Checklist**:

- [ ] Create `ICommand<TResponse>` marker interface
- [ ] Create `IQuery<TResponse>` marker interface
- [ ] Create `ICommandHandler<TCommand, TResponse>` interface
- [ ] Create `IQueryHandler<TQuery, TResponse>` interface
- [ ] (Optional) Create `CommandHandlerBase<TCommand, TResponse>` abstract class
- [ ] (Optional) Create `QueryHandlerBase<TQuery, TResponse>` abstract class
- [ ] Write sample command handler with tests
- [ ] Write sample query handler with tests
- [ ] Document naming conventions in team wiki

---

**Last Updated**: 2025-01-08  
**Next Guide**: 02-MEDIATR-INTEGRATION.md
