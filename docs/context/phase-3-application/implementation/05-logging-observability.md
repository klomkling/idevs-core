# Guide 5: Logging & Observability

**Phase**: 3 - Application Layer  
**Component**: Logging Behavior & Observability  
**Prerequisites**: Guide 4 (Authorization)  
**Estimated Time**: 2 hours

---

## Table of Contents

1. [Overview](#overview)
2. [Structured Logging](#structured-logging)
3. [Correlation IDs](#correlation-ids)
4. [Performance Metrics](#performance-metrics)
5. [Logging Behavior Implementation](#logging-behavior-implementation)
6. [Best Practices](#best-practices)
7. [Next Steps](#next-steps)

---

## Overview

Effective logging and observability enable debugging, monitoring, and troubleshooting in production. Key features:

- **Structured logging**: Machine-readable log entries
- **Correlation IDs**: Track requests across services
- **Performance metrics**: Monitor handler execution time
- **Context enrichment**: Include user, tenant, request details
- **Log levels**: Appropriate verbosity for different scenarios

---

## Structured Logging

### ILogger Configuration

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Handling {RequestName} ({CorrelationId})",
            requestName,
            correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} ({CorrelationId}) in {ElapsedMs}ms",
                requestName,
                correlationId,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error handling {RequestName} ({CorrelationId}) after {ElapsedMs}ms",
                requestName,
                correlationId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
```

---

## Correlation IDs

### HTTP Context Integration

```csharp
public interface ICorrelationIdService
{
    string GetCorrelationId();
    void SetCorrelationId(string correlationId);
}

public sealed class CorrelationIdService : ICorrelationIdService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        
        if (context?.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) == true)
            return correlationId.ToString();

        var newCorrelationId = Guid.NewGuid().ToString();
        context?.Response.Headers.Add(CorrelationIdHeader, newCorrelationId);
        
        return newCorrelationId;
    }

    public void SetCorrelationId(string correlationId)
    {
        var context = _httpContextAccessor.HttpContext;
        context?.Response.Headers.Add(CorrelationIdHeader, correlationId);
    }
}
```

### Enhanced Logging Behavior with Correlation

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICorrelationIdService correlationIdService,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = _correlationIdService.GetCorrelationId();
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId ?? Guid.Empty,
            ["TenantId"] = tenantId ?? Guid.Empty,
            ["RequestName"] = requestName
        }))
        {
            _logger.LogInformation(
                "Executing {RequestName}",
                requestName);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next();
                stopwatch.Stop();

                var logLevel = stopwatch.ElapsedMilliseconds > 1000 
                    ? LogLevel.Warning 
                    : LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "Executed {RequestName} in {ElapsedMs}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Failed executing {RequestName} after {ElapsedMs}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
```

---

## Performance Metrics

### Performance Monitoring Behavior

```csharp
public sealed class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly IMetricsService _metricsService;
    private const int SlowRequestThresholdMs = 500;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        IMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Record metrics
        _metricsService.RecordHandlerDuration(requestName, elapsedMs);

        // Log slow requests
        if (elapsedMs > SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request detected: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                requestName,
                elapsedMs,
                SlowRequestThresholdMs);
        }

        return response;
    }
}
```

---

## Best Practices

### ✅ DO

1. **Use structured logging**
   ```csharp
   _logger.LogInformation(
       "User {UserId} created order {OrderId}",
       userId, orderId);
   ```

2. **Include correlation IDs**
   ```csharp
   using (_logger.BeginScope(new { CorrelationId = correlationId }))
   ```

3. **Log appropriate levels**
   ```csharp
   LogDebug: Development details
   LogInformation: Normal operations
   LogWarning: Unusual but handled
   LogError: Failures requiring attention
   LogCritical: System-wide failures
   ```

4. **Measure performance**
   ```csharp
   var stopwatch = Stopwatch.StartNew();
   // ... operation
   _logger.LogInformation("Operation took {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
   ```

5. **Use scopes for context**
   ```csharp
   using (_logger.BeginScope(new { TenantId, UserId }))
   {
       // All logs in scope include context
   }
   ```

---

### ❌ DON'T

1. **Don't log sensitive data**
   ```csharp
   _logger.LogInformation("Password: {Password}", password); // ❌ Never!
   ```

2. **Don't over-log**
   ```csharp
   // ❌ Bad: Logs on every iteration
   foreach (var item in items)
       _logger.LogDebug("Processing {Item}", item);
   ```

3. **Don't use string interpolation in log messages**
   ```csharp
   _logger.LogInformation($"User {userId} logged in"); // ❌ Bad
   _logger.LogInformation("User {UserId} logged in", userId); // ✅ Good
   ```

---

## Next Steps

1. **Read Guide 6**: Transaction Management (`06-TRANSACTION-MANAGEMENT.md`)
2. **Implement LoggingBehavior**: Add to pipeline
3. **Configure Serilog or NLog**: Set up structured logging
4. **Add Application Insights**: For production monitoring
5. **Set up dashboards**: Visualize metrics and logs

---

**Summary**: Effective logging with correlation IDs, performance metrics, and structured data enables debugging and monitoring in production. Always include relevant context and never log sensitive information.

---

**Last Updated**: 2025-01-08  
**Next Guide**: 06-TRANSACTION-MANAGEMENT.md
