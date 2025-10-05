# Observability Blueprint

**Document Owner**: Platform Engineering Team  
**Last Updated**: 2025-10-04  
**Status**: Baseline Established

## Purpose

This document defines the observability strategy for the **Idevs** framework (repo: `idevs-core`), covering structured logging, distributed tracing, and metrics collection. It ensures the framework provides comprehensive visibility into system behavior, performance, and health across all tenants.

## Observability Pillars

### The Three Pillars

```
┌─────────────────────────────────────────────────────────────────┐
│                    OBSERVABILITY PILLARS                        │
├─────────────────────┬───────────────────┬───────────────────────┤
│     LOGS            │      METRICS      │      TRACES           │
│  (What happened)    │  (How much/fast)  │  (How it flows)       │
├─────────────────────┼───────────────────┼───────────────────────┤
│ • Structured events │ • Counters        │ • Request flows       │
│ • Debug context     │ • Gauges          │ • Timing breakdown    │
│ • Error details     │ • Histograms      │ • Dependencies        │
│ • Audit trail       │ • Summaries       │ • Critical path       │
└─────────────────────┴───────────────────┴───────────────────────┘
```

### Correlation Strategy

All three pillars are correlated via:
- **Correlation ID**: Unique per request, propagated across all components
- **Tenant ID**: Multi-tenant context for filtering and isolation
- **User ID**: Actor identification (when authenticated)
- **Trace ID**: Distributed tracing identifier (OpenTelemetry)
- **Span ID**: Individual operation within a trace

## Logging Strategy

### Structured Logging with Serilog

**Philosophy**: Treat logs as structured data, not text.

```csharp
// ❌ Avoid string interpolation
Log.Information($"Order {orderId} created for tenant {tenantId}");

// ✅ Use structured properties
Log.Information(
    "Order created for tenant {TenantId} with {OrderId}", 
    tenantId, 
    orderId
);
```

### Minimum Enrichment Requirements

All log entries must include:

```csharp
{
    "Timestamp": "2025-10-04T09:00:00.123Z",        // UTC timestamp
    "Level": "Information",                          // Log level
    "MessageTemplate": "Order created",              // Template
    "Properties": {
        "CorrelationId": "abc-123",                 // Request correlation
        "TenantId": "tenant-uuid",                  // Multi-tenant context
        "UserId": "user-uuid",                      // Actor (if authenticated)
        "Environment": "Production",                 // Deployment environment
        "MachineName": "web-01",                    // Instance identifier
        "Application": "Idevs",                     // Application name
        "Version": "1.2.3"                          // Semantic version
    }
}
```

### Log Levels

| Level | When to Use | Example | Typical Volume |
|-------|-------------|---------|----------------|
| **Trace** | Very detailed debugging | Method entry/exit with parameters | High (dev only) |
| **Debug** | Diagnostic information | Query parameters, cache hits | Medium (dev/staging) |
| **Information** | General flow tracking | Command executed, query completed | Low-Medium |
| **Warning** | Unexpected but recoverable | Retryable failure, fallback used | Low |
| **Error** | Operation failed | Unhandled exception, validation failure | Very Low |
| **Critical** | System-level failure | Database unreachable, service down | Rare |

### Event IDs

Use consistent event IDs for categorization:

```csharp
public static class LogEvents
{
    // Authentication: 1000-1099
    public const int UserAuthenticated = 1000;
    public const int UserAuthenticationFailed = 1001;
    public const int TokenExpired = 1002;
    
    // Commands: 2000-2099
    public const int CommandExecuting = 2000;
    public const int CommandExecuted = 2001;
    public const int CommandFailed = 2002;
    public const int CommandValidationFailed = 2003;
    
    // Queries: 3000-3099
    public const int QueryExecuting = 3000;
    public const int QueryExecuted = 3001;
    public const int QueryFailed = 3002;
    
    // Multi-tenancy: 4000-4099
    public const int TenantResolved = 4000;
    public const int TenantResolutionFailed = 4001;
    public const int CrossTenantAccessAttempt = 4002;
    
    // Performance: 5000-5099
    public const int SlowQuery = 5000;
    public const int HighMemoryUsage = 5001;
    public const int CircuitBreakerOpened = 5002;
}

// Usage
Log.Information(
    LogEvents.CommandExecuted, 
    "Command {CommandType} executed in {Duration}ms", 
    commandType, 
    duration
);
```

### PII Redaction Policy

**Rule**: Never log Personally Identifiable Information (PII) in plain text.

#### Automatic Redaction

```csharp
public class PiiRedactionEnricher : ILogEventEnricher
{
    private static readonly Regex[] PiiPatterns =
    [
        new(@"\b[\w\.-]+@[\w\.-]+\.\w+\b"),                    // Email
        new(@"\b\d{3}-\d{2}-\d{4}\b"),                         // SSN
        new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b"),  // Credit card
        new(@"\b\d{3}-\d{3}-\d{4}\b")                          // Phone (US)
    ];
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var message = logEvent.RenderMessage();
        
        foreach (var pattern in PiiPatterns)
        {
            message = pattern.Replace(message, "[REDACTED]");
        }
        
        logEvent.AddOrUpdateProperty(
            factory.CreateProperty("SafeMessage", message));
    }
}
```

#### Manual Masking for Known PII Fields

```csharp
public static class PiiMask
{
    public static string Email(string email) 
        => email?[..3] + "***@***" + email?[email.LastIndexOf('.')..];
    
    public static string CreditCard(string cc) 
        => "****-****-****-" + cc?[^4..];
    
    public static string Phone(string phone) 
        => "***-***-" + phone?[^4..];
}

// Usage
Log.Information(
    "Payment processed for {MaskedEmail} using {MaskedCard}", 
    PiiMask.Email(email), 
    PiiMask.CreditCard(cardNumber)
);
```

### Serilog Configuration

```csharp
public static IHostBuilder ConfigureLogging(this IHostBuilder host)
{
    return host.UseSerilog((context, services, config) =>
    {
        config
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "Idevs")
            .Enrich.WithProperty("Version", GetVersion())
            .Enrich.With<CorrelationIdEnricher>()
            .Enrich.With<TenantEnricher>()
            .Enrich.With<PiiRedactionEnricher>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/idevs-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                formatter: new JsonFormatter())
            .WriteTo.Seq(
                serverUrl: context.Configuration["Seq:ServerUrl"])
            .WriteTo.ApplicationInsights(
                services.GetRequiredService<TelemetryConfiguration>(),
                TelemetryConverter.Traces);
    });
}
```

## Distributed Tracing Strategy

### OpenTelemetry Integration

**Standard**: W3C Trace Context for trace propagation

```
Traceparent: 00-{trace-id}-{span-id}-{flags}
Example: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
```

### Trace Hierarchy

```
Trace: HTTP Request (trace-id: abc-123)
├─ Span: TenantResolutionMiddleware (10ms)
├─ Span: AuthenticationMiddleware (15ms)
├─ Span: CreateOrderCommand (120ms)
│  ├─ Span: ValidateOrder (5ms)
│  ├─ Span: GetCustomer Query (25ms)
│  │  └─ Span: Database Query (20ms)
│  ├─ Span: CheckInventory Query (30ms)
│  │  ├─ Span: Database Query (15ms)
│  │  └─ Span: Cache Lookup (2ms)
│  └─ Span: SaveOrder (60ms)
│     ├─ Span: Database Insert (40ms)
│     └─ Span: Audit Log (15ms)
└─ Span: Response Serialization (5ms)

Total Duration: 150ms
```

### Automatic Instrumentation

```csharp
public static IServiceCollection AddObservability(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.AddOpenTelemetry()
        .WithTracing(builder =>
        {
            builder
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("tenant.id", request.Headers["X-Tenant-Id"]);
                        activity.SetTag("correlation.id", request.Headers["X-Correlation-Id"]);
                    };
                })
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.tenant", GetTenantFromContext());
                    };
                })
                .AddHttpClientInstrumentation()
                .AddRedisInstrumentation()
                .AddSource("Idevs")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(configuration["Otlp:Endpoint"]);
                });
        });
    
    return services;
}
```

### Manual Span Creation

```csharp
public class OrderCommandHandler
{
    private static readonly ActivitySource ActivitySource = new("Idevs");
    
    public async Task<Result> HandleAsync(CreateOrderCommand command)
    {
        using var activity = ActivitySource.StartActivity(
            "CreateOrder", 
            ActivityKind.Internal);
        
        activity?.SetTag("order.id", command.OrderId);
        activity?.SetTag("tenant.id", _tenantContext.Id);
        activity?.SetTag("customer.id", command.CustomerId);
        
        try
        {
            var result = await ProcessOrderAsync(command);
            
            activity?.SetTag("order.status", result.Status);
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Trace Sampling Strategy

```csharp
// Always sample errors and slow requests
public class AdaptiveSampler : Sampler
{
    private readonly double _baseSamplingRate = 0.1; // 10% baseline
    
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Always sample if error
        if (samplingParameters.Tags.Any(t => t.Key == "error" && (bool)t.Value))
            return new SamplingResult(SamplingDecision.RecordAndSample);
        
        // Always sample if slow (>1s)
        if (samplingParameters.Tags.Any(t => t.Key == "duration" && (long)t.Value > 1000))
            return new SamplingResult(SamplingDecision.RecordAndSample);
        
        // Otherwise, probabilistic sampling
        return Random.Shared.NextDouble() < _baseSamplingRate
            ? new SamplingResult(SamplingDecision.RecordAndSample)
            : new SamplingResult(SamplingDecision.Drop);
    }
}
```

## Metrics Strategy

### RED Metrics (Request Rate, Error Rate, Duration)

**Service-Level Metrics** for every command and query handler:

```csharp
public static class Metrics
{
    private static readonly Meter Meter = new("Idevs", "1.0.0");
    
    // Request Rate
    public static readonly Counter<long> CommandCount = Meter.CreateCounter<long>(
        "commands.executed",
        description: "Total number of commands executed");
    
    public static readonly Counter<long> QueryCount = Meter.CreateCounter<long>(
        "queries.executed",
        description: "Total number of queries executed");
    
    // Error Rate
    public static readonly Counter<long> CommandErrors = Meter.CreateCounter<long>(
        "commands.failed",
        description: "Total number of command failures");
    
    public static readonly Counter<long> QueryErrors = Meter.CreateCounter<long>(
        "queries.failed",
        description: "Total number of query failures");
    
    // Duration (Histogram for percentiles)
    public static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        "commands.duration",
        unit: "ms",
        description: "Command execution duration");
    
    public static readonly Histogram<double> QueryDuration = Meter.CreateHistogram<double>(
        "queries.duration",
        unit: "ms",
        description: "Query execution duration");
}

// Usage in decorator
public class MetricsCommandHandler<T> : ICommandHandler<T>
{
    private readonly ICommandHandler<T> _inner;
    private readonly ITenantContext _tenantContext;
    
    public async Task<Result> HandleAsync(T command)
    {
        var tags = new TagList
        {
            { "command.type", typeof(T).Name },
            { "tenant.id", _tenantContext.Id }
        };
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _inner.HandleAsync(command);
            
            stopwatch.Stop();
            
            tags.Add("result", result.IsSuccess ? "success" : "failure");
            
            Metrics.CommandCount.Add(1, tags);
            Metrics.CommandDuration.Record(stopwatch.ElapsedMilliseconds, tags);
            
            if (result.IsFailure)
            {
                Metrics.CommandErrors.Add(1, tags);
            }
            
            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            tags.Add("result", "error");
            
            Metrics.CommandCount.Add(1, tags);
            Metrics.CommandDuration.Record(stopwatch.ElapsedMilliseconds, tags);
            Metrics.CommandErrors.Add(1, tags);
            
            throw;
        }
    }
}
```

### Domain Metrics

```csharp
// Business-specific metrics
public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>(
    "orders.created",
    description: "Total orders created");

public static readonly Histogram<decimal> OrderValue = Meter.CreateHistogram<decimal>(
    "orders.value",
    unit: "USD",
    description: "Order monetary value");

public static readonly Gauge<int> ActiveTenants = Meter.CreateObservableGauge(
    "tenants.active",
    () => GetActiveTenantCount(),
    description: "Number of active tenants");

public static readonly Counter<long> CacheHits = Meter.CreateCounter<long>(
    "cache.hits",
    description: "Cache hit count");

public static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>(
    "cache.misses",
    description: "Cache miss count");
```

### Resource Metrics

```csharp
// Infrastructure metrics
public static readonly Histogram<double> DbQueryDuration = Meter.CreateHistogram<double>(
    "db.query.duration",
    unit: "ms");

public static readonly Counter<long> DbConnections = Meter.CreateCounter<long>(
    "db.connections.opened");

public static readonly Gauge<int> DbConnectionPoolSize = Meter.CreateObservableGauge(
    "db.connection_pool.size",
    () => GetConnectionPoolSize());

public static readonly Gauge<long> MemoryUsage = Meter.CreateObservableGauge(
    "process.memory.bytes",
    () => GC.GetTotalMemory(false));
```

## Dashboards & Alerts

### Overview

Effective observability requires more than just collecting metrics—you need **actionable insights** through well-designed dashboards and **proactive notification** through intelligent alerting. This section provides detailed guidance on building production-ready monitoring.

### Dashboard Design Philosophy

#### The Golden Signals

Every dashboard should answer these four questions:

1. **Latency**: How long does it take to serve a request?
2. **Traffic**: How much demand is on the system?
3. **Errors**: What is the rate of failed requests?
4. **Saturation**: How full is the service?

#### Dashboard Hierarchy

```
┌──────────────────────────────────────────────────────────────┐
│ Level 1: EXECUTIVE DASHBOARD                                 │
│ • System-wide health status (red/yellow/green)               │
│ • SLO compliance percentage                                  │
│ • Error budget remaining                                     │
│ • Active incidents count                                     │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│ Level 2: SERVICE HEALTH DASHBOARD (Operations Team)         │
│ • Request rate, error rate, latency percentiles              │
│ • Resource utilization (CPU, memory, connections)            │
│ • Top errors by frequency                                    │
│ • Deployment timeline overlay                                │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│ Level 3: COMPONENT DASHBOARDS (On-Call Engineers)           │
│ • Multi-tenant: per-tenant metrics and anomalies             │
│ • Database: query performance, connection pools              │
│ • Cache: hit/miss ratios, eviction rates                     │
│ • Background Jobs: queue depth, processing time              │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│ Level 4: DEBUG DASHBOARDS (Developers)                      │
│ • Command/Query execution breakdown                          │
│ • Trace visualization and flame graphs                       │
│ • Detailed logs with filtering                               │
│ • Correlation of metrics, traces, and logs                   │
└──────────────────────────────────────────────────────────────┘
```

### Essential Dashboards

---

#### 1. Service Health Dashboard

**Purpose**: Primary operational view for on-call engineers to quickly assess system health.

**Target Audience**: Operations team, DevOps, SREs

**Refresh Rate**: 10-30 seconds

##### Panels Layout

```
┌─────────────────────────────────────────────────────────────┐
│ SERVICE HEALTH: Idevs Framework               [Last 1 hour] │
├───────────────────────┬───────────────────────┬─────────────┤
│ Requests/sec          │ Error Rate %          │ P99 Latency │
│ [Graph: line chart]   │ [Graph: line + gauge] │ [Graph]     │
│ Current: 1,234        │ Current: 0.03%        │ 95ms        │
│ Trend: ↑ 5%          │ Threshold: <0.1%      │ SLO: <150ms │
├───────────────────────┴───────────────────────┴─────────────┤
│ Request Latency Distribution                   [Histogram]  │
│ P50: 45ms | P90: 78ms | P95: 95ms | P99: 122ms             │
├─────────────────────────────────────────────────────────────┤
│ Top 5 Commands by Volume    │ Top 5 Slowest Operations     │
│ 1. CreateOrder (40%)         │ 1. GenerateReport (850ms)    │
│ 2. GetCustomer (25%)         │ 2. BulkImport (620ms)        │
│ 3. UpdateInventory (15%)     │ 3. ComplexQuery (340ms)      │
├──────────────────────────────┴──────────────────────────────┤
│ Recent Errors (Last 5 minutes)                [Table]       │
│ Time     | Tenant    | Command        | Error               │
│ 09:22:15 | acme-corp | CreateOrder    | ValidationFailed    │
│ 09:21:03 | xyz-inc   | UpdatePayment  | PaymentGatewayError │
└─────────────────────────────────────────────────────────────┘
```

##### Queries (Prometheus/Grafana)

**Request Rate (Time Series)**:
```promql
# Total commands per second
sum(rate(commands_executed_total[5m]))

# Broken down by command type
sum by (command_type) (rate(commands_executed_total[5m]))
```

**Error Rate Percentage (Gauge + Graph)**:
```promql
# Current error rate
(
  sum(rate(commands_failed_total[5m])) 
  / 
  sum(rate(commands_executed_total[5m]))
) * 100

# Error rate with threshold line at 0.1%
# Use Grafana threshold visualization: green <0.05%, yellow 0.05-0.1%, red >0.1%
```

**Latency Percentiles (Multi-line Graph)**:
```promql
# P50 latency
histogram_quantile(0.50, sum by (le) (rate(commands_duration_bucket[5m])))

# P90 latency
histogram_quantile(0.90, sum by (le) (rate(commands_duration_bucket[5m])))

# P95 latency
histogram_quantile(0.95, sum by (le) (rate(commands_duration_bucket[5m])))

# P99 latency (critical)
histogram_quantile(0.99, sum by (le) (rate(commands_duration_bucket[5m])))
```

**Top Commands by Volume (Table)**:
```promql
# Top 10 most executed commands
topk(10, sum by (command_type) (rate(commands_executed_total[5m])))
```

**Slowest Operations (Table)**:
```promql
# Average duration by command type (only >100ms)
topk(10, 
  sum by (command_type) (
    rate(commands_duration_sum[5m])
  ) 
  / 
  sum by (command_type) (
    rate(commands_duration_count[5m])
  )
) > 100
```

**Active Connections**:
```promql
# Current active HTTP connections
sum(aspnetcore_connections_active)

# Database connection pool size
sum(db_connection_pool_size)
```

**Memory Usage (Gauge)**:
```promql
# Current process memory in MB
process_memory_bytes / 1024 / 1024

# GC pressure (collections per second)
rate(dotnet_gc_collections_total[1m])
```

---

#### 2. Multi-Tenant Dashboard

**Purpose**: Monitor tenant-specific behavior, detect anomalies, enforce fair-use policies.

**Target Audience**: Product managers, customer success, operations

**Key Concerns**:
- Noisy neighbor detection
- Tenant-specific performance degradation
- Usage patterns for billing
- Compliance with tenant SLAs

##### Panels Layout

```
┌─────────────────────────────────────────────────────────────┐
│ MULTI-TENANT INSIGHTS                         [Last 6 hours]│
├─────────────────────────────────────────────────────────────┤
│ Active Tenants: 247        │ Total Requests: 1.2M           │
│ Tenants >SLA: 3 ⚠️         │ Avg Latency by Tier:           │
│                            │   Enterprise: 42ms             │
│                            │   Professional: 58ms           │
│                            │   Basic: 95ms                  │
├────────────────────────────┴────────────────────────────────┤
│ Top 10 Tenants by Request Volume              [Bar Chart]   │
│ acme-corp    ████████████████████████████████████ 125k      │
│ globex       ███████████████████████ 78k                    │
│ initech      ███████████████ 52k                            │
├─────────────────────────────────────────────────────────────┤
│ Tenants with Elevated Error Rates (>1%)       [Table]       │
│ Tenant ID    | Tier        | Error Rate | Last Error        │
│ xyz-inc      | Enterprise  | 2.3%       | PaymentTimeout    │
│ startup-co   | Basic       | 1.8%       | RateLimitExceeded │
├─────────────────────────────────────────────────────────────┤
│ Request Latency by Tenant Tier                [Heatmap]     │
│ Shows P99 latency distribution across tenant tiers          │
└─────────────────────────────────────────────────────────────┘
```

##### Queries (Prometheus/Grafana)

**Top Tenants by Request Volume**:
```promql
# Top 10 most active tenants (last 1 hour)
topk(10, sum by (tenant_id) (rate(commands_executed_total[1h])))

# With tenant names (requires join with metadata)
topk(10, 
  sum by (tenant_id, tenant_name) (
    rate(commands_executed_total[1h])
  )
)
```

**Error Rate by Tenant**:
```promql
# Tenants with error rate > 1%
(
  sum by (tenant_id) (rate(commands_failed_total[5m])) 
  /
  sum by (tenant_id) (rate(commands_executed_total[5m]))
) > 0.01

# Sort by highest error rate
topk(20, 
  sum by (tenant_id) (rate(commands_failed_total[5m])) 
  /
  sum by (tenant_id) (rate(commands_executed_total[5m]))
)
```

**Resource Consumption by Tenant**:
```promql
# Database query time per tenant (in seconds)
sum by (tenant_id) (rate(db_query_duration_sum{tenant_id!=""}[5m]))

# Memory allocation per tenant (if instrumented)
sum by (tenant_id) (rate(tenant_memory_allocated_bytes[5m]))
```

**Latency Percentiles by Tenant Tier**:
```promql
# P99 latency for Enterprise tier
histogram_quantile(
  0.99,
  sum by (le) (
    rate(commands_duration_bucket{tenant_tier="enterprise"}[5m])
  )
)

# P99 latency for Basic tier
histogram_quantile(
  0.99,
  sum by (le) (
    rate(commands_duration_bucket{tenant_tier="basic"}[5m])
  )
)
```

**Noisy Neighbor Detection**:
```promql
# Tenants consuming >10% of total database time
(
  sum by (tenant_id) (rate(db_query_duration_sum[5m]))
  /
  sum(rate(db_query_duration_sum[5m]))
) > 0.1

# Alert if single tenant exceeds 20% of resources
```

---

#### 3. Database Performance Dashboard

**Purpose**: Identify database bottlenecks, slow queries, connection issues.

**Target Audience**: DBAs, backend engineers, performance engineers

##### Panels Layout

```
┌─────────────────────────────────────────────────────────────┐
│ DATABASE PERFORMANCE: PostgreSQL              [Last 1 hour] │
├───────────────────────┬─────────────────────────────────────┤
│ Connection Pool       │ Query Performance                   │
│ Active: 45/100        │ P95: 25ms   P99: 78ms              │
│ Waiting: 2            │ Slow queries (>100ms): 12           │
│ [Graph: gauge]        │ [Graph: histogram]                  │
├───────────────────────┴─────────────────────────────────────┤
│ Queries per Second                             [Line Chart] │
│ SELECT: 850/s  │  INSERT: 120/s  │  UPDATE: 80/s           │
├─────────────────────────────────────────────────────────────┤
│ Top 10 Slowest Queries (Avg Duration)         [Table]       │
│ Query                               | Avg  | Count | P99    │
│ SELECT * FROM orders WHERE...       | 250ms| 1,234 | 850ms  │
│ UPDATE inventory SET qty =...       | 180ms| 567   | 420ms  │
├─────────────────────────────────────────────────────────────┤
│ Deadlocks & Lock Waits                        [Time Series] │
│ Deadlocks: 0 (last hour) ✓                                  │
│ Lock wait time: P95 = 5ms                                   │
└─────────────────────────────────────────────────────────────┘
```

##### Queries (Prometheus)

**Connection Pool Utilization**:
```promql
# Current active connections
db_connection_pool_active

# Max pool size
db_connection_pool_size

# Utilization percentage
(db_connection_pool_active / db_connection_pool_size) * 100

# Alert if >80% for 5+ minutes
```

**Query Duration Percentiles**:
```promql
# P50, P95, P99 of all queries
histogram_quantile(0.50, rate(db_query_duration_bucket[5m]))
histogram_quantile(0.95, rate(db_query_duration_bucket[5m]))
histogram_quantile(0.99, rate(db_query_duration_bucket[5m]))
```

**Slow Query Count**:
```promql
# Queries taking longer than 100ms
sum(rate(db_query_duration_bucket{le="100"}[5m])) 
- 
sum(rate(db_query_duration_bucket{le="+Inf"}[5m]))

# Or using custom metric:
sum(rate(db_slow_queries_total[5m]))
```

**Query Rate by Operation**:
```promql
# Queries per second by type
sum by (operation) (rate(db_queries_total[5m]))

# Where operation in [SELECT, INSERT, UPDATE, DELETE]
```

**Deadlock Detection** (requires custom instrumentation):
```promql
# Deadlock events
increase(db_deadlocks_total[1h])

# Lock wait time P95
histogram_quantile(0.95, rate(db_lock_wait_duration_bucket[5m]))
```

---

#### 4. Business Metrics Dashboard

**Purpose**: Track domain-specific KPIs that matter to the business.

**Target Audience**: Product owners, business stakeholders

##### Example Panels

```promql
# Orders created per hour
sum(increase(orders_created_total[1h]))

# Total order value (revenue proxy)
sum(rate(orders_value_sum[1h]))

# Average order value
sum(rate(orders_value_sum[1h])) / sum(rate(orders_value_count[1h]))

# Cache effectiveness
(
  sum(rate(cache_hits_total[5m])) 
  / 
  (sum(rate(cache_hits_total[5m])) + sum(rate(cache_misses_total[5m])))
) * 100
```

---

### Alert Rules

#### Alert Design Principles

**Rule #1: Alerts Should Be Actionable**  
Every alert must have a clear response action. If you can't define what to do when the alert fires, don't create it.

**Rule #2: Avoid Alert Fatigue**  
Too many false positives train teams to ignore alerts. Use appropriate thresholds and `for` durations.

**Rule #3: Alert on Symptoms, Not Causes**  
Alert on user-visible problems (high latency, errors) rather than implementation details (high CPU).

**Rule #4: Use Severity Appropriately**
- **Critical**: Page immediately, user-facing outage or data loss risk
- **Warning**: Investigate during business hours, potential issue
- **Info**: For awareness, no action required

#### Alert Severity Matrix

| Severity | Response Time | Channel | Examples |
|----------|---------------|---------|----------|
| **Critical** | Immediate (24/7) | PagerDuty, SMS | Error rate >5%, Database down, Security breach |
| **Warning** | Within 1 hour (business hours) | Slack, Email | Elevated latency, Cache degradation, Disk 80% full |
| **Info** | No action required | Email digest | Deployment completed, SLO budget consumed 50% |

---

#### Critical Alerts (Page Immediately)

These alerts indicate **active user impact** or **imminent system failure**.

##### 1. High Error Rate

```yaml
- alert: HighErrorRate
  expr: |
    (
      sum(rate(commands_failed_total[5m])) 
      / 
      sum(rate(commands_executed_total[5m]))
    ) > 0.05
  for: 2m
  labels:
    severity: critical
    team: backend
    runbook: https://wiki.company.com/runbooks/high-error-rate
  annotations:
    summary: "High error rate: {{ $value | humanizePercentage }}"
    description: |
      Error rate is {{ $value | humanizePercentage }} (threshold: 5%).
      
      Immediate actions:
      1. Check recent deployments (rollback if needed)
      2. Review error logs: `kubectl logs -l app=idevs --tail=100 | grep ERROR`
      3. Check external dependencies (database, payment gateway)
      4. Verify tenant-specific errors vs system-wide
      
      Dashboard: https://grafana.company.com/d/service-health
    dashboard: https://grafana.company.com/d/service-health
```

**Why this threshold?**
- 5% error rate means 1 in 20 requests fail—significant user impact
- `for: 2m` prevents false positives from brief spikes
- Shorter than SLO violation (we want to catch issues before SLO breach)

**Common causes**:
- Recent deployment with bugs
- Database connectivity issues
- External service outage (payment gateway, email provider)
- Configuration error

---

##### 2. High Latency (P99)

```yaml
- alert: HighLatency
  expr: |
    histogram_quantile(
      0.99, 
      sum by (le) (rate(commands_duration_bucket[5m]))
    ) > 1000
  for: 5m
  labels:
    severity: critical
    team: backend
    runbook: https://wiki.company.com/runbooks/high-latency
  annotations:
    summary: "P99 latency is {{ $value }}ms (threshold: 1000ms)"
    description: |
      P99 latency exceeds 1 second. Users are experiencing slow responses.
      
      Immediate actions:
      1. Check for slow database queries: `SELECT * FROM pg_stat_activity WHERE state = 'active' AND query_start < now() - interval '1 second'`
      2. Review distributed traces for slow spans
      3. Check connection pool saturation
      4. Verify no tenant is monopolizing resources (noisy neighbor)
      
      Dashboard: https://grafana.company.com/d/database-performance
```

**Why P99 and not P50?**
- P99 captures the worst user experience (1 in 100 requests)
- P50 can look good while some users have terrible experience
- 1 second is a psychological threshold for user frustration

---

##### 3. Database Connection Pool Exhaustion

```yaml
- alert: DatabaseConnectionPoolExhausted
  expr: |
    (
      db_connection_pool_active 
      / 
      db_connection_pool_size
    ) > 0.9
  for: 3m
  labels:
    severity: critical
    team: backend
  annotations:
    summary: "Database connection pool at {{ $value | humanizePercentage }} capacity"
    description: |
      Connection pool is nearly exhausted. New requests will fail or timeout.
      
      Immediate actions:
      1. Check for connection leaks: `SELECT count(*) FROM pg_stat_activity WHERE state = 'idle in transaction'`
      2. Review long-running queries
      3. Consider increasing pool size (temporary)
      4. Check for missing `using` statements in code
```

---

##### 4. Cross-Tenant Data Access Attempt (SECURITY)

```yaml
- alert: CrossTenantAccessAttempt
  expr: rate(cross_tenant_access_attempts_total[1m]) > 0
  for: 1m
  labels:
    severity: critical
    team: security
    compliance: "REQUIRED"
  annotations:
    summary: "SECURITY: Cross-tenant data access attempted"
    description: |
      A request attempted to access data belonging to another tenant.
      This is a CRITICAL SECURITY VIOLATION.
      
      Immediate actions:
      1. Review audit logs for tenant_id mismatch
      2. Identify the user/API key making the request
      3. Suspend the account if malicious
      4. Notify security team
      5. Check for similar attempts in last 24 hours
      
      Logs: `grep "CrossTenantAccessAttempt" /var/log/idevs/*.log`
```

**Why 1 minute?**
- Security violations require immediate response
- No false positives expected (this should NEVER happen)
- Part of ASVS compliance requirement

---

##### 5. Database Unavailable

```yaml
- alert: DatabaseUnavailable
  expr: up{job="postgresql"} == 0
  for: 1m
  labels:
    severity: critical
    team: infrastructure
  annotations:
    summary: "PostgreSQL database is DOWN"
    description: |
      The PostgreSQL database is unreachable. ALL write operations are failing.
      
      Immediate actions:
      1. Check database server status
      2. Verify network connectivity
      3. Check database logs for crash/OOM
      4. Initiate failover to replica if available
      5. Update status page
```

---

##### 6. Disk Space Critical

```yaml
- alert: DiskSpaceCritical
  expr: |
    (
      node_filesystem_avail_bytes{mountpoint="/var/lib/postgresql"} 
      / 
      node_filesystem_size_bytes{mountpoint="/var/lib/postgresql"}
    ) < 0.10
  for: 5m
  labels:
    severity: critical
    team: infrastructure
  annotations:
    summary: "Disk space critical: {{ $value | humanizePercentage }} remaining"
    description: |
      Disk space below 10%. Database writes may fail soon.
      
      Immediate actions:
      1. Check for large log files: `du -sh /var/log/* | sort -h`
      2. Clean up old backups
      3. Archive old audit logs
      4. Consider emergency disk expansion
```

---

#### Warning Alerts (Investigate, Don't Page)

These alerts indicate **potential problems** that should be investigated but don't require immediate paging.

##### 1. Elevated Latency (P95)

```yaml
- alert: ElevatedLatency
  expr: |
    histogram_quantile(
      0.95,
      sum by (le) (rate(commands_duration_bucket[5m]))
    ) > 500
  for: 10m
  labels:
    severity: warning
    team: backend
  annotations:
    summary: "P95 latency elevated: {{ $value }}ms"
    description: |
      P95 latency exceeds 500ms. Not critical yet, but trending in wrong direction.
      
      Actions:
      - Review recent code changes
      - Check for slow queries (>100ms)
      - Monitor for continued degradation
      - May escalate to critical if P99 approaches 1s
```

---

##### 2. Low Cache Hit Ratio

```yaml
- alert: LowCacheHitRatio
  expr: |
    (
      sum(rate(cache_hits_total[5m])) 
      /
      (
        sum(rate(cache_hits_total[5m])) + 
        sum(rate(cache_misses_total[5m]))
      )
    ) < 0.7
  for: 15m
  labels:
    severity: warning
    team: backend
  annotations:
    summary: "Cache hit ratio low: {{ $value | humanizePercentage }}"
    description: |
      Cache hit ratio below 70%. More database queries than expected.
      
      Possible causes:
      - Cache invalidation too aggressive
      - Cache size too small (check eviction rate)
      - New traffic pattern not in cache
      - Cache warming needed after deployment
```

---

##### 3. High Database Connection Usage

```yaml
- alert: HighDatabaseConnectionUsage
  expr: |
    (
      db_connection_pool_active 
      / 
      db_connection_pool_size
    ) > 0.7
  for: 10m
  labels:
    severity: warning
    team: backend
  annotations:
    summary: "Database connections at {{ $value | humanizePercentage }}"
    description: |
      Connection pool usage above 70%. Approaching critical threshold (90%).
      
      Actions:
      - Monitor for continued growth
      - Check for connection leaks
      - Review long-running transactions
      - Consider increasing pool size if sustained
```

---

##### 4. Error Rate Elevated (Per Tenant)

```yaml
- alert: TenantErrorRateElevated
  expr: |
    (
      sum by (tenant_id) (rate(commands_failed_total[5m])) 
      /
      sum by (tenant_id) (rate(commands_executed_total[5m]))
    ) > 0.02
  for: 10m
  labels:
    severity: warning
    team: customer-success
  annotations:
    summary: "Tenant {{ $labels.tenant_id }} error rate: {{ $value | humanizePercentage }}"
    description: |
      Specific tenant experiencing elevated errors (>2%).
      
      Actions:
      - Check tenant-specific logs
      - Review recent tenant configuration changes
      - May indicate tenant-specific data issue
      - Contact tenant if persistent
```

---

##### 5. Slow Background Jobs

```yaml
- alert: BackgroundJobBacklog
  expr: background_job_queue_depth > 1000
  for: 15m
  labels:
    severity: warning
    team: backend
  annotations:
    summary: "Background job queue depth: {{ $value }}"
    description: |
      Background job queue is growing. Processing may be falling behind.
      
      Actions:
      - Check worker health
      - Review failed jobs
      - Consider adding workers if sustained
      - Check for poison messages
```

---

#### Info Alerts (Awareness Only)

```yaml
- alert: DeploymentCompleted
  expr: changes(deployment_version[1m]) > 0
  labels:
    severity: info
  annotations:
    summary: "New version deployed: {{ $labels.version }}"

- alert: SLOErrorBudget50PercentConsumed
  expr: slo_error_budget_remaining < 0.5
  labels:
    severity: info
  annotations:
    summary: "Error budget 50% consumed"
    description: "Consider slowing feature velocity or improving reliability."
```

---

### Implementation Guide

#### Option 1: Full External Stack (Recommended for Production)

**Stack**: Prometheus + Grafana + Alertmanager

**Pros**:
- Industry standard, battle-tested
- Rich ecosystem of exporters and integrations
- Excellent query language (PromQL)
- Grafana has beautiful, customizable dashboards
- Alertmanager handles complex routing and silencing

**Cons**:
- Separate infrastructure to maintain
- Learning curve for PromQL
- Additional cost (hosting, cloud services)

**Setup (Docker Compose)**:

```yaml
# docker-compose.observability.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.enable-lifecycle'
  
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - grafana-data:/var/lib/grafana
      - ./grafana/provisioning:/etc/grafana/provisioning
  
  alertmanager:
    image: prom/alertmanager:latest
    ports:
      - "9093:9093"
    volumes:
      - ./alertmanager.yml:/etc/alertmanager/alertmanager.yml
    command:
      - '--config.file=/etc/alertmanager/alertmanager.yml'

volumes:
  prometheus-data:
  grafana-data:
```

**Prometheus Configuration**:

```yaml
# prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093

rule_files:
  - "alerts/*.yml"

scrape_configs:
  - job_name: 'idevs'
    static_configs:
      - targets: ['host.docker.internal:5000']  # ASP.NET Core app
    metric_relabel_configs:
      # Add tenant labels
      - source_labels: [__name__]
        regex: '(.*)'
        target_label: 'service'
        replacement: 'idevs'
  
  - job_name: 'postgresql'
    static_configs:
      - targets: ['postgres-exporter:9187']
```

**ASP.NET Core Integration**:

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();  // Expose /metrics endpoint
    });

var app = builder.Build();

// Expose Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();  // Adds /metrics route

app.Run();
```

**Package Required**:
```bash
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
```

---

#### Option 2: Internal Dashboard (Built-in .NET Metrics)

**Use Case**: Lightweight monitoring without external infrastructure, suitable for:
- Development/staging environments
- Small deployments
- Internal tooling
- Teams preferring embedded solutions

**Pros**:
- No external dependencies (uses built-in System.Diagnostics.Metrics)
- Embedded in application
- Easy authentication (use existing auth)
- Zero additional infrastructure
- Apache 2.0 license (safe for commercial use)

**Cons**:
- Limited scalability
- Manual implementation effort
- Less sophisticated than Grafana
- No built-in alerting

##### Implementation: ASP.NET Core Dashboard

**1. No Additional Packages Required**:

Uses built-in .NET libraries:
- `System.Diagnostics.Metrics` (included in .NET 8+)
- `System.Diagnostics.DiagnosticSource` (included in .NET 8+)
- `Microsoft.AspNetCore.Diagnostics.HealthChecks` (MIT license)

**2. Create Metrics Collection Service**:

```csharp
// Services/MetricsCollectionService.cs
public class MetricsCollectionService
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, List<double>> _histograms = new();
    
    public void IncrementCounter(string name, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        _counters.AddOrUpdate(key, 1, (_, current) => current + 1);
    }
    
    public void RecordValue(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        _histograms.AddOrUpdate(key, new List<double> { value }, (_, list) =>
        {
            list.Add(value);
            if (list.Count > 1000) list.RemoveAt(0); // Keep last 1000
            return list;
        });
    }
    
    public Dictionary<string, long> GetCounters() => new(_counters);
    
    public Dictionary<string, double> GetPercentiles(string name)
    {
        if (!_histograms.TryGetValue(name, out var values) || values.Count == 0)
            return new();
        
        var sorted = values.OrderBy(x => x).ToArray();
        return new Dictionary<string, double>
        {
            ["p50"] = GetPercentile(sorted, 0.50),
            ["p95"] = GetPercentile(sorted, 0.95),
            ["p99"] = GetPercentile(sorted, 0.99)
        };
    }
    
    private double GetPercentile(double[] sorted, double percentile)
    {
        var index = (int)Math.Ceiling(sorted.Length * percentile) - 1;
        return sorted[Math.Max(0, index)];
    }
    
    private string BuildKey(string name, Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0) return name;
        var tagString = string.Join(",", tags.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));
        return $"{name}{{{tagString}}}";
    }
}
```

**3. Create Dashboard Controller**:

```csharp
// Controllers/ObservabilityDashboardController.cs
[Route("internal/observability")]
[Authorize(Policy = "InternalOnly")] // Restrict access
public class ObservabilityDashboardController : Controller
{
    private readonly MetricsCollectionService _metrics;
    private readonly ILogger<ObservabilityDashboardController> _logger;
    
    public ObservabilityDashboardController(
        MetricsCollectionService metrics,
        ILogger<ObservabilityDashboardController> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        var counters = _metrics.GetCounters();
        var commandLatency = _metrics.GetPercentiles("commands.duration");
        
        var totalCommands = counters.GetValueOrDefault("commands.executed", 0);
        var failedCommands = counters.GetValueOrDefault("commands.failed", 0);
        
        var viewModel = new DashboardViewModel
        {
            RequestRate = totalCommands,
            ErrorRate = totalCommands > 0 ? (failedCommands * 100.0 / totalCommands) : 0,
            P99Latency = commandLatency.GetValueOrDefault("p99", 0),
            ActiveConnections = GetActiveConnections(),
            MemoryUsageMB = GC.GetTotalMemory(false) / 1024 / 1024,
            Uptime = GetUptime(),
            RecentErrors = GetRecentErrors(),
            TopTenants = GetTopTenants(counters)
        };
        
        return View(viewModel);
    }
    
    [HttpGet("api/metrics")]
    public IActionResult GetMetrics()
    {
        var counters = _metrics.GetCounters();
        var latency = _metrics.GetPercentiles("commands.duration");
        
        var totalCommands = counters.GetValueOrDefault("commands.executed", 0);
        var failedCommands = counters.GetValueOrDefault("commands.failed", 0);
        
        return Json(new
        {
            timestamp = DateTime.UtcNow,
            requestRate = totalCommands,
            errorRate = totalCommands > 0 ? (failedCommands * 100.0 / totalCommands) : 0,
            latency = new
            {
                p50 = latency.GetValueOrDefault("p50", 0),
                p95 = latency.GetValueOrDefault("p95", 0),
                p99 = latency.GetValueOrDefault("p99", 0)
            }
        });
    }
    
    private int GetActiveConnections() => 0; // Implement based on your needs
    private TimeSpan GetUptime() => DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
    
    private List<ErrorLogEntry> GetRecentErrors()
    {
        // Query from in-memory buffer or database
        return new List<ErrorLogEntry>();
    }
    
    private List<TenantMetric> GetTopTenants(Dictionary<string, long> counters)
    {
        return counters
            .Where(kv => kv.Key.Contains("tenant="))
            .GroupBy(kv => ExtractTenantId(kv.Key))
            .Select(g => new TenantMetric
            {
                TenantId = g.Key,
                RequestCount = g.Sum(kv => kv.Value)
            })
            .OrderByDescending(t => t.RequestCount)
            .Take(10)
            .ToList();
    }
    
    private string ExtractTenantId(string key)
    {
        var match = System.Text.RegularExpressions.Regex.Match(key, @"tenant=([^,}]+)");
        return match.Success ? match.Groups[1].Value : "unknown";
    }
}
```

**4. Dashboard View (Razor)** (unchanged):

```cshtml
@* Views/ObservabilityDashboard/Index.cshtml *@
@model DashboardViewModel

<!DOCTYPE html>
<html>
<head>
    <title>Idevs Framework Observability</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">
    <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/axios/dist/axios.min.js"></script>
</head>
<body>
    <div class="container-fluid mt-4">
        <h1>Idevs Framework Service Health</h1>
        
        <!-- Key Metrics -->
        <div class="row mt-4">
            <div class="col-md-3">
                <div class="card">
                    <div class="card-body">
                        <h5 class="card-title">Request Rate</h5>
                        <h2 id="request-rate">@Model.RequestRate.ToString("N0")/s</h2>
                        <canvas id="request-rate-chart" height="50"></canvas>
                    </div>
                </div>
            </div>
            
            <div class="col-md-3">
                <div class="card @(Model.ErrorRate > 5 ? "border-danger" : "")">
                    <div class="card-body">
                        <h5 class="card-title">Error Rate</h5>
                        <h2 id="error-rate" class="@(Model.ErrorRate > 5 ? "text-danger" : "")">@Model.ErrorRate.ToString("N2")%</h2>
                        <small class="text-muted">Threshold: <0.1%</small>
                    </div>
                </div>
            </div>
            
            <div class="col-md-3">
                <div class="card @(Model.P99Latency > 1000 ? "border-warning" : "")">
                    <div class="card-body">
                        <h5 class="card-title">P99 Latency</h5>
                        <h2 id="p99-latency">@Model.P99Latency.ToString("N0")ms</h2>
                        <small class="text-muted">SLO: <150ms</small>
                    </div>
                </div>
            </div>
            
            <div class="col-md-3">
                <div class="card">
                    <div class="card-body">
                        <h5 class="card-title">Memory</h5>
                        <h2>@Model.MemoryUsageMB.ToString("N0") MB</h2>
                        <small class="text-muted">Uptime: @Model.Uptime</small>
                    </div>
                </div>
            </div>
        </div>
        
        <!-- Recent Errors -->
        <div class="row mt-4">
            <div class="col-md-12">
                <div class="card">
                    <div class="card-header">
                        <h5>Recent Errors (Last 5 minutes)</h5>
                    </div>
                    <div class="card-body">
                        <table class="table table-sm">
                            <thead>
                                <tr>
                                    <th>Time</th>
                                    <th>Tenant</th>
                                    <th>Command</th>
                                    <th>Error</th>
                                    <th>Trace ID</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var error in Model.RecentErrors)
                                {
                                    <tr>
                                        <td>@error.Timestamp.ToString("HH:mm:ss")</td>
                                        <td>@error.TenantId</td>
                                        <td>@error.CommandType</td>
                                        <td><span class="badge bg-danger">@error.ErrorType</span></td>
                                        <td><code>@error.TraceId</code></td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
        
        <!-- Top Tenants -->
        <div class="row mt-4">
            <div class="col-md-6">
                <div class="card">
                    <div class="card-header">
                        <h5>Top 10 Tenants by Request Volume</h5>
                    </div>
                    <div class="card-body">
                        <canvas id="tenant-chart"></canvas>
                    </div>
                </div>
            </div>
            
            <div class="col-md-6">
                <div class="card">
                    <div class="card-header">
                        <h5>Latency Distribution</h5>
                    </div>
                    <div class="card-body">
                        <canvas id="latency-chart"></canvas>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <script>
        // Auto-refresh metrics every 10 seconds
        setInterval(async () => {
            const response = await axios.get('/internal/observability/api/metrics');
            const data = response.data;
            
            document.getElementById('request-rate').textContent = data.requestRate.toFixed(0) + '/s';
            document.getElementById('error-rate').textContent = data.errorRate.toFixed(2) + '%';
            document.getElementById('p99-latency').textContent = data.latency.p99.toFixed(0) + 'ms';
            
            // Update error rate card styling
            const errorCard = document.getElementById('error-rate').closest('.card');
            if (data.errorRate > 5) {
                errorCard.classList.add('border-danger');
            } else {
                errorCard.classList.remove('border-danger');
            }
        }, 10000);
    </script>
</body>
</html>
```

**4. Authorization Policy**:

```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin", "Operations");
        // Or IP whitelist
        policy.RequireAssertion(context =>
        {
            var httpContext = context.Resource as HttpContext;
            var remoteIp = httpContext?.Connection.RemoteIpAddress;
            return remoteIp != null && IsInternalIP(remoteIp);
        });
    });
});
```

---

### Alert Notification Implementation

#### Option 1: Using Alertmanager (with Prometheus)

**Alertmanager Configuration**:

```yaml
# alertmanager.yml
global:
  resolve_timeout: 5m
  slack_api_url: 'https://hooks.slack.com/services/YOUR/WEBHOOK/URL'

route:
  group_by: ['alertname', 'severity']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 12h
  receiver: 'default'
  
  routes:
    # Critical alerts -> PagerDuty + Slack
    - match:
        severity: critical
      receiver: 'pagerduty-critical'
      continue: true
    
    - match:
        severity: critical
      receiver: 'slack-critical'
    
    # Warning alerts -> Slack only
    - match:
        severity: warning
      receiver: 'slack-warnings'
    
    # Security alerts -> Security team + Slack
    - match:
        team: security
      receiver: 'security-team'

receivers:
  - name: 'default'
    email_configs:
      - to: 'ops@company.com'
        from: 'alertmanager@company.com'
        smarthost: 'smtp.company.com:587'
  
  - name: 'pagerduty-critical'
    pagerduty_configs:
      - service_key: 'YOUR_PAGERDUTY_SERVICE_KEY'
        description: '{{ .CommonAnnotations.summary }}'
        details:
          firing: '{{ .Alerts.Firing | len }}'
          resolved: '{{ .Alerts.Resolved | len }}'
  
  - name: 'slack-critical'
    slack_configs:
      - channel: '#alerts-critical'
        title: 'CRITICAL: {{ .CommonAnnotations.summary }}'
        text: |
          {{ range .Alerts }}
          *Alert:* {{ .Labels.alertname }}
          *Severity:* {{ .Labels.severity }}
          *Description:* {{ .Annotations.description }}
          *Dashboard:* {{ .Annotations.dashboard }}
          {{ end }}
        color: 'danger'
        send_resolved: true
  
  - name: 'slack-warnings'
    slack_configs:
      - channel: '#alerts-warnings'
        title: 'Warning: {{ .CommonAnnotations.summary }}'
        color: 'warning'
  
  - name: 'security-team'
    email_configs:
      - to: 'security@company.com'
        headers:
          Subject: 'SECURITY ALERT: {{ .CommonAnnotations.summary }}'
    slack_configs:
      - channel: '#security-alerts'
        color: 'danger'

inhibit_rules:
  # Don't alert on warning if critical is firing
  - source_match:
      severity: 'critical'
    target_match:
      severity: 'warning'
    equal: ['alertname', 'cluster', 'service']
```

---

#### Option 2: Custom .NET Alert Notification Service

**Use Case**: When you want full control or don't use Prometheus/Alertmanager.

**1. Alert Service Interface**:

```csharp
public interface IAlertService
{
    Task SendAlertAsync(Alert alert, CancellationToken cancellationToken = default);
}

public record Alert(
    string Name,
    AlertSeverity Severity,
    string Summary,
    string Description,
    Dictionary<string, string> Labels,
    Dictionary<string, string> Annotations,
    DateTime FiredAt);

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}
```

**2. Alert Service Implementation**:

```csharp
public class AlertNotificationService : IAlertService
{
    private readonly ISlackNotifier _slack;
    private readonly IEmailNotifier _email;
    private readonly IPagerDutyNotifier _pagerDuty;
    private readonly ILogger<AlertNotificationService> _logger;
    
    public AlertNotificationService(
        ISlackNotifier slack,
        IEmailNotifier email,
        IPagerDutyNotifier pagerDuty,
        ILogger<AlertNotificationService> logger)
    {
        _slack = slack;
        _email = email;
        _pagerDuty = pagerDuty;
        _logger = logger;
    }
    
    public async Task SendAlertAsync(Alert alert, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Alert triggered: {AlertName} (Severity: {Severity})",
            alert.Name,
            alert.Severity);
        
        switch (alert.Severity)
        {
            case AlertSeverity.Critical:
                // Page on-call engineer
                await _pagerDuty.TriggerIncidentAsync(
                    title: alert.Summary,
                    description: alert.Description,
                    urgency: "high",
                    cancellationToken: cancellationToken);
                
                // Also notify Slack
                await _slack.SendMessageAsync(
                    channel: "#alerts-critical",
                    message: FormatSlackMessage(alert, "🚨"),
                    cancellationToken: cancellationToken);
                break;
            
            case AlertSeverity.Warning:
                // Slack only
                await _slack.SendMessageAsync(
                    channel: "#alerts-warnings",
                    message: FormatSlackMessage(alert, "⚠️"),
                    cancellationToken: cancellationToken);
                break;
            
            case AlertSeverity.Info:
                // Email digest (batched)
                await _email.QueueForDigestAsync(alert, cancellationToken);
                break;
        }
        
        // Security alerts always notify security team
        if (alert.Labels.TryGetValue("team", out var team) && team == "security")
        {
            await _email.SendAsync(
                to: "security@company.com",
                subject: $"SECURITY ALERT: {alert.Summary}",
                body: alert.Description,
                cancellationToken: cancellationToken);
        }
    }
    
    private string FormatSlackMessage(Alert alert, string emoji)
    {
        return $"""
            {emoji} *{alert.Summary}*
            
            *Severity:* {alert.Severity}
            *Time:* {alert.FiredAt:yyyy-MM-dd HH:mm:ss} UTC
            
            {alert.Description}
            
            *Labels:*
            {string.Join("\n", alert.Labels.Select(kv => $"• {kv.Key}: {kv.Value}"))}
            """;
    }
}
```

**3. Slack Notifier Implementation**:

```csharp
public interface ISlackNotifier
{
    Task SendMessageAsync(string channel, string message, CancellationToken cancellationToken);
}

public class SlackNotifier : ISlackNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;
    
    public SlackNotifier(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _webhookUrl = configuration["Slack:WebhookUrl"] 
            ?? throw new InvalidOperationException("Slack webhook URL not configured");
    }
    
    public async Task SendMessageAsync(
        string channel, 
        string message, 
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            channel,
            text = message,
            username = "Idevs Alert",
            icon_emoji = ":warning:"
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            _webhookUrl, 
            payload, 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
    }
}
```

**4. PagerDuty Notifier**:

```csharp
public interface IPagerDutyNotifier
{
    Task TriggerIncidentAsync(
        string title, 
        string description, 
        string urgency, 
        CancellationToken cancellationToken);
}

public class PagerDutyNotifier : IPagerDutyNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _integrationKey;
    
    public PagerDutyNotifier(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _integrationKey = configuration["PagerDuty:IntegrationKey"]
            ?? throw new InvalidOperationException("PagerDuty integration key not configured");
    }
    
    public async Task TriggerIncidentAsync(
        string title,
        string description,
        string urgency,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            routing_key = _integrationKey,
            event_action = "trigger",
            dedup_key = $"idevs-{Guid.NewGuid()}",
            payload = new
            {
                summary = title,
                source = "idevs",
                severity = urgency,
                custom_details = new { description }
            }
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "https://events.pagerduty.com/v2/enqueue",
            payload,
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
    }
}
```

**5. Background Alert Monitoring Service**:

```csharp
public class AlertMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertMonitoringService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var metrics = scope.ServiceProvider.GetRequiredService<IMetricsRoot>();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                
                var snapshot = metrics.Snapshot.Get();
                
                // Check error rate
                var errorRate = CalculateErrorRate(snapshot);
                if (errorRate > 5.0)
                {
                    await alertService.SendAlertAsync(new Alert(
                        Name: "HighErrorRate",
                        Severity: AlertSeverity.Critical,
                        Summary: $"High error rate: {errorRate:F2}%",
                        Description: "Error rate exceeds 5% threshold. Immediate action required.",
                        Labels: new Dictionary<string, string>
                        {
                            ["severity"] = "critical",
                            ["team"] = "backend"
                        },
                        Annotations: new Dictionary<string, string>
                        {
                            ["runbook"] = "https://wiki.company.com/runbooks/high-error-rate"
                        },
                        FiredAt: DateTime.UtcNow
                    ), stoppingToken);
                }
                
                // Check P99 latency
                var p99Latency = CalculateP99Latency(snapshot);
                if (p99Latency > 1000)
                {
                    await alertService.SendAlertAsync(new Alert(
                        Name: "HighLatency",
                        Severity: AlertSeverity.Critical,
                        Summary: $"P99 latency: {p99Latency:F0}ms",
                        Description: "P99 latency exceeds 1 second. Users experiencing slow responses.",
                        Labels: new Dictionary<string, string>
                        {
                            ["severity"] = "critical",
                            ["team"] = "backend"
                        },
                        Annotations: new Dictionary<string, string>(),
                        FiredAt: DateTime.UtcNow
                    ), stoppingToken);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alert monitoring service");
            }
        }
    }
}
```

**6. Service Registration**:

```csharp
// Program.cs
builder.Services.AddHttpClient<ISlackNotifier, SlackNotifier>();
builder.Services.AddHttpClient<IPagerDutyNotifier, PagerDutyNotifier>();
builder.Services.AddScoped<IEmailNotifier, EmailNotifier>();
builder.Services.AddScoped<IAlertService, AlertNotificationService>();
builder.Services.AddHostedService<AlertMonitoringService>();
```

**7. Configuration**:

```json
// appsettings.json
{
  "Slack": {
    "WebhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
  },
  "PagerDuty": {
    "IntegrationKey": "YOUR_PAGERDUTY_INTEGRATION_KEY"
  },
  "Alerts": {
    "ErrorRateThreshold": 5.0,
    "LatencyThresholdMs": 1000,
    "CheckIntervalSeconds": 30
  }
}
```

---

### Comparison Matrix

| Feature | Prometheus + Grafana + Alertmanager | Internal Dashboard + Custom Alerts |
|---------|-------------------------------------|-------------------------------------|
| **Setup Complexity** | Medium (Docker Compose) | Low (embedded) |
| **Scalability** | Excellent (production-ready) | Limited (single instance) |
| **Query Language** | PromQL (powerful) | C# LINQ (familiar) |
| **Dashboard Flexibility** | Excellent (Grafana) | Limited (manual HTML/JS) |
| **Alert Routing** | Advanced (Alertmanager) | Custom logic required |
| **Cost** | Infrastructure + time | Development time only |
| **Learning Curve** | Steep (PromQL, Grafana) | Shallow (.NET devs) |
| **Best For** | Production, multi-service | Development, small deployments |

---

### Recommendation

**For Production**: Use **Prometheus + Grafana + Alertmanager**
- Industry standard, battle-tested
- Rich ecosystem and community support
- Better for multi-service architectures

**For Development/Staging**: Start with **Internal Dashboard**
- Faster initial setup
- No external dependencies
- Easy to iterate and customize
- Can migrate to Prometheus later

**Hybrid Approach** (Best of Both):
1. Expose Prometheus metrics endpoint (`/metrics`)
2. Build lightweight internal dashboard for quick checks
3. Use Grafana for detailed analysis
4. Use Alertmanager for production alerts

---

### SLO Monitoring

**Service Level Objectives**:

| Metric | SLO | Error Budget (Monthly) |
|--------|-----|------------------------|
| Availability (Dedicated) | 99.9% | 43 minutes |
| Availability (Multi-tenant) | 99.5% | 3.6 hours |
| P99 Latency | < 150ms | 5% of requests |
| Error Rate | < 0.1% | 0.1% of requests |

**Error Budget Calculation**:
```promql
# Remaining error budget percentage
100 - ((1 - (sum(rate(http_requests_total{status!~"5.."}[30d])) / sum(rate(http_requests_total[30d])))) / (1 - 0.999)) * 100
```

## Developer Workflow

### Local Development

```csharp
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "Debug" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/dev-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  },
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4317"  // Local Jaeger
  }
}
```

### Testing Logging

```csharp
[Fact]
public async Task CreateOrder_LogsCommandExecution()
{
    // Arrange
    var logSink = new TestSink();
    var logger = new LoggerConfiguration()
        .WriteTo.Sink(logSink)
        .CreateLogger();
    
    var handler = new CreateOrderHandler(logger, _repository);
    
    // Act
    await handler.HandleAsync(new CreateOrderCommand { ... });
    
    // Assert
    logSink.Events.Should().ContainSingle(e => 
        e.MessageTemplate.Text.Contains("Order created") &&
        e.Properties.ContainsKey("OrderId"));
}
```

### Log Sampling in High-Volume Scenarios

```csharp
public class SamplingEnricher : ILogEventEnricher
{
    private readonly double _samplingRate;
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        // Sample 1% of Info logs, 100% of warnings/errors
        if (logEvent.Level == LogEventLevel.Information)
        {
            if (Random.Shared.NextDouble() > _samplingRate)
            {
                logEvent.AddPropertyIfAbsent(
                    factory.CreateProperty("Sampled", "dropped"));
            }
        }
    }
}
```

## Implementation Checklist

### Logging
- [ ] Serilog configured with structured logging
- [ ] Correlation ID enricher implemented
- [ ] Tenant context enricher implemented
- [ ] PII redaction enricher implemented
- [ ] Event IDs defined for all categories
- [ ] Log levels consistently applied
- [ ] Console, File, and remote sinks configured

### Tracing
- [ ] OpenTelemetry configured with W3C Trace Context
- [ ] ASP.NET Core automatic instrumentation enabled
- [ ] EF Core automatic instrumentation enabled
- [ ] Manual spans for critical operations
- [ ] Trace sampling strategy implemented
- [ ] Traces exported to Jaeger/Zipkin/AppInsights

### Metrics
- [ ] RED metrics implemented for all handlers
- [ ] Domain-specific metrics defined
- [ ] Resource metrics (DB, memory, connections) tracked
- [ ] Metrics exported to Prometheus/AppInsights
- [ ] Tenant tags applied to all metrics

### Dashboards & Alerts
- [ ] Service health dashboard created
- [ ] Multi-tenant dashboard created
- [ ] Database performance dashboard created
- [ ] Critical alerts configured (error rate, latency, security)
- [ ] Warning alerts configured (elevated latency, cache hit ratio)
- [ ] SLO monitoring and error budget tracking

## References

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Writing-Log-Events)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [RED Metrics Pattern](https://www.weave.works/blog/the-red-method-key-metrics-for-microservices-architecture/)
- [Discovery Summary](discovery-summary.md)
- [CQRS Framework Plan](cqrs-framework-plan.md)
- [Threat Model](threat-model.md)

---

**Next Review**: Quarterly or when adding new major features  
**Maintainer**: Platform Engineering Team
