# Phase 2: Domain & Contracts

**Phase Owner**: Domain Architecture Team  
**Last Updated**: 2025-10-04  
**Status**: Planning  
**Dependencies**: Phase 0 (Discovery & Guardrails), Phase 1 (Platform Scaffolding)

---

## Purpose

Phase 2 establishes the **domain foundation** for the **Idevs** framework (repo: `idevs-core`) by defining core contracts, base abstractions, and patterns for entities, value objects, aggregates, CQRS handlers, repositories, and domain events. This phase ensures a solid, testable, multi-tenant-aware domain layer before any infrastructure implementation begins.

**Key Principle**: *"Guard invariants first, implement once"* — establish domain contracts and validation early to prevent architectural drift.

---

## Objectives

### Primary Objectives

1. **Define Core Entity Interfaces**
   - Document base entity contracts (IEntity, IAuditableEntity, ISoftDeletableEntity, ITenantEntity)
   - Establish tenant isolation at the domain level
   - Define audit trail requirements per ADR-0002
   - Implement soft-delete semantics per ADR-0003

2. **Establish Result Patterns**
   - Define Result<T> for error handling without exceptions
   - Create PagedResult<T> for query pagination
   - Document ValidationResult for input validation
   - Provide error code and message patterns

3. **Design CQRS Contracts**
   - Define ICommand and IQuery marker interfaces
   - Create ICommandHandler<TCommand> and IQueryHandler<TQuery, TResult>
   - Document handler registration patterns (explicit, no reflection)
   - Establish decorator pattern for cross-cutting concerns

4. **Implement DDD Patterns**
   - Create base classes for Entities, Value Objects, and Aggregate Roots
   - Define Repository pattern interfaces
   - Establish Unit of Work pattern
   - Document aggregate boundaries and transactional consistency

---

## Key Activities

### 1. Core Entity Interfaces

**Activity**: Define base contracts for all domain entities

#### Entity Base Interfaces

**IEntity<TKey>** - Base interface for all entities

```csharp
namespace Idevs.Domain.Abstractions;

public interface IEntity<TKey> where TKey : notnull
{
    TKey Id { get; }
}
```text

**IAuditableEntity<TUserKey>** - Entity with audit trail (per ADR-0002)

```csharp
namespace Idevs.Domain.Abstractions;

public interface IAuditableEntity<TUserKey> where TUserKey : IEquatable<TUserKey>
{
    DateTime CreatedAt { get; }
    TUserKey CreatedBy { get; }
    DateTime? UpdatedAt { get; set; }
    TUserKey? UpdatedBy { get; set; }
}

// Convenience interface for Guid-based user IDs (most common)
public interface IAuditableEntity : IAuditableEntity<Guid> { }
```text

**ISoftDeletableEntity<TUserKey>** - Entity with soft-delete support (per ADR-0003)

```csharp
namespace Idevs.Domain.Abstractions;

public interface ISoftDeletableEntity<TUserKey> where TUserKey : IEquatable<TUserKey>
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    TUserKey? DeletedBy { get; set; }
}

// Convenience interface for Guid-based user IDs (most common)
public interface ISoftDeletableEntity : ISoftDeletableEntity<Guid> { }
```text

**ITenantEntity<TTenantKey>** - Entity belonging to a tenant (per ADR-0001)

```csharp
namespace Idevs.Domain.Abstractions;

public interface ITenantEntity<TTenantKey> where TTenantKey : IEquatable<TTenantKey>
{
    TTenantKey TenantId { get; }
}

// Convenience interface for Guid-based tenancy (most common)
public interface ITenantEntity : ITenantEntity<Guid> { }
```text

**Design Decisions**:

- ✅ **Generic User Keys**: `TUserKey` supports Guid, int, string, etc. per ADR-0003
- ✅ **Generic Tenant Keys**: `TTenantKey` supports Guid (most common) or other types
- ✅ **Convenience Interfaces**: Non-generic versions default to `Guid` for simplicity
- ✅ Audit fields use `DateTime` (will map to `timestamptz` in PostgreSQL)
- ✅ Created fields are required; Updated/Deleted are nullable
- ✅ Interfaces are composable (entity can implement multiple)
- ✅ No base classes yet - keep domain persistence-agnostic

---

### 2. Value Objects

**Activity**: Create immutable value object base class with structural equality

#### ValueObject Base Class

```csharp
namespace Idevs.Domain.Primitives;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(default(HashCode), (hash, obj) =>
            {
                hash.Add(obj);
                return hash;
            })
            .ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
```text

#### Example Value Objects

**Money** - Amount with currency

```csharp
namespace Idevs.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money>.Failure("Currency code is required");

        if (currency.Length != 3)
            return Result<Money>.Failure("Currency code must be 3 characters (ISO 4217)");

        return Result<Money>.Success(new Money(amount, currency.ToUpperInvariant()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
```text

**Email** - Validated email address

```csharp
namespace Idevs.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("Email is required");

        // Simple validation - use regex or library for production
        if (!value.Contains('@') || !value.Contains('.'))
            return Result<Email>.Failure("Invalid email format");

        return Result<Email>.Success(new Email(value.ToLowerInvariant()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
```text

**Design Principles**:

- ✅ **Immutability**: All properties are `init` or have private setters
- ✅ **Factory Methods**: Use static `Create()` returning `Result<T>` (no exceptions)
- ✅ **Validation**: Guard clauses in factory method
- ✅ **Equality**: Structural equality via `GetEqualityComponents()`
- ✅ **No Reflection**: Explicit property definitions

---

### 3. Result Patterns

**Activity**: Define result types for error handling without exceptions

#### Result (Non-Generic)

```csharp
namespace Idevs.Domain.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string[] Errors { get; }

    protected Result(bool isSuccess, string[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? [];
    }

    public static Result Success() => new(true, []);
    
    public static Result Failure(params string[] errors) => new(false, errors);
    
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToArray());
}
```text

#### Result<T> (Generic)

```csharp
namespace Idevs.Domain.Results;

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string[] errors) : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, []);
    
    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);
    
    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.ToArray());

    // Implicit conversion from T to Result<T>
    public static implicit operator Result<T>(T value) => Success(value);
}
```text

#### PagedResult<T>

```csharp
namespace Idevs.Domain.Results;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items.ToList().AsReadOnly();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static PagedResult<T> Empty(int pageNumber, int pageSize) 
        => new([], 0, pageNumber, pageSize);
}
```text

**Usage Pattern**:

```csharp
// In a handler
public async Task<Result<Order>> HandleAsync(CreateOrderCommand command)
{
    var customerEmail = Email.Create(command.Email);
    if (customerEmail.IsFailure)
        return Result<Order>.Failure(customerEmail.Errors);

    var total = Money.Create(command.TotalAmount, command.Currency);
    if (total.IsFailure)
        return Result<Order>.Failure(total.Errors);

    var order = Order.Create(customerEmail.Value, total.Value);
    await _repository.AddAsync(order);
    
    return Result<Order>.Success(order);
}
```text

---

### 4. CQRS Contracts

**Activity**: Define command and query interfaces with handlers

#### Command Contracts

```csharp
namespace Idevs.Domain.Cqrs;

// Marker interface for commands (no return value)
public interface ICommand { }

// Marker interface for commands with return value
public interface ICommand<TResponse> { }

// Handler for commands without return value
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

// Handler for commands with return value
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```text

#### Query Contracts

```csharp
namespace Idevs.Domain.Cqrs;

// Marker interface for queries
public interface IQuery<TResponse> { }

// Handler for queries
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```text

#### Example Command & Handler

```csharp
namespace MyApp.Orders.Commands;

public sealed record CreateOrderCommand(
    string CustomerEmail,
    decimal TotalAmount,
    string Currency) : ICommand<Guid>;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CreateOrderHandler(IOrderRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateOrderCommand command, 
        CancellationToken cancellationToken = default)
    {
        var email = Email.Create(command.CustomerEmail);
        if (email.IsFailure)
            return Result<Guid>.Failure(email.Errors);

        var total = Money.Create(command.TotalAmount, command.Currency);
        if (total.IsFailure)
            return Result<Guid>.Failure(total.Errors);

        var order = Order.Create(
            _tenantContext.TenantId,
            email.Value,
            total.Value);

        await _repository.AddAsync(order, cancellationToken);
        
        return Result<Guid>.Success(order.Id);
    }
}
```text

**Registration Pattern** (per ADR-0005 - no reflection):

```csharp
// In DI configuration
services.AddScoped<ICommandHandler<CreateOrderCommand, Guid>, CreateOrderHandler>();

// Or using extension method
services.AddCommandHandler<CreateOrderCommand, Guid, CreateOrderHandler>();
```text

---

### 5. Aggregate Roots & Entities

**Activity**: Create base classes for aggregates and entities with domain event support

#### Entity Base Class

```csharp
namespace Idevs.Domain.Primitives;

public abstract class Entity<TKey> : IEntity<TKey>, IEquatable<Entity<TKey>> 
    where TKey : notnull
{
    public TKey Id { get; protected init; } = default!;

    protected Entity() { }

    protected Entity(TKey id)
    {
        Id = id;
    }

    public bool Equals(Entity<TKey>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Entity<TKey>);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right) => !(left == right);
}
```text

#### Aggregate Root Base Class

```csharp
namespace Idevs.Domain.Primitives;

public abstract class AggregateRoot<TKey> : Entity<TKey> where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(TKey id) : base(id) { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```text

#### Example: Order Aggregate Root

```csharp
namespace MyApp.Orders;

public sealed class Order : AggregateRoot<Guid>, 
    ITenantEntity,           // Uses Guid for TenantId (convenience interface)
    IAuditableEntity,        // Uses Guid for user IDs (convenience interface)
    ISoftDeletableEntity     // Uses Guid for DeletedBy (convenience interface)
{
    // Tenant isolation (from ITenantEntity)
    public Guid TenantId { get; private init; }

    // Domain properties
    public Email CustomerEmail { get; private set; } = null!;
    public Money Total { get; private set; } = null!;
    public OrderStatus Status { get; private set; }

    // Audit properties (from IAuditableEntity)
    public DateTime CreatedAt { get; private init; }
    public Guid CreatedBy { get; private init; }      // Guid user ID
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }      // Guid user ID

    // Soft delete properties (from ISoftDeletableEntity)
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }      // Guid user ID

    // Private constructor for EF Core
    private Order() { }

    // Factory method
    public static Order Create(Guid tenantId, Email customerEmail, Money total, Guid createdBy)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerEmail = customerEmail,
            Total = total,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy  // Guid from ICurrentUser or ITenantContext
        };

        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, tenantId, customerEmail));
        return order;
    }

    // Business logic methods
    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure("Order can only be confirmed when pending");

        if (IsDeleted)
            return Result.Failure("Cannot confirm deleted order");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new OrderConfirmedEvent(Id, TenantId));
        return Result.Success();
    }

    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;  // Guid user ID
        
        RaiseDomainEvent(new OrderDeletedEvent(Id, TenantId));
    }
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
```text

**Design Principles**:

- ✅ **Invariant Protection**: All state changes through public methods
- ✅ **Tenant Isolation**: `TenantId` set at creation, immutable
- ✅ **Domain Events**: Raised for significant state changes
- ✅ **Validation**: Business rule checks in methods, return `Result`
- ✅ **Factory Methods**: Static `Create()` ensures valid initial state

---

### 6. Tenant & User Context

**Activity**: Define context interfaces for tenant and user information

#### ITenantContext Interface

```csharp
namespace Idevs.Domain.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantName { get; }
    Guid UserId { get; }        // Current user ID for audit
    string UserName { get; }    // Current username for display
}
```text

#### ICurrentUser Interface (Alternative)

```csharp
namespace Idevs.Domain.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    string UserName { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
}
```text

**Design Note**:

- `ITenantContext` is **request-scoped** and provides both tenant and user information
- Implemented in infrastructure layer (Phase 5) from HTTP context, JWT claims, or session
- Used by handlers and aggregates for audit trail population
- Both `TenantId` and `UserId` are required for all operations

**Usage in Handler**:

```csharp
public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly ITenantContext _tenantContext;

    public async Task<Result<Guid>> HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        // Use context for tenant isolation and audit
        var order = Order.Create(
            tenantId: _tenantContext.TenantId,
            customerEmail: emailResult.Value,
            total: totalResult.Value,
            createdBy: _tenantContext.UserId);  // Audit: who created
            
        await _repository.AddAsync(order, ct);
        return Result<Guid>.Success(order.Id);
    }
}
```text

---

### 7. Repository & Unit of Work Patterns

**Activity**: Define repository abstractions for aggregate persistence

#### Repository Interfaces

```csharp
namespace Idevs.Domain.Repositories;

public interface IRepository<TEntity, TKey> 
    where TEntity : AggregateRoot<TKey> 
    where TKey : notnull
{
    // Query operations
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    // Write operations
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);

    // Specification pattern support
    Task<TEntity?> FindAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAllAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<PagedResult<TEntity>> FindPagedAsync(ISpecification<TEntity> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
```text

**Note**: Repositories are **scoped to tenant** - all queries automatically filter by `TenantId`.

#### Unit of Work Interface

```csharp
namespace Idevs.Domain.Repositories;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<Result> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```text

**Usage Pattern**:

```csharp
public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;  // Provides TenantId and UserId

    public async Task<Result<Guid>> HandleAsync(
        CreateOrderCommand command, 
        CancellationToken cancellationToken)
    {
        // Validate and create aggregate
        var email = Email.Create(command.CustomerEmail);
        if (email.IsFailure) return Result<Guid>.Failure(email.Errors);

        var total = Money.Create(command.TotalAmount, command.Currency);
        if (total.IsFailure) return Result<Guid>.Failure(total.Errors);

        var order = Order.Create(
            _tenantContext.TenantId, 
            email.Value, 
            total.Value,
            _tenantContext.UserId);  // Pass current user ID for audit

        // Persist aggregate
        await _orderRepository.AddAsync(order, cancellationToken);

        // Commit transaction (domain events published here)
        var result = await _unitOfWork.CommitAsync(cancellationToken);
        
        return result.IsSuccess 
            ? Result<Guid>.Success(order.Id) 
            : Result<Guid>.Failure(result.Errors);
    }
}
```text

---

### 8. Domain Events

**Activity**: Define domain event infrastructure for inter-aggregate communication

#### Domain Event Interface

```csharp
namespace Idevs.Domain.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    Guid TenantId { get; }
}
```text

#### Base Domain Event

```csharp
namespace Idevs.Domain.Events;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public required Guid TenantId { get; init; }
}
```text

#### Example Domain Events

```csharp
namespace MyApp.Orders.Events;

public sealed record OrderCreatedEvent(
    Guid OrderId,
    Guid TenantId,
    Email CustomerEmail) : DomainEvent
{
    public Guid OrderId { get; } = OrderId;
    public Email CustomerEmail { get; } = CustomerEmail;
}

public sealed record OrderConfirmedEvent(
    Guid OrderId,
    Guid TenantId) : DomainEvent
{
    public Guid OrderId { get; } = OrderId;
}

public sealed record OrderDeletedEvent(
    Guid OrderId,
    Guid TenantId) : DomainEvent
{
    public Guid OrderId { get; } = OrderId;
}
```text

#### Domain Event Publisher (Interface)

```csharp
namespace Idevs.Domain.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
```text

**Event Publishing Strategy**:

1. **Deferred Dispatch**: Events are collected on aggregates but NOT published immediately
2. **Unit of Work Integration**: Events published after successful `SaveChangesAsync()`
3. **Transactional Consistency**: If save fails, events are not published
4. **Implementation**: Actual publisher implemented in Infrastructure layer (Phase 5)

**Event Flow**:

```text
1. Command → Handler creates/modifies Aggregate
2. Aggregate raises domain events (stored in memory)
3. Repository saves aggregate
4. UnitOfWork.CommitAsync() called
   ├─ EF Core saves changes
   ├─ If successful, publish domain events
   └─ Clear events from aggregate
5. Event handlers react asynchronously
```text

---

### 9. Specification Pattern

**Activity**: Define composable query specifications

#### Specification Interface

```csharp
namespace Idevs.Domain.Specifications;

public interface ISpecification<T>
{
    // Core criteria
    Expression<Func<T, bool>>? Criteria { get; }

    // Includes (for eager loading)
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }

    // Ordering
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }

    // Paging
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
}
```text

#### Base Specification Class

```csharp
namespace Idevs.Domain.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
```text

#### Example Specifications

```csharp
namespace MyApp.Orders.Specifications;

public sealed class OrdersByCustomerEmailSpec : Specification<Order>
{
    public OrdersByCustomerEmailSpec(Email customerEmail)
    {
        AddCriteria(o => o.CustomerEmail == customerEmail);
        ApplyOrderByDescending(o => o.CreatedAt);
    }
}

public sealed class OrdersByStatusSpec : Specification<Order>
{
    public OrdersByStatusSpec(OrderStatus status)
    {
        AddCriteria(o => o.Status == status);
        ApplyOrderBy(o => o.CreatedAt);
    }
}

public sealed class OrdersPagedSpec : Specification<Order>
{
    public OrdersPagedSpec(int pageNumber, int pageSize)
    {
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        ApplyOrderByDescending(o => o.CreatedAt);
    }
}
```text

#### Specification Combinators

```csharp
namespace Idevs.Domain.Specifications;

public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        var spec = new CompositeSpecification<T>();
        
        if (left.Criteria != null && right.Criteria != null)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceParameterVisitor(left.Criteria.Parameters[0], parameter);
            var rightVisitor = new ReplaceParameterVisitor(right.Criteria.Parameters[0], parameter);
            
            var leftBody = leftVisitor.Visit(left.Criteria.Body);
            var rightBody = rightVisitor.Visit(right.Criteria.Body);
            
            var combined = Expression.AndAlso(leftBody, rightBody);
            spec.AddCriteria(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }
        
        return spec;
    }
}

internal class ReplaceParameterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}
```text

**Usage Pattern**:

```csharp
// Simple specification
var spec = new OrdersByStatusSpec(OrderStatus.Pending);
var orders = await _orderRepository.FindAllAsync(spec);

// Combined specification
var statusSpec = new OrdersByStatusSpec(OrderStatus.Confirmed);
var emailSpec = new OrdersByCustomerEmailSpec(customerEmail);
var combined = statusSpec.And(emailSpec);
var filteredOrders = await _orderRepository.FindAllAsync(combined);

// Paged specification
var pagedSpec = new OrdersPagedSpec(pageNumber: 1, pageSize: 20);
var paged = await _orderRepository.FindPagedAsync(pagedSpec, 1, 20);
```text

---

## Deliverables

| Deliverable | Status | Owner | Notes |
|-------------|--------|-------|-------|
| Entity Interfaces (IEntity, IAuditableEntity, ISoftDeletableEntity, ITenantEntity) | 🔲 Pending | Domain Team | Core contracts |
| ValueObject base class | 🔲 Pending | Domain Team | With structural equality |
| Example Value Objects (Money, Email) | 🔲 Pending | Domain Team | Production-ready examples |
| Result<T> family | 🔲 Pending | Domain Team | Non-exception error handling |
| PagedResult<T> | 🔲 Pending | Domain Team | Query pagination support |
| CQRS Interfaces (ICommand, IQuery, Handlers) | 🔲 Pending | Domain Team | Handler contracts |
| Entity<TKey> base class | 🔲 Pending | Domain Team | Identity-based equality |
| AggregateRoot<TKey> base class | 🔲 Pending | Domain Team | With domain events |
| Repository Interfaces | 🔲 Pending | Domain Team | IRepository<TEntity, TKey> |
| Unit of Work Interface | 🔲 Pending | Domain Team | IUnitOfWork |
| Domain Event Interfaces | 🔲 Pending | Domain Team | IDomainEvent, IDomainEventPublisher |
| Specification Pattern | 🔲 Pending | Domain Team | ISpecification<T> |
| Unit Tests for all abstractions | 🔲 Pending | Domain Team | ≥80% coverage |
| Code examples documentation | 🔲 Pending | Domain Team | Appendix sections |

---

## Success Metrics

### Quantitative Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Test Coverage** | ≥80% branch coverage | Coverlet report for domain project |
| **Code Examples** | 100% compilable | Sample project builds successfully |
| **Reflection Usage** | Zero instances | Static analysis via Roslyn analyzer |
| **Documentation Links** | Zero broken links | markdownlint + link checker |
| **ADR Alignment** | 100% references validated | Manual review against ADR-0001, 0002, 0003, 0005 |
| **Multi-Tenant Safety** | All entities tenant-aware | ITenantEntity on all aggregates |

### Qualitative Metrics

- **Clarity**: Can a developer understand the domain contracts in <30 minutes?
- **Consistency**: Do all patterns follow the same style (factory methods, Result returns)?
- **Multi-Tenancy**: Is tenant isolation enforced at every level?
- **Testability**: Can all domain logic be unit tested without infrastructure dependencies?
- **Modern C#**: Are C# 12 features used appropriately (records, primary constructors, collection expressions)?

---

## Risks & Mitigations

### Risk 1: Reflection Leakage

**Impact**: High  
**Probability**: Medium  
**Symptom**: Performance degradation, startup time issues  
**Mitigation**:

- Enforce "no reflection" rule via custom Roslyn analyzer
- Code review checklist includes reflection check
- Use explicit handler registration pattern per ADR-0005
- Document source generator approach for future optimizations

### Risk 2: Cross-Tenant Data Leakage

**Impact**: Critical  
**Probability**: Low  
**Symptom**: Users see other tenants' data  
**Mitigation**:

- `TenantId` required on all aggregates via `ITenantEntity`
- Repository queries auto-filter by tenant (enforced in infrastructure)
- Canary tests in CI to detect tenant filter bypass
- Architecture tests validate all aggregates implement `ITenantEntity`

### Risk 3: Over-Coupling to PostgreSQL

**Impact**: Medium  
**Probability**: Low  
**Symptom**: Cannot switch database providers  
**Mitigation**:

- Domain layer is persistence-agnostic (only interfaces)
- PostgreSQL-specific guidance in comments, not contracts
- EF Core mapping done in infrastructure layer (Phase 5)
- Value objects and entities use standard .NET types

### Risk 4: Exception-Based Control Flow

**Impact**: Medium  
**Probability**: Medium  
**Symptom**: Performance issues, unclear error handling  
**Mitigation**:

- Enforce `Result<T>` pattern for all operations
- Factory methods return `Result<T>`, never throw
- Architecture tests validate no exceptions in domain logic
- Code review guideline: "No throw in domain layer"

### Risk 5: Specification-to-EF Translation Mismatch

**Impact**: Medium  
**Probability**: Medium  
**Symptom**: Specifications work in memory but fail with real database  
**Mitigation**:

- Expression tree validation tests
- Integration tests with EF Core InMemory provider
- E2E tests with PostgreSQL in Phase 5
- Document limitations of Expression<Func<T, bool>>

### Risk 6: Inconsistent Domain Patterns

**Impact**: Low  
**Probability**: Medium  
**Symptom**: Confusion, refactoring needed  
**Mitigation**:

- Comprehensive examples in this document
- Coding standards enforced via EditorConfig
- Architecture review for each aggregate
- Pair programming for initial implementations

---

## Exit Criteria

Phase 2 is **complete** when:

### Must Have (Blocking)

- [ ] **Entity Interfaces**: IEntity, IAuditableEntity, ISoftDeletableEntity, ITenantEntity defined
- [ ] **Value Object Base**: Immutable base class with structural equality
- [ ] **Result Pattern**: Result, Result<T>, PagedResult<T> implemented
- [ ] **CQRS Contracts**: ICommand, IQuery, Handler interfaces defined
- [ ] **Aggregate Base**: Entity<TKey> and AggregateRoot<TKey> with domain events
- [ ] **Repository Interfaces**: IRepository<TEntity, TKey> and IUnitOfWork
- [ ] **Domain Events**: IDomainEvent, IDomainEventPublisher, deferred dispatch pattern
- [ ] **Specification Pattern**: ISpecification<T> with composable criteria
- [ ] **Unit Tests**: ≥80% coverage for all domain abstractions
- [ ] **Code Examples**: At least 3 value objects, 1 aggregate example
- [ ] **Documentation**: All patterns documented with usage examples
- [ ] **No Reflection**: Zero reflection usage confirmed via analyzer
- [ ] **Multi-Tenant Enforcement**: ITenantEntity on all examples

### Should Have (Non-Blocking)

- [ ] Specification combinators (And, Or, Not)
- [ ] Additional value object examples (Address, DateRange, PhoneNumber)
- [ ] Error catalog pattern for common validation errors
- [ ] Sample domain events with handlers (documentation)
- [ ] Performance benchmarks for Result vs exceptions

### Nice to Have (Future)

- [ ] Source generator design notes for handler registration
- [ ] Advanced specification features (dynamic queries)
- [ ] Domain-driven error codes with localization support
- [ ] Audit log entity and snapshot patterns

---

## Tracking Checklist

Use this checklist to track progress within Phase 2:

### Core Interfaces

- [ ] Define IEntity<TKey>
- [ ] Define IAuditableEntity
- [ ] Define ISoftDeletableEntity
- [ ] Define ITenantEntity
- [ ] Write unit tests for interface compositions

### Value Objects

- [ ] Implement ValueObject base class
- [ ] Create Money value object with tests
- [ ] Create Email value object with tests
- [ ] Document factory method pattern
- [ ] Validate Result<T> usage in factory methods

### Aggregates & Entities

- [ ] Implement Entity<TKey> base class
- [ ] Implement AggregateRoot<TKey> with domain events
- [ ] Create Order aggregate example
- [ ] Write tests for invariant protection
- [ ] Validate tenant isolation on aggregates

### Result Pattern

- [ ] Implement Result (non-generic)
- [ ] Implement Result<T>
- [ ] Implement PagedResult<T>
- [ ] Write unit tests for Result combinators
- [ ] Document usage patterns

### CQRS Contracts

- [ ] Define ICommand and ICommand<TResponse>
- [ ] Define IQuery<TResponse>
- [ ] Define ICommandHandler<TCommand>
- [ ] Define IQueryHandler<TQuery, TResponse>
- [ ] Create example command and handler
- [ ] Document explicit registration pattern

### Repositories

- [ ] Define IRepository<TEntity, TKey>
- [ ] Define IUnitOfWork
- [ ] Document tenant-scoped repository behavior
- [ ] Create usage examples

### Specifications

- [ ] Define ISpecification<T>
- [ ] Implement Specification<T> base class
- [ ] Create OrdersByStatusSpec example
- [ ] Implement And combinator
- [ ] Write tests for specification composition

### Domain Events

- [ ] Define IDomainEvent interface
- [ ] Create DomainEvent base class
- [ ] Define IDomainEventPublisher
- [ ] Create OrderCreatedEvent example
- [ ] Document deferred dispatch pattern

### Testing & Quality

- [ ] Achieve ≥80% test coverage
- [ ] Run mutation tests on value objects
- [ ] Verify zero reflection usage
- [ ] Run architecture tests
- [ ] Validate markdown lint passes

### Documentation

- [ ] Complete Key Activities section
- [ ] Complete Deliverables table
- [ ] Complete all code examples
- [ ] Add cross-references to ADRs
- [ ] Review and finalize

---

## Dependencies & Relationships

### Prerequisites

- **Phase 0**: Design principles, ADR-0001 (Tenancy), ADR-0002 (Audit), ADR-0003 (Soft Delete)
- **Phase 1**: Solution structure, build configuration, testing framework setup
- **ADR-0005**: DI Container Strategy (explicit registration, no reflection)

### Outputs to Other Phases

- **Phase 3 (Application)**: Handler decorators will consume CQRS contracts
- **Phase 4 (Web)**: Controllers will use commands/queries and Result<T>
- **Phase 5 (Infrastructure)**: EF Core will implement repository interfaces and specifications
- **Phase 6 (Release)**: Sample applications will demonstrate domain patterns

### External Dependencies

- .NET 8.0 SDK (for modern C# features)
- xUnit (testing framework)
- Shouldly (assertions)
- NSubstitute (mocking)
- EF Core 8.0 (for expression tree compatibility in specifications)

---

## Review Schedule

| Review Type | Cadence | Attendees | Purpose |
|-------------|---------|-----------|----------|
| **Domain Model Review** | Weekly | Domain Team + Architects | Validate aggregate boundaries and DDD patterns |
| **Code Example Review** | Per PR | Domain Team + Reviewers | Ensure compilability and idiomatic usage |
| **Architecture Review** | At 50% complete | Architects + Security | Validate ADR alignment and multi-tenancy |
| **Final Sign-off** | At exit criteria | All stakeholders | Approve phase completion |

---

## References

### Internal Documents

- [Phase 0: Discovery & Guardrails](../phase-0-discovery/phase-0-discovery.md)
- [Phase 1: Platform Scaffolding](../phase-1-platform/phase-1-platform.md)
- [ADR-0001: Tenancy Strategy](../adrs/ADR-0001-Tenancy-Strategy.md)
- [ADR-0002: Audit Logging](../adrs/ADR-0002-Audit-Logging.md)
- [ADR-0003: Soft Delete](../adrs/ADR-0003-Soft-Delete.md)
- [ADR-0005: DI Container Strategy](../adrs/ADR-0005-DI-Container-Strategy.md)
- [Discovery Summary](../discovery-summary.md)
- [Glossary](../glossary.md)
- [CQRS Framework Plan](../cqrs-framework-plan.md)

### External References

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Implementing Domain-Driven Design by Vaughn Vernon](https://vaughnvernon.com/)
- [C# 12 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)
- [.NET 8 What's New](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Specification Pattern](https://en.wikipedia.org/wiki/Specification_pattern)
- [Repository Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Value Objects](https://martinfowler.com/bliki/ValueObject.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)

---

**Next Phase**: [Phase 3: Application Layer](../phase-3-application/phase-3-application.md) (pending)

**Document Maintainer**: Domain Architecture Team  
**Last Review Date**: 2025-10-04  
**Next Review Date**: TBD (after initial draft)
