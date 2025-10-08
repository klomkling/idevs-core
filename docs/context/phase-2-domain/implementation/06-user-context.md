# 06: Tenant & User Context - Part 2

> **Navigation:** [Index](README.md) • [Part 1](06-tenant-context.md) • [Part 2](06-user-context.md)

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

---

**[← Part 1](06-tenant-context.md)** | **[Back to Phase 2](../phase-2-domain.md)**
