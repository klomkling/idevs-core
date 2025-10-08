# 06: Tenant & User Context

> **Phase:** 2 - Domain & Contracts  
> **Guide:** 06 of 09  
> **Estimated Time:** 2-3 hours

## Overview

Define abstractions for accessing current tenant and user context throughout the application, enabling multi-tenant isolation, audit trails, and authorization decisions.

## Prerequisites

- Phase 1 complete (solution structure)
- Guide 01 complete (entity interfaces with ITenant)
- Understanding of multi-tenancy patterns
- Familiarity with dependency injection and scoped services

## Objectives

### What You'll Build

1. **ITenantContext** - Access current tenant information
2. **ICurrentUser** - Access authenticated user information
3. **TenantInfo** - Tenant metadata (ID, name, plan)
4. **UserInfo** - User metadata (ID, email, roles, claims)
5. **Test implementations** - In-memory context for testing

### Why This Matters

- Enforces tenant isolation at the framework level
- Enables automatic audit trail population (CreatedBy, UpdatedBy)
- Supports row-level security in database queries
- Simplifies authorization decisions (check current user roles)
- Prevents cross-tenant data leaks

## Implementation Steps

### 1. Create Context Folder

```bash
mkdir -p src/Idevs/Context
```

### 2. Define TenantInfo Class

**File:** `src/Idevs/Context/TenantInfo.cs`

```csharp path=null start=null
namespace Idevs.Context;

/// <summary>
/// Represents information about the current tenant
/// </summary>
public sealed class TenantInfo
{
    public TenantInfo(Guid tenantId, string name, string? subscriptionPlan = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required", nameof(name));

        TenantId = tenantId;
        Name = name;
        SubscriptionPlan = subscriptionPlan;
    }

    /// <summary>
    /// Unique identifier of the tenant
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    /// Display name of the tenant
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Subscription plan (e.g., "Free", "Pro", "Enterprise")
    /// </summary>
    public string? SubscriptionPlan { get; }

    /// <summary>
    /// Additional metadata (feature flags, limits, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
```

**Design Rationale:**
- Immutable by design (init-only properties)
- Validation in constructor prevents invalid state
- Metadata dictionary for extensibility (feature flags, tenant-specific config)
- Subscription plan for feature gating

### 3. Define ITenantContext Interface

**File:** `src/Idevs/Context/ITenantContext.cs`

```csharp path=null start=null
namespace Idevs.Context;

/// <summary>
/// Provides access to the current tenant context
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID. Returns null for system operations.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets detailed information about the current tenant
    /// </summary>
    TenantInfo? GetTenantInfo();

    /// <summary>
    /// Indicates whether a tenant is currently resolved
    /// </summary>
    bool HasTenant { get; }
}
```

**Design Rationale:**
- `TenantId` nullable for system operations (background jobs, migrations)
- `GetTenantInfo()` for full tenant details (lazy load if needed)
- `HasTenant` for explicit checks before tenant-scoped operations

### 4. Define UserInfo Class

**File:** `src/Idevs/Context/UserInfo.cs`

```csharp path=null start=null
namespace Idevs.Context;

/// <summary>
/// Represents information about the current user
/// </summary>
public sealed class UserInfo
{
    public UserInfo(
        string userId,
        string email,
        string? displayName = null,
        IReadOnlyList<string>? roles = null,
        Dictionary<string, string>? claims = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        UserId = userId;
        Email = email;
        DisplayName = displayName;
        Roles = roles ?? Array.Empty<string>();
        Claims = claims ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Unique identifier of the user
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Display name (full name)
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// User roles (e.g., "Admin", "Manager", "User")
    /// </summary>
    public IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Additional claims from identity provider
    /// </summary>
    public Dictionary<string, string> Claims { get; }

    /// <summary>
    /// Checks if user has a specific role
    /// </summary>
    public bool IsInRole(string role) => 
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if user has any of the specified roles
    /// </summary>
    public bool IsInAnyRole(params string[] roles) => 
        roles.Any(role => IsInRole(role));
}
```

**Design Rationale:**
- String-based UserId (supports various identity providers)
- Immutable design with read-only collections
- Helper methods for role checks (case-insensitive)
- Claims dictionary for custom authorization logic

### 5. Define ICurrentUser Interface

**File:** `src/Idevs/Context/ICurrentUser.cs`

```csharp path=null start=null
namespace Idevs.Context;

/// <summary>
/// Provides access to the current authenticated user
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the current user ID. Returns null for anonymous requests.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user's email. Returns null for anonymous requests.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Indicates whether a user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets detailed information about the current user
    /// </summary>
    UserInfo? GetUserInfo();

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Gets the value of a specific claim
    /// </summary>
    string? GetClaimValue(string claimType);
}
```

**Design Rationale:**
- Nullable properties for anonymous requests
- `IsAuthenticated` flag for guard clauses
- Convenience methods (IsInRole, GetClaimValue) reduce boilerplate
- `GetUserInfo()` for full user details

### 6. Create Test Implementations

**File:** `src/Idevs/Context/Testing/TestTenantContext.cs`

```csharp path=null start=null
namespace Idevs.Context.Testing;

/// <summary>
/// In-memory tenant context for testing
/// </summary>
public sealed class TestTenantContext : ITenantContext
{
    private TenantInfo? _tenantInfo;

    public Guid? TenantId => _tenantInfo?.TenantId;
    public bool HasTenant => _tenantInfo != null;

    public TenantInfo? GetTenantInfo() => _tenantInfo;

    /// <summary>
    /// Sets the current tenant for testing
    /// </summary>
    public void SetTenant(TenantInfo tenantInfo)
    {
        _tenantInfo = tenantInfo;
    }

    /// <summary>
    /// Sets the current tenant by ID and name
    /// </summary>
    public void SetTenant(Guid tenantId, string name)
    {
        _tenantInfo = new TenantInfo(tenantId, name);
    }

    /// <summary>
    /// Clears the current tenant context
    /// </summary>
    public void Clear()
    {
        _tenantInfo = null;
    }
}
```

**File:** `src/Idevs/Context/Testing/TestCurrentUser.cs`

```csharp path=null start=null
namespace Idevs.Context.Testing;

/// <summary>
/// In-memory current user for testing
/// </summary>
public sealed class TestCurrentUser : ICurrentUser
{
    private UserInfo? _userInfo;

    public string? UserId => _userInfo?.UserId;
    public string? Email => _userInfo?.Email;
    public bool IsAuthenticated => _userInfo != null;

    public UserInfo? GetUserInfo() => _userInfo;

    public bool IsInRole(string role)
    {
        return _userInfo?.IsInRole(role) ?? false;
    }

    public string? GetClaimValue(string claimType)
    {
        return _userInfo?.Claims.TryGetValue(claimType, out var value) == true
            ? value
            : null;
    }

    /// <summary>
    /// Sets the current user for testing
    /// </summary>
    public void SetUser(UserInfo userInfo)
    {
        _userInfo = userInfo;
    }

    /// <summary>
    /// Sets the current user by ID and email
    /// </summary>
    public void SetUser(string userId, string email, params string[] roles)
    {
        _userInfo = new UserInfo(userId, email, roles: roles);
    }

    /// <summary>
    /// Clears the current user context (anonymous)
    /// </summary>
    public void Clear()
    {
        _userInfo = null;
    }
}
```

**Design Rationale:**
- Test doubles for unit testing without authentication middleware
- Mutable for test setup (SetTenant, SetUser, Clear)
- Null-safe implementations return false/null when not set

### 7. Write Unit Tests

**File:** `tests/Idevs.Tests/Context/TenantContextTests.cs`

```csharp path=null start=null
using Idevs.Context;
using Idevs.Context.Testing;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Context;

public class TenantContextTests
{
    [Fact]
    public void TenantInfo_RequiresTenantId()
    {
        Should.Throw<ArgumentException>(() =>
            new TenantInfo(Guid.Empty, "Test Tenant"));
    }

    [Fact]
    public void TenantInfo_RequiresName()
    {
        Should.Throw<ArgumentException>(() =>
            new TenantInfo(Guid.NewGuid(), ""));
    }

    [Fact]
    public void TenantInfo_StoresMetadata()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new TenantInfo(tenantId, "Test Tenant", "Pro")
        {
            Metadata = { ["MaxUsers"] = 100, ["FeatureX"] = true }
        };

        tenant.TenantId.ShouldBe(tenantId);
        tenant.Name.ShouldBe("Test Tenant");
        tenant.SubscriptionPlan.ShouldBe("Pro");
        tenant.Metadata["MaxUsers"].ShouldBe(100);
    }

    [Fact]
    public void TestTenantContext_InitiallyEmpty()
    {
        var context = new TestTenantContext();

        context.HasTenant.ShouldBeFalse();
        context.TenantId.ShouldBeNull();
        context.GetTenantInfo().ShouldBeNull();
    }

    [Fact]
    public void TestTenantContext_SetTenant_StoresInfo()
    {
        var context = new TestTenantContext();
        var tenantId = Guid.NewGuid();

        context.SetTenant(tenantId, "Test Tenant");

        context.HasTenant.ShouldBeTrue();
        context.TenantId.ShouldBe(tenantId);
        context.GetTenantInfo().ShouldNotBeNull();
        context.GetTenantInfo()!.Name.ShouldBe("Test Tenant");
    }

    [Fact]
    public void TestTenantContext_Clear_RemovesTenant()
    {
        var context = new TestTenantContext();
        context.SetTenant(Guid.NewGuid(), "Test");

        context.Clear();

        context.HasTenant.ShouldBeFalse();
        context.TenantId.ShouldBeNull();
    }
}
```

**File:** `tests/Idevs.Tests/Context/CurrentUserTests.cs`

```csharp path=null start=null
using Idevs.Context;
using Idevs.Context.Testing;
using Shouldly;
using Xunit;

namespace Idevs.Tests.Context;

public class CurrentUserTests
{
    [Fact]
    public void UserInfo_RequiresUserId()
    {
        Should.Throw<ArgumentException>(() =>
            new UserInfo("", "test@example.com"));
    }

    [Fact]
    public void UserInfo_RequiresEmail()
    {
        Should.Throw<ArgumentException>(() =>
            new UserInfo("user-123", ""));
    }

    [Fact]
    public void UserInfo_IsInRole_CaseInsensitive()
    {
        var user = new UserInfo(
            "user-123",
            "test@example.com",
            roles: new[] { "Admin", "Manager" });

        user.IsInRole("admin").ShouldBeTrue();
        user.IsInRole("ADMIN").ShouldBeTrue();
        user.IsInRole("Manager").ShouldBeTrue();
        user.IsInRole("User").ShouldBeFalse();
    }

    [Fact]
    public void UserInfo_IsInAnyRole_ChecksMultiple()
    {
        var user = new UserInfo(
            "user-123",
            "test@example.com",
            roles: new[] { "Manager" });

        user.IsInAnyRole("Admin", "Manager").ShouldBeTrue();
        user.IsInAnyRole("Admin", "User").ShouldBeFalse();
    }

    [Fact]
    public void TestCurrentUser_InitiallyAnonymous()
    {
        var currentUser = new TestCurrentUser();

        currentUser.IsAuthenticated.ShouldBeFalse();
        currentUser.UserId.ShouldBeNull();
        currentUser.Email.ShouldBeNull();
    }

    [Fact]
    public void TestCurrentUser_SetUser_StoresInfo()
    {
        var currentUser = new TestCurrentUser();

        currentUser.SetUser("user-123", "test@example.com", "Admin");

        currentUser.IsAuthenticated.ShouldBeTrue();
        currentUser.UserId.ShouldBe("user-123");
        currentUser.Email.ShouldBe("test@example.com");
        currentUser.IsInRole("Admin").ShouldBeTrue();
    }

    [Fact]
    public void TestCurrentUser_Clear_MakesAnonymous()
    {
        var currentUser = new TestCurrentUser();
        currentUser.SetUser("user-123", "test@example.com");

        currentUser.Clear();

        currentUser.IsAuthenticated.ShouldBeFalse();
        currentUser.IsInRole("Admin").ShouldBeFalse();
    }

    [Fact]
    public void TestCurrentUser_GetClaimValue_ReturnsValue()
    {
        var currentUser = new TestCurrentUser();
        var claims = new Dictionary<string, string>
        {
            ["department"] = "Engineering",
            ["level"] = "Senior"
        };

        currentUser.SetUser(new UserInfo(
            "user-123",
            "test@example.com",
            claims: claims));

        currentUser.GetClaimValue("department").ShouldBe("Engineering");
        currentUser.GetClaimValue("level").ShouldBe("Senior");
        currentUser.GetClaimValue("nonexistent").ShouldBeNull();
    }
}
```

### 8. Run Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj --filter "FullyQualifiedName~Context"
```

Expected: ✅ All 13+ tests passing

## Usage Examples

### In Command Handlers

```csharp path=null start=null
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public async Task<Result<Guid>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // Ensure tenant context
        if (!_tenantContext.HasTenant)
            return new Error("Order.NoTenant", "Tenant context is required");

        // Create order with tenant isolation
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            CustomerId = command.CustomerId,
            CreatedBy = _currentUser.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(order, cancellationToken);
        return order.Id;
    }
}
```

### In EF Core Interceptors (Phase 5)

```csharp path=null start=null
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var context = eventData.Context;
        var entries = context.ChangeTracker.Entries<IAuditable>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                entry.Entity.CreatedBy = _currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                entry.Entity.UpdatedBy = _currentUser.UserId;
            }
        }

        return result;
    }
}
```

### In Authorization Policies

```csharp path=null start=null
public class TenantAuthorizationHandler : AuthorizationHandler<TenantRequirement>
{
    private readonly ITenantContext _tenantContext;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequirement requirement)
    {
        if (_tenantContext.HasTenant)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

## Verification Checklist

- [ ] TenantInfo class with validation
- [ ] ITenantContext interface
- [ ] UserInfo class with role helpers
- [ ] ICurrentUser interface
- [ ] TestTenantContext for testing
- [ ] TestCurrentUser for testing
- [ ] ≥13 unit tests covering all scenarios
- [ ] Usage examples documented

## Common Pitfalls

### ❌ Forgetting Tenant Context in Commands
```csharp
// BAD - no tenant isolation
public async Task<Result<Guid>> HandleAsync(CreateOrderCommand command)
{
    var order = new Order { CustomerId = command.CustomerId };
    await _repository.AddAsync(order);
    return order.Id;
}

// GOOD - enforces tenant context
public async Task<Result<Guid>> HandleAsync(CreateOrderCommand command)
{
    if (!_tenantContext.HasTenant)
        return new Error("Order.NoTenant", "Tenant required");

    var order = new Order 
    { 
        TenantId = _tenantContext.TenantId!.Value,
        CustomerId = command.CustomerId 
    };
    await _repository.AddAsync(order);
    return order.Id;
}
```

### ❌ Using Context in Domain Layer
```csharp
// BAD - domain layer depends on infrastructure
public class Order : Entity<Guid>
{
    private readonly ICurrentUser _currentUser; // NO!
    
    public void PlaceOrder()
    {
        CreatedBy = _currentUser.UserId;
    }
}

// GOOD - pass user ID explicitly
public class Order : Entity<Guid>
{
    public void PlaceOrder(string userId)
    {
        CreatedBy = userId;
    }
}
```

### ❌ Not Handling Anonymous Users
```csharp
// BAD - assumes authenticated user
var userId = _currentUser.UserId;
order.AssignTo(userId); // NullReferenceException!

// GOOD - checks authentication
if (!_currentUser.IsAuthenticated)
    return new Error("Order.Unauthorized", "Authentication required");

order.AssignTo(_currentUser.UserId!);
```

## Next Steps

- **[07-repository-uow.md](07-repository-uow.md)** - Repository pattern & Unit of Work
- **[08-domain-events.md](08-domain-events.md)** - Domain events infrastructure

## References

- [Multi-Tenancy Patterns](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/tenancy-models)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)

---

**[← Back to Phase 2](../phase-2-domain-NEW.md)** | **[Next: Repository & UoW →](07-repository-uow.md)**
