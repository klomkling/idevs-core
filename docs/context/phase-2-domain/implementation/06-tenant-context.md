# 06: Tenant & User Context - Part 1

> **Navigation:** [Index](README.md) • [Part 1](06-tenant-context.md) • [Part 2](06-user-context.md)

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

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 →](06-user-context.md)**
