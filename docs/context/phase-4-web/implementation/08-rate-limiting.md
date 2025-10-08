# Guide 8: Rate Limiting

Phase: 4 - Web/API Layer
Component: Rate Limiting
Depends On: Middleware Pipeline

---

## Overview

Use ASP.NET Core rate limiting (System.Threading.RateLimiting) to protect your API against abuse and provide fairness.
- Global limiter for baseline protection
- Policy-based limiters for specific endpoints or users
- Partitioning by user, tenant, or IP
- RFC 7807 ProblemDetails for rejections

---

## Configuration

```csharp
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    // Global fixed window limiter (per IP or user)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.User.Identity?.Name
                  ?? context.Request.Headers["X-Tenant-ID"].FirstOrDefault()
                  ?? context.Connection.RemoteIpAddress?.ToString()
                  ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
        });
    });

    // Higher allowance for authenticated users (token bucket)
    options.AddPolicy("authenticated", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.User.Identity?.Name ?? "anonymous",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1000,
                ReplenishmentPeriod = TimeSpan.FromHours(1),
                TokensPerPeriod = 1000,
                AutoReplenishment = true
            }));

    // Strict limit for anonymous
    options.AddPolicy("anonymous", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Rate limit exceeded",
            Detail = "Too many requests. Please try again later.",
            Instance = context.HttpContext.Request.Path
        };
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
            problem.Extensions["retryAfterSeconds"] = (int)retryAfter.TotalSeconds;
        }
        await context.HttpContext.Response.WriteAsJsonAsync(problem, token);
    };
});

app.UseRateLimiter();
```

---

## Controller Annotations

```csharp
[EnableRateLimiting("authenticated")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ApiControllerBase
{
    // All endpoints in this controller use the "authenticated" policy
}

public sealed class PublicController : ControllerBase
{
    [HttpPost("/contact")]
    [EnableRateLimiting("anonymous")]
    public IActionResult Contact([FromBody] ContactRequest request) => Ok();
}
```

Disable for specific endpoints:

```csharp
[DisableRateLimiting]
[HttpGet("/public-feed")]
public IActionResult PublicFeed() => Ok();
```

---

## Best Practices

- Partition by highest-entropy identifier available (user → tenant → IP)
- Return Retry-After header when possible
- Log rejections with correlation and tenant for analytics
- Start conservative and increase limits as needed

---

## Pitfalls

- Partitioning only by IP can punish NATed clients; prefer user/tenant when available
- Not enabling AutoReplenishment can lead to unexpected limiter behavior

---

Last Updated: 2025-01-08
Maintained By: Idevs Framework Team
License: MIT