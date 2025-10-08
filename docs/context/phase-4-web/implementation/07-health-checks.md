# Guide 7: Health Checks

Phase: 4 - Web/API Layer
Component: Health Checks
Depends On: Middleware Pipeline

---

## Overview

Health checks expose application status for orchestrators and load balancers.
- Liveness (/health/live): Is the process running?
- Readiness (/health/ready): Can the app serve traffic (dependencies OK)?
- General (/health): Detailed JSON for observability

---

## Packages

Most features come from Microsoft.Extensions.Diagnostics.HealthChecks, included in ASP.NET Core. For advanced providers, add specific packages (e.g., Redis, Npgsql).

---

## Configuration

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
    .AddDbContextCheck<ApplicationDbContext>(name: "database", tags: new[] {"ready"});

// Map endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // If the app responds, it's live
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteJsonResponse
});

static Task WriteJsonResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var json = System.Text.Json.JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.TotalMilliseconds,
            tags = e.Value.Tags
        })
    });
    return context.Response.WriteAsync(json);
}
```

---

## Custom Health Check Example

```csharp
public sealed class ExternalApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _client;
    public ExternalApiHealthCheck(HttpClient client) => _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetAsync("/status", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("External API OK")
                : HealthCheckResult.Unhealthy($"External API returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("External API unreachable", ex);
        }
    }
}
```

Register with tags for readiness:

```csharp
builder.Services.AddHttpClient<ExternalApiHealthCheck>(c => c.BaseAddress = new Uri("https://external-api"));
builder.Services.AddHealthChecks().AddCheck<ExternalApiHealthCheck>("external-api", tags: new[]{"ready"});
```

---

## Best Practices

- Keep liveness simple; avoid dependencies
- Tag dependency checks with "ready" and expose via /health/ready
- Return JSON for /health for easy ingestion
- Timeouts: ensure health checks complete quickly (<2s) to avoid cascading failures

---

## Pitfalls

- Long-running checks in liveness endpoints cause false negatives
- Missing tags result in noisy readiness probe data

---

Last Updated: 2025-01-08
Maintained By: Idevs Framework Team
License: MIT