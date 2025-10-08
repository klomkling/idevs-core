# 04: CQRS Contracts - Part 2

> **Navigation:** [Index](README.md) • [Part 1](04-cqrs-contracts-basic.md) • [Part 2](04-cqrs-contracts-advanced.md)

using Xunit;

namespace Idevs.Tests.CQRS;

public class CqrsContractsTests
{
    [Fact]
    public void Unit_AllInstancesEqual()
    {
        var unit1 = Unit.Value;
        var unit2 = new Unit();

        unit1.ShouldBe(unit2);
        (unit1 == unit2).ShouldBeTrue();
    }

    [Fact]
    public async Task CommandHandler_ReturnsResult()
    {
        var handler = new CreateCustomerCommandHandler();
        var command = new CreateCustomerCommand("John Doe", "john@example.com");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CommandHandler_ValidatesInput()
    {
        var handler = new CreateCustomerCommandHandler();
        var command = new CreateCustomerCommand("", "john@example.com");

        var result = await handler.HandleAsync(command);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("Validation");
    }

    [Fact]
    public async Task QueryHandler_ReturnsData()
    {
        var handler = new GetCustomerByIdQueryHandler();
        var query = new GetCustomerByIdQuery(Guid.NewGuid());

        var result = await handler.HandleAsync(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("John Doe");
    }

    [Fact]
    public async Task QueryHandler_HandlesNotFound()
    {
        var handler = new GetCustomerByIdQueryHandler();
        var query = new GetCustomerByIdQuery(Guid.Empty);

        var result = await handler.HandleAsync(query);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotFound");
    }

    [Fact]
    public void Command_ImplementsICommand()
    {
        var command = new CreateCustomerCommand("Test", "test@example.com");

        (command is ICommand).ShouldBeTrue();
        (command is ICommand<Guid>).ShouldBeTrue();
    }

    [Fact]
    public void Query_ImplementsIQuery()
    {
        var query = new GetCustomerByIdQuery(Guid.NewGuid());

        (query is IQuery).ShouldBeTrue();
        (query is IQuery<CustomerDto>).ShouldBeTrue();
    }
}

```

### 10. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~CqrsContractsTests"
```

Expected: ✅ All 7 tests passing

### 11. Document Handler Registration

**File:** `src/Idevs/CQRS/README.md`

```markdown
# CQRS Contracts

## Handler Registration (DI)

### Manual Registration
```csharp
services.AddScoped<ICommandHandler<CreateCustomerCommand, Guid>, CreateCustomerCommandHandler>();
services.AddScoped<IQueryHandler<GetCustomerByIdQuery, CustomerDto>, GetCustomerByIdQueryHandler>();
```

### Assembly Scanning (Phase 3)

```csharp
// Register all handlers in assembly
services.AddHandlers(typeof(CreateCustomerCommand).Assembly);
```

## Command Pattern

### Command Definition

```csharp
public record CreateOrderCommand(
    Guid CustomerId,
    List<OrderItemDto> Items) : ICommand<Guid>;
```

### Handler Implementation

```csharp
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate
        if (command.Items.Count == 0)
            return DomainErrors.Validation.Required(nameof(command.Items));

        // 2. Create aggregate
        var order = Order.Create(command.CustomerId, command.Items);

        // 3. Persist
        await _repository.AddAsync(order, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 4. Return result
        return order.Id;
    }
}
```

## Query Pattern

### Query Definition

```csharp
public record GetOrdersByCustomerQuery(
    Guid CustomerId,
    int Page = 1,
    int PageSize = 10) : IQuery<PagedResult<OrderDto>>;
```

### Handler Implementation

```csharp
public class GetOrdersByCustomerQueryHandler 
    : IQueryHandler<GetOrdersByCustomerQuery, PagedResult<OrderDto>>
{
    private readonly IOrderReadService _readService;

    public GetOrdersByCustomerQueryHandler(IOrderReadService readService)
    {
        _readService = readService;
    }

    public async Task<Result<PagedResult<OrderDto>>> HandleAsync(
        GetOrdersByCustomerQuery query,
        CancellationToken cancellationToken = default)
    {
        var orders = await _readService.GetOrdersByCustomerAsync(
            query.CustomerId,
            query.Page,
            query.PageSize,
            cancellationToken);

        return PagedResult<OrderDto>.Success(
            orders.Items,
            query.Page,
            query.PageSize,
            orders.TotalCount);
    }
}
```

## Decorator Pattern (Phase 3)

Handlers can be wrapped with decorators for:

- **Validation:** FluentValidation integration
- **Logging:** Log command/query execution
- **Caching:** Cache query results
- **Transactions:** Automatic commit/rollback
- **Authorization:** Check permissions

Example decorator:

```csharp
public class LoggingCommandHandlerDecorator<TCommand, TResponse> 
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ICommandHandler<TCommand, TResponse> _inner;
    private readonly ILogger<TCommand> _logger;

    public async Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing {Command}", typeof(TCommand).Name);
        
        var result = await _inner.HandleAsync(command, cancellationToken);
        
        if (result.IsSuccess)
            _logger.LogInformation("Command {Command} succeeded", typeof(TCommand).Name);
        else
            _logger.LogWarning("Command {Command} failed: {Error}", 
                typeof(TCommand).Name, result.Error);
        
        return result;
    }
}
```

```

## Verification Checklist

- [ ] Unit type defined (void replacement)
- [ ] ICommand and ICommand<TResponse> interfaces
- [ ] IQuery<TResponse> interface
- [ ] ICommandHandler and IQueryHandler interfaces
- [ ] Example command/handler pair
- [ ] Example query/handler pair
- [ ] All handlers return `Task<Result<T>>`
- [ ] ≥7 unit tests covering handlers
- [ ] README with usage patterns and DI registration

## Common Pitfalls

### ❌ Commands Returning Too Much Data
```csharp
// BAD - command returns full entity
public record CreateCustomerCommand(...) : ICommand<Customer>;

// GOOD - command returns ID only
public record CreateCustomerCommand(...) : ICommand<Guid>;
```

### ❌ Queries with Side Effects

```csharp
// BAD - query modifies state
public class GetCustomerQueryHandler : IQueryHandler<GetCustomerQuery, CustomerDto>
{
    public async Task<Result<CustomerDto>> HandleAsync(...)
    {
        customer.LastAccessedAt = DateTime.UtcNow; // Side effect!
        await _repo.SaveAsync(customer);
        return dto;
    }
}

// GOOD - query is read-only
public class GetCustomerQueryHandler : IQueryHandler<GetCustomerQuery, CustomerDto>
{
    public async Task<Result<CustomerDto>> HandleAsync(...)
    {
        var customer = await _repo.GetByIdAsync(query.CustomerId);
        return MapToDto(customer);
    }
}
```

### ❌ Not Using CancellationToken

```csharp
// BAD - ignores cancellation
public async Task<Result<CustomerDto>> HandleAsync(
    GetCustomerQuery query,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Customers.ToListAsync(); // Missing token!
}

// GOOD - propagates cancellation
public async Task<Result<CustomerDto>> HandleAsync(
    GetCustomerQuery query,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Customers.ToListAsync(cancellationToken);
}
```

## Next Steps

- **[05-aggregates-entities.md](05-aggregates-entities.md)** - Entity base classes
- **[06-tenant-user-context.md](06-tenant-user-context.md)** - Tenant/user abstractions

## References

- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Mediator Pattern](https://refactoring.guru/design-patterns/mediator)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)

---

**[← Back to Phase 2](../phase-2-domain-NEW.md)** | **[Next: Aggregates & Entities →](05-aggregates-entities.md)**

---

**[← Part 1](04-cqrs-contracts-basic.md)** | **[Back to Phase 2](../phase-2-domain.md)**
