# Guide 3: Middleware Pipeline

Phase: 4 - Web/API Layer
Component: Middleware Pipeline
Depends On: 01-Base Controllers, Phase 3 behaviors (validation, authorization)

---

## Overview

The ASP.NET Core middleware pipeline composes cross-cutting concerns for every HTTP request. This guide provides production-ready middleware components for:
- Correlation IDs
- Global exception handling (ProblemDetails)
- Request logging and latency metrics
- Tenant resolution
- Pipeline ordering and registration helpers

All middleware is implemented as lightweight, async-first components with DI support and clear logging.

---

## Pipeline Design & Order

Recommended order in Program.cs:

```csharp
// Program.cs: middleware order (top → bottom)
app.UseCorrelationId();        // 1. Set/propagate correlation ID
app.UseGlobalException();      // 2. Convert exceptions to ProblemDetails
app.UseHttpsRedirection();     // 3. Security baseline
app.UseRouting();              // 4. Enable endpoint routing
app.UseCors();                 // 5. CORS policy (if needed)
app.UseAuthentication();       // 6. AuthN
app.UseAuthorization();        // 7. AuthZ
app.UseTenantResolution();     // 8. Resolve tenant context
// app.UseRateLimiter();       // 9. Rate limiting (Guide 8)
app.MapControllers();          // 10. Map endpoints
```

---

## Correlation ID Middleware

### Purpose
- Ensure every request/response pair carries a unique correlation ID
- Surface correlation ID in logs and downstream calls

### Implementation

```csharp
using System.Diagnostics;

namespace Idevs.Web.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using var activity = new Activity("http.request").Start();
        activity?.AddTag("correlation.id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object>{{"CorrelationId", correlationId}}))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
```

---

## Global Exception Handling Middleware

### Goals
- Never leak unhandled exceptions
- Normalize error responses to RFC 7807 ProblemDetails
- Preserve correlation/trace identifiers

### Implementation

```csharp
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Idevs.Web.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client aborted or request cancelled; do not treat as server error
            _logger.LogWarning("Request was cancelled by client.");
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest; // NGINX convention
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetailsAsync(context, ex);
        }
    }

    private static Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var status = StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = exception.Message,
            Status = status,
            Instance = context.Request.Path,
            Type = "about:blank"
        };

        // Attach trace/correlation identifiers
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlation))
        {
            problem.Extensions["correlationId"] = correlation?.ToString();
        }

        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(problem);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalException(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
```

---

## Request Logging and Timing Middleware

- Structured logs including method, path, status, duration, and correlation ID
- Helps build SLOs and track performance regressions

```csharp
using System.Diagnostics;

namespace Idevs.Web.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = ValueStopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        string? correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var c)
            ? c?.ToString()
            : null;

        try
        {
            await _next(context);
            _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms {CorrelationId}",
                method, path, context.Response.StatusCode, sw.ElapsedMilliseconds, correlationId);
        }
        catch
        {
            _logger.LogError("HTTP {Method} {Path} failed in {Elapsed}ms {CorrelationId}",
                method, path, sw.ElapsedMilliseconds, correlationId);
            throw;
        }
    }
}

internal readonly struct ValueStopwatch
{
    private static readonly double TimestampToMilliseconds = 1000.0 / Stopwatch.Frequency;
    private readonly long _start;

    private ValueStopwatch(long start) => _start = start;

    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());
    public long ElapsedMilliseconds => (long)((Stopwatch.GetTimestamp() - _start) * TimestampToMilliseconds);
}

public static class RequestLoggingExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();
}
```

---

## Tenant Resolution Middleware

Resolves the current tenant from headers or claims and stores it in a request-scoped context.

```csharp
namespace Idevs.Web.MultiTenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    void SetTenantId(Guid id);
}

public sealed class TenantContext : ITenantContext
{
    private Guid? _tenantId;
    public Guid? TenantId => _tenantId;
    public void SetTenantId(Guid id) => _tenantId = id;
}

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private const string TenantHeader = "X-Tenant-ID";

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        string? tenant = context.Request.Headers[TenantHeader].FirstOrDefault()
            ?? context.User.FindFirst("tenant_id")?.Value;

        if (Guid.TryParse(tenant, out var tenantId))
        {
            tenantContext.SetTenantId(tenantId);
            _logger.LogDebug("Resolved tenant {TenantId}", tenantId);
        }
        else if (!string.IsNullOrWhiteSpace(tenant))
        {
            _logger.LogWarning("Invalid tenant header value: {Value}", tenant);
        }

        await _next(context);
    }
}

public static class TenantResolutionExtensions
{
    public static IServiceCollection AddTenantContext(this IServiceCollection services)
        => services.AddScoped<ITenantContext, TenantContext>();

    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
        => app.UseMiddleware<TenantResolutionMiddleware>();
}
```

---

## Registration Snippets (Program.cs)

```csharp
// Services
builder.Services.AddTenantContext();

// Pipeline
app.UseCorrelationId();
app.UseGlobalException();
app.UseRequestLogging();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantResolution();
```

---

## Testing the Middleware

### Unit tests with TestServer

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class MiddlewareTests
{
    [Fact]
    public async Task CorrelationId_Is_Returned_In_Response_Header()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(s => s.AddLogging())
            .Configure(app =>
            {
                app.UseCorrelationId();
                app.Run(ctx => ctx.Response.WriteAsync("ok"));
            });

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var response = await client.GetAsync("/");

        response.Headers.TryGetValues("X-Correlation-ID", out var values).ShouldBeTrue();
        values!.First().ShouldNotBeNullOrWhiteSpace();
    }
}
```

---

## Best Practices

- Keep middleware focused; do one thing well
- Ensure ordering is explicit and documented
- Avoid blocking calls; always use async
- Do not swallow exceptions; centralize in global handler
- Add structured logging for observability (include correlation and tenant)

---

## Common Pitfalls

- Placing authentication/authorization before exception handling can leak raw exceptions
- Missing correlation propagation leads to difficult tracing across services
- Tenant resolution after authorization can cause policy mis-evaluations

---

Last Updated: 2025-01-08
Maintained By: Idevs Framework Team
License: MIT