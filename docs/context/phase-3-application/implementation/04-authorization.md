# Guide 4: Authorization

**Phase**: 3 - Application Layer  
**Component**: Authorization Behavior & Permission Management  
**Prerequisites**: Guide 3 (Validation Pipeline)  
**Estimated Time**: 2-3 hours

---

## Table of Contents

1. [Overview](#overview)
2. [Design Rationale](#design-rationale)
3. [Core Concepts](#core-concepts)
4. [Authorization Behavior Implementation](#authorization-behavior-implementation)
5. [Permission Checking](#permission-checking)
6. [Tenant Isolation](#tenant-isolation)
7. [Usage Examples](#usage-examples)
8. [Unit Testing](#unit-testing)
9. [Best Practices](#best-practices)
10. [Common Pitfalls](#common-pitfalls)
11. [Next Steps](#next-steps)

---

## Overview

The Authorization layer ensures that only authenticated users with proper permissions can execute commands and queries. It provides:

- **Role-based access control (RBAC)**: Users have roles with specific permissions
- **Policy-based authorization**: Flexible permission rules
- **Tenant isolation**: Multi-tenant data segregation
- **Resource-level authorization**: Check ownership before operations
- **Audit logging**: Track who did what and when

### Key Components

| Component | Purpose |
|-----------|---------|
| `ICurrentUserService` | Provides current user context |
| `IAuthorizationService` | Checks permissions |
| `AuthorizationBehavior<TRequest, TResponse>` | MediatR behavior enforcing authorization |
| `[Authorize]` attribute | Marks commands/queries requiring authorization |
| `ITenantContext` | Provides current tenant context |

---

## Design Rationale

### Why Authorization in Pipeline?

```
Request → ValidationBehavior → AuthorizationBehavior → Handler
                                      ↑ Permission denied
                                      ↓ Return 403 Forbidden
```

**Benefits**:
1. **Centralized**: Single place for all authorization logic
2. **Declarative**: Mark commands/queries with attributes
3. **Consistent**: Same authorization rules everywhere
4. **Testable**: Easy to test authorization independently
5. **Auditable**: All authorization decisions logged

### Permission Model

```
User → Has → Roles → Contain → Permissions
                                    ↓
                            Can execute Commands/Queries
```

---

## Core Concepts

### 1. Current User Context

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Provides information about the current authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's unique identifier.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user's roles.
    /// </summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Gets the current tenant identifier (for multi-tenant apps).
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Determines if the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the user has a specific permission.
    /// </summary>
    bool HasPermission(string permission);
}
```

---

### 2. Authorization Attribute

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Marks a command or query as requiring authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// Required permissions (any of them grants access).
    /// </summary>
    public string[] Permissions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Required roles (any of them grants access).
    /// </summary>
    public string[] Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Requires authenticated user (default: true).
    /// </summary>
    public bool RequireAuthentication { get; init; } = true;

    public AuthorizeAttribute()
    {
    }

    public AuthorizeAttribute(params string[] permissions)
    {
        Permissions = permissions;
    }
}
```

---

### 3. Permission Constants

```csharp
namespace Idevs.Application.Common;

/// <summary>
/// Defines application permissions.
/// </summary>
public static class Permissions
{
    public static class Products
    {
        public const string View = "products.view";
        public const string Create = "products.create";
        public const string Update = "products.update";
        public const string Delete = "products.delete";
        public const string Manage = "products.manage"; // All permissions
    }

    public static class Orders
    {
        public const string View = "orders.view";
        public const string Create = "orders.create";
        public const string Update = "orders.update";
        public const string Cancel = "orders.cancel";
        public const string Manage = "orders.manage";
    }

    public static class Users
    {
        public const string View = "users.view";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string ManageRoles = "users.manage_roles";
    }

    public static class Reports
    {
        public const string View = "reports.view";
        public const string Export = "reports.export";
    }
}
```

---

## Authorization Behavior Implementation

### Core Authorization Behavior

```csharp
namespace Idevs.Application.Behaviors;

/// <summary>
/// Enforces authorization rules before executing handlers.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        ICurrentUserService currentUserService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var authorizeAttribute = request.GetType()
            .GetCustomAttribute<AuthorizeAttribute>();

        // No authorization required
        if (authorizeAttribute is null)
            return await next();

        var requestName = typeof(TRequest).Name;

        // Check authentication
        if (authorizeAttribute.RequireAuthentication && !_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning(
                "Unauthorized access attempt to {RequestName}",
                requestName);

            return CreateUnauthorizedResult<TResponse>();
        }

        // Check roles
        if (authorizeAttribute.Roles.Length > 0)
        {
            var hasRequiredRole = authorizeAttribute.Roles
                .Any(role => _currentUserService.Roles.Contains(role));

            if (!hasRequiredRole)
            {
                _logger.LogWarning(
                    "User {UserId} lacks required role for {RequestName}. Required: {RequiredRoles}",
                    _currentUserService.UserId,
                    requestName,
                    string.Join(", ", authorizeAttribute.Roles));

                return CreateForbiddenResult<TResponse>();
            }
        }

        // Check permissions
        if (authorizeAttribute.Permissions.Length > 0)
        {
            var hasRequiredPermission = authorizeAttribute.Permissions
                .Any(permission => _currentUserService.HasPermission(permission));

            if (!hasRequiredPermission)
            {
                _logger.LogWarning(
                    "User {UserId} lacks required permission for {RequestName}. Required: {RequiredPermissions}",
                    _currentUserService.UserId,
                    requestName,
                    string.Join(", ", authorizeAttribute.Permissions));

                return CreateForbiddenResult<TResponse>();
            }
        }

        _logger.LogDebug(
            "User {UserId} authorized for {RequestName}",
            _currentUserService.UserId,
            requestName);

        return await next();
    }

    private static TResponse CreateUnauthorizedResult<T>()
    {
        var error = Error.Unauthorized(
            "Authorization.Unauthenticated",
            "Authentication is required to access this resource.");

        return CreateAuthorizationResult<T>(error);
    }

    private static TResponse CreateForbiddenResult<T>()
    {
        var error = Error.Forbidden(
            "Authorization.Forbidden",
            "You do not have permission to access this resource.");

        return CreateAuthorizationResult<T>(error);
    }

    private static TResponse CreateAuthorizationResult<T>(Error error)
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

## Permission Checking

### Resource Ownership Checking

```csharp
namespace Idevs.Application.Products.Commands.Update;

[Authorize(Permissions.Products.Update)]
public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    decimal Price
) : ICommand<Unit>;

public sealed class UpdateProductCommandHandler
    : ICommandHandler<UpdateProductCommand, Unit>
{
    private readonly IRepository<Product> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
        IRepository<Product> repository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);
        var product = await _repository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
            return Result.Failure<Unit>(
                Error.NotFound("Product.NotFound", "Product not found."));

        // Check resource ownership (if applicable)
        if (!CanModifyProduct(product))
            return Result.Failure<Unit>(
                Error.Forbidden(
                    "Product.NotOwned",
                    "You can only modify your own products."));

        var updateResult = product.UpdateDetails(request.Name, Money.From(request.Price));
        if (updateResult.IsFailure)
            return Result.Failure<Unit>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }

    private bool CanModifyProduct(Product product)
    {
        // Admins can modify any product
        if (_currentUserService.Roles.Contains("Admin"))
            return true;

        // Users can only modify their own products
        return product.CreatedByUserId == _currentUserService.UserId;
    }
}
```

---

## Tenant Isolation

### Tenant Context

```csharp
namespace Idevs.Application.Contracts;

/// <summary>
/// Provides current tenant context for multi-tenant applications.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant identifier.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Gets the current tenant name.
    /// </summary>
    string TenantName { get; }

    /// <summary>
    /// Determines if the application is running in multi-tenant mode.
    /// </summary>
    bool IsMultiTenant { get; }
}
```

---

### Tenant-Aware Repository

```csharp
namespace Idevs.Application.Common;

/// <summary>
/// Base class for tenant-aware repositories.
/// </summary>
public abstract class TenantAwareRepositoryBase<T> : IRepository<T>
    where T : class, IAggregateRoot
{
    protected readonly DbContext Context;
    protected readonly ITenantContext TenantContext;

    protected TenantAwareRepositoryBase(
        DbContext context,
        ITenantContext tenantContext)
    {
        Context = context;
        TenantContext = tenantContext;
    }

    public virtual async Task<T?> GetByIdAsync(
        EntityId<T> id,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<T>()
            .Where(e => e.Id == id && e.TenantId == TenantContext.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<T>()
            .Where(e => e.TenantId == TenantContext.TenantId);

        query = specification.Apply(query);

        return await query.ToListAsync(cancellationToken);
    }

    // Other repository methods with tenant filtering...
}
```

---

### Tenant Isolation Behavior

```csharp
namespace Idevs.Application.Behaviors;

/// <summary>
/// Ensures all queries are filtered by current tenant.
/// </summary>
public sealed class TenantIsolationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantIsolationBehavior<TRequest, TResponse>> _logger;

    public TenantIsolationBehavior(
        ITenantContext tenantContext,
        ILogger<TenantIsolationBehavior<TRequest, TResponse>> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsMultiTenant)
            return await next();

        var requestName = typeof(TRequest).Name;

        _logger.LogDebug(
            "Executing {RequestName} for tenant {TenantId}",
            requestName,
            _tenantContext.TenantId);

        return await next();
    }
}
```

---

## Usage Examples

### Example 1: Command with Permission Check

```csharp
namespace Idevs.Application.Products.Commands.Delete;

[Authorize(Permissions.Products.Delete)]
public sealed record DeleteProductCommand(Guid ProductId) : ICommand<Unit>;

public sealed class DeleteProductCommandHandler
    : ICommandHandler<DeleteProductCommand, Unit>
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(
        IRepository<Product> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);
        var product = await _repository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
            return Result.Failure<Unit>(
                Error.NotFound("Product.NotFound", "Product not found."));

        await _repository.DeleteAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}
```

---

### Example 2: Query with Role Check

```csharp
namespace Idevs.Application.Reports.Queries.SalesReport;

[Authorize(Roles = new[] { "Admin", "Manager" })]
public sealed record GetSalesReportQuery(
    DateTime StartDate,
    DateTime EndDate
) : IQuery<SalesReportDto>;

public sealed class GetSalesReportQueryHandler
    : IQueryHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly ISalesReportingService _reportingService;

    public GetSalesReportQueryHandler(ISalesReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    public async Task<Result<SalesReportDto>> Handle(
        GetSalesReportQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _reportingService.GenerateSalesReportAsync(
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return Result.Success(report);
    }
}
```

---

### Example 3: Multi-Permission Check

```csharp
namespace Idevs.Application.Orders.Commands.Cancel;

[Authorize(
    Permissions.Orders.Cancel,
    Permissions.Orders.Manage)]
public sealed record CancelOrderCommand(Guid OrderId) : ICommand<Unit>;
```

---

### Example 4: Public Query (No Authorization)

```csharp
namespace Idevs.Application.Products.Queries.ListPublic;

// No [Authorize] attribute - publicly accessible
public sealed record ListPublicProductsQuery(
    int PageNumber,
    int PageSize
) : IQuery<PagedResult<ProductListItemDto>>;
```

---

## Unit Testing

### Test 1: Authorized Access

```csharp
namespace Idevs.Application.Tests.Behaviors;

public sealed class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_AuthorizedUser_CallsNextDelegate()
    {
        // Arrange
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.IsAuthenticated.Returns(true);
        currentUserService.UserId.Returns(Guid.NewGuid());
        currentUserService.HasPermission(Permissions.Products.Create).Returns(true);

        var logger = Substitute.For<ILogger<AuthorizationBehavior<TestCommand, Result<string>>>>();
        var behavior = new AuthorizationBehavior<TestCommand, Result<string>>(
            currentUserService,
            logger);

        var command = new TestCommand();
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
    public async Task Handle_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.IsAuthenticated.Returns(false);

        var logger = Substitute.For<ILogger<AuthorizationBehavior<TestCommand, Result<string>>>>();
        var behavior = new AuthorizationBehavior<TestCommand, Result<string>>(
            currentUserService,
            logger);

        var command = new TestCommand();
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
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_LacksPermission_ReturnsForbidden()
    {
        // Arrange
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.IsAuthenticated.Returns(true);
        currentUserService.UserId.Returns(Guid.NewGuid());
        currentUserService.HasPermission(Permissions.Products.Create).Returns(false);

        var logger = Substitute.For<ILogger<AuthorizationBehavior<TestCommand, Result<string>>>>();
        var behavior = new AuthorizationBehavior<TestCommand, Result<string>>(
            currentUserService,
            logger);

        var command = new TestCommand();
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
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Authorize(Permissions.Products.Create)]
    private sealed record TestCommand : ICommand<string>;
}
```

---

### Test 2: Resource Ownership

```csharp
public sealed class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserOwnsProduct_UpdatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = CreateTestProduct(createdByUserId: userId);

        var repository = Substitute.For<IRepository<Product>>();
        repository.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(userId);
        currentUserService.Roles.Returns(new[] { "User" });

        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new UpdateProductCommandHandler(
            repository,
            currentUserService,
            unitOfWork);

        var command = new UpdateProductCommand(product.Id.Value, "New Name", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnProduct_ReturnsForbidden()
    {
        // Arrange
        var productOwnerId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var product = CreateTestProduct(createdByUserId: productOwnerId);

        var repository = Substitute.For<IRepository<Product>>();
        repository.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(currentUserId);
        currentUserService.Roles.Returns(new[] { "User" });

        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new UpdateProductCommandHandler(
            repository,
            currentUserService,
            unitOfWork);

        var command = new UpdateProductCommand(product.Id.Value, "New Name", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

---

## Best Practices

### ✅ DO

1. **Use permission constants**
   ```csharp
   [Authorize(Permissions.Products.Create)] // ✅ Type-safe
   [Authorize("products.create")] // ❌ Magic string
   ```

2. **Check resource ownership in handlers**
   ```csharp
   if (product.CreatedByUserId != _currentUserService.UserId)
       return Result.Failure<Unit>(Error.Forbidden(...));
   ```

3. **Log authorization failures**
   ```csharp
   _logger.LogWarning(
       "User {UserId} denied access to {Resource}",
       userId, resourceId);
   ```

4. **Use tenant-aware repositories**
   ```csharp
   public class ProductRepository : TenantAwareRepositoryBase<Product>
   ```

5. **Separate read and write permissions**
   ```csharp
   public static class Products
   {
       public const string View = "products.view";
       public const string Modify = "products.modify";
   }
   ```

---

### ❌ DON'T

1. **Don't hardcode user IDs**
   ```csharp
   if (userId == Guid.Parse("...")) // ❌ Bad
   ```

2. **Don't skip authorization for "admin" endpoints**
   ```csharp
   // ❌ Bad: Still needs [Authorize]
   public sealed record DeleteAllProductsCommand : ICommand<Unit>;
   ```

3. **Don't mix authorization and business logic**
   ```csharp
   // ❌ Bad: Authorization in domain
   public class Product
   {
       public void Delete(Guid userId)
       {
           if (CreatedByUserId != userId) throw ...
       }
   }
   ```

4. **Don't return detailed error messages**
   ```csharp
   // ❌ Bad: Leaks information
   return Error.Forbidden("User john@example.com lacks permission");

   // ✅ Good: Generic message
   return Error.Forbidden("You do not have permission");
   ```

5. **Don't forget tenant filtering in queries**
   ```csharp
   // ❌ Bad: Returns data from all tenants
   var products = await _context.Products.ToListAsync();

   // ✅ Good: Filtered by tenant
   var products = await _repository.ListAsync(spec);
   ```

---

## Common Pitfalls

### Pitfall 1: Authorization After Loading Data

**Problem**: Loading sensitive data before checking authorization.

```csharp
// ❌ Bad
public async Task<Result<ProductDto>> Handle(...)
{
    var product = await _repository.GetByIdAsync(id); // Loads data
    
    if (!_currentUserService.HasPermission(...))
        return Result.Failure(...);
}
```

**Solution**: Check authorization first when possible.

```csharp
// ✅ Good
[Authorize(Permissions.Products.View)]
public sealed record GetProductQuery(...) : IQuery<ProductDto>;
```

---

### Pitfall 2: Tenant Leakage

**Problem**: Forgetting to filter by tenant ID.

```csharp
// ❌ Bad: Cross-tenant data leak
public async Task<Product?> GetByIdAsync(ProductId id)
{
    return await _context.Products
        .FirstOrDefaultAsync(p => p.Id == id);
}
```

**Solution**: Always include tenant filter.

```csharp
// ✅ Good
public async Task<Product?> GetByIdAsync(ProductId id)
{
    return await _context.Products
        .Where(p => p.Id == id && p.TenantId == _tenantContext.TenantId)
        .FirstOrDefaultAsync();
}
```

---

## Next Steps

1. **Read Guide 5**: Logging & Observability (`05-LOGGING-OBSERVABILITY.md`)
2. **Implement ICurrentUserService**: Extract user from HTTP context
3. **Implement AuthorizationBehavior**: Add to MediatR pipeline
4. **Define Permissions**: Create permission constants
5. **Add [Authorize] attributes**: Mark commands/queries requiring authorization
6. **Test Authorization**: Write unit tests for authorization scenarios

---

**Summary**: Authorization ensures only permitted users can execute commands and queries. Use the AuthorizationBehavior to check permissions declaratively with attributes, and enforce tenant isolation for multi-tenant applications.

---

**Implementation Checklist**:

- [ ] Create `ICurrentUserService` interface and implementation
- [ ] Create `AuthorizeAttribute` with permission/role properties
- [ ] Define permission constants in `Permissions` class
- [ ] Implement `AuthorizationBehavior<TRequest, TResponse>`
- [ ] Add AuthorizationBehavior to MediatR pipeline (after validation)
- [ ] Implement tenant-aware repositories (if multi-tenant)
- [ ] Add [Authorize] attributes to commands/queries
- [ ] Write unit tests for authorization scenarios
- [ ] Test tenant isolation thoroughly

---

**Last Updated**: 2025-01-08  
**Next Guide**: 05-LOGGING-OBSERVABILITY.md
