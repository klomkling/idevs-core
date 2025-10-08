# Guide 6: Multi-Tenancy

Phase: 4 - Web/API Layer
Component: Multi-Tenancy
Depends On: 03-Middleware Pipeline, Phase 3 Authorization

---

## Overview

Multi-tenancy ensures data and behavior are scoped to the current tenant. This guide covers:
- Tenant context abstraction
- Resolution strategies: header, claims, (optional) subdomain
- Middleware and policy enforcement
- Testing patterns

---

## Tenant Context

```csharp
namespace Idevs.Web.MultiTenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    void SetTenantId(Guid id);
}

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public void SetTenantId(Guid id) => TenantId = id;
}
```

Register as scoped service:

```csharp
builder.Services.AddScoped<ITenantContext, TenantContext>();
```

---

## Resolution Strategies

### 1) Header (preferred)

```csharp
public static class TenantResolutionConstants
{
    public const string TenantHeader = "X-Tenant-ID";
}

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var header = context.Request.Headers[TenantResolutionConstants.TenantHeader].FirstOrDefault();
        var claim = context.User.FindFirst("tenant_id")?.Value;
        var value = header ?? claim;

        if (Guid.TryParse(value, out var tenantId))
        {
            tenantContext.SetTenantId(tenantId);
            _logger.LogDebug("Resolved tenant {TenantId}", tenantId);
        }
        else if (!string.IsNullOrWhiteSpace(value))
        {
            _logger.LogWarning("Invalid tenant value: {Value}", value);
        }

        await _next(context);
    }
}
```

### 2) Subdomain (optional)

```csharp
public static class SubdomainTenantParser
{
    public static bool TryParse(Uri requestUri, out Guid tenantId)
    {
        // Example: https://{tenant}.api.idevs.work
        tenantId = default;
        var host = requestUri.Host;
        var first = host.Split('.').FirstOrDefault();
        return Guid.TryParse(first, out tenantId);
    }
}
```

---

## Enforcement

### Authorization Policy

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTenant", policy => policy.RequireAssertion(context =>
    {
        var tenantContext = context.User?.Identity?.IsAuthenticated == true
            ? context.Resource is HttpContext httpContext
                ? httpContext.RequestServices.GetRequiredService<ITenantContext>()
                : null
            : null;
        return tenantContext?.TenantId is not null;
    }));
});
```

### Controller Usage

```csharp
[Authorize(Policy = "RequireTenant")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly ITenantContext _tenant;

    public OrdersController(IMediator mediator, ITenantContext tenant) : base(mediator) => _tenant = tenant;

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => SendQuery(new ListOrdersQuery(_tenant.TenantId!.Value, page, pageSize));
}
```

---

## Data Access Considerations

- Always include TenantId in queries (repository/specification pattern)
- Enforce tenant checks in handlers and domain services
- Add unique constraints by TenantId + NaturalKey when applicable

---

## Testing

```csharp
public sealed class TenantMiddlewareTests
{
    [Fact]
    public async Task Resolves_Tenant_From_Header()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(s => s.AddScoped<ITenantContext, TenantContext>())
            .Configure(app =>
            {
                app.UseMiddleware<TenantResolutionMiddleware>();
                app.Run(ctx =>
                {
                    var tenant = ctx.RequestServices.GetRequiredService<ITenantContext>().TenantId;
                    return ctx.Response.WriteAsync(tenant?.ToString() ?? "null");
                });
            });

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var tenantId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Tenant-ID", tenantId.ToString());
        var response = await client.SendAsync(request);
        var value = await response.Content.ReadAsStringAsync();
        value.ShouldBe(tenantId.ToString());
    }
}
```

---

## Pitfalls

- Resolving tenant after authorization can invalidate policy checks
- Not validating tenant format allows header injection; always Guid.TryParse
- Missing tenant in data layer leads to cross-tenant data leakage

---

Last Updated: 2025-01-08
Maintained By: Idevs Framework Team
License: MIT