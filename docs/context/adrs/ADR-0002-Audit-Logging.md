# ADR-0002: Audit Logging Strategy

**Status**: Proposed  
**Date**: 2025-10-04  
**Deciders**: Architecture Team, Security Team, Compliance Team  
**Related**: ADR-0001 (Tenancy Strategy), Phase 0 (Discovery), Threat Model, Discovery Summary (Regulated Tenant Persona)

---

## Context

The Idevs framework must provide comprehensive audit logging to meet compliance requirements and support forensic analysis across multiple regulatory frameworks:

### Compliance Requirements

1. **SOC 2 Type II** - System and Organization Controls
   - Track all data modifications (who, what, when, where)
   - Immutable audit trails
   - Log retention for defined periods
   
2. **GDPR** - General Data Protection Regulation
   - Right to access (provide audit trail of data access)
   - Right to erasure (track deletion requests)
   - Data breach notification (forensic capability)

3. **HIPAA** - Health Insurance Portability and Accountability Act
   - Access logs for protected health information (PHI)
   - Audit controls (§164.312(b))
   - Integrity controls (§164.312(c)(1))

4. **ISO 27001** - Information Security Management
   - Logging and monitoring (A.12.4.1)
   - Protection of log information (A.12.4.2)
   - Administrator and operator logs (A.12.4.3)

### Multi-Tenant Considerations

From [ADR-0001 Tenancy Strategy](./ADR-0001-Tenancy-Strategy.md):
- Every audit entry must include `tenant_id`
- Cross-tenant audit queries prohibited
- Tenant-specific retention policies required
- Audit isolation as critical as data isolation

### Regulated Tenant Persona Requirements

From [Discovery Summary - Regulated Tenant](../discovery-summary.md#4-regulated-tenant):
- Extended retention (7+ years)
- Immutable logs with cryptographic verification
- Rapid incident notifications
- Export capability for regulatory review
- Data anonymization for PII

### Performance and Storage Constraints

**Constraints**:
- Audit logging must not block business operations
- Storage costs must be predictable and manageable
- Query performance on audit data must support investigations

**Targets**:
- P99 write latency < 50ms overhead
- Audit storage growth rate: ~5-10% of primary database size annually
- Audit query response < 3 seconds for 90-day windows

---

## Decision

**Implement a dual-strategy audit approach:**

1. **Database Audit Trail**: EF Core SaveChanges interceptors capturing data changes to an append-only `audit_logs` table
2. **Application Audit Trail**: Serilog structured logging for operational events, command execution, and queries

### Strategy 1: Database Audit Trail (Data Changes)

**Mechanism**: EF Core `SaveChangesInterceptor`

```csharp
// Idevs.Data/Interceptors/AuditInterceptor.cs
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ICorrelationContext _correlationContext;
    
    public AuditInterceptor(
        ITenantContext tenantContext,
        IUserContext userContext,
        ICorrelationContext correlationContext)
    {
        _tenantContext = tenantContext;
        _userContext = userContext;
        _correlationContext = correlationContext;
    }
    
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return result;
            
        var auditEntries = CreateAuditEntries(eventData.Context);
        
        // Add audit entries to context
        foreach (var entry in auditEntries)
        {
            eventData.Context.Set<AuditLog>().Add(entry);
        }
        
        return result;
    }
    
    private List<AuditLog> CreateAuditEntries(DbContext context)
    {
        var entries = new List<AuditLog>();
        
        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Skip audit entities themselves (prevent recursion)
            if (entry.Entity is AuditLog)
                continue;
                
            // Skip unchanged or detached entities
            if (entry.State == EntityState.Unchanged || 
                entry.State == EntityState.Detached)
                continue;
            
            var auditEntry = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                UserId = _userContext.UserId,
                UserName = _userContext.UserName,
                CorrelationId = _correlationContext.CorrelationId,
                EntityType = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                Action = entry.State.ToString(), // Added, Modified, Deleted
                Timestamp = DateTime.UtcNow,
                BeforeValues = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                    ? SerializeEntity(entry.OriginalValues)
                    : null,
                AfterValues = entry.State == EntityState.Added || entry.State == EntityState.Modified
                    ? SerializeEntity(entry.CurrentValues)
                    : null,
                ChangedProperties = entry.State == EntityState.Modified
                    ? GetChangedProperties(entry)
                    : null
            };
            
            entries.Add(auditEntry);
        }
        
        return entries;
    }
    
    private string GetEntityId(EntityEntry entry)
    {
        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue?.ToString() ?? "unknown";
    }
    
    private string? SerializeEntity(PropertyValues values)
    {
        var dictionary = new Dictionary<string, object?>();
        
        foreach (var property in values.Properties)
        {
            var value = values[property];
            
            // Redact PII based on EF Core metadata annotations
            if (ShouldRedact(property))
            {
                value = "[REDACTED]";
            }
            
            dictionary[property.Name] = value;
        }
        
        return JsonSerializer.Serialize(dictionary);
    }
    
    private bool ShouldRedact(IProperty property)
    {
        // Check EF Core metadata for PII annotation (no reflection!)
        return property.FindAnnotation("IsPII")?.Value as bool? ?? false;
    }
    
    private string? GetChangedProperties(EntityEntry entry)
    {
        var changedProps = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => p.Metadata.Name)
            .ToList();
            
        return changedProps.Any() 
            ? JsonSerializer.Serialize(changedProps)
            : null;
    }
}
```

**Audit Table Schema**:

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    user_id UUID NOT NULL,
    user_name VARCHAR(255) NOT NULL,
    correlation_id UUID NOT NULL,
    entity_type VARCHAR(255) NOT NULL,
    entity_id VARCHAR(255) NOT NULL,
    action VARCHAR(50) NOT NULL, -- Added, Modified, Deleted
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    before_values JSONB,
    after_values JSONB,
    changed_properties JSONB,
    
    CONSTRAINT fk_audit_logs_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);

-- Critical indexes for audit queries
CREATE INDEX ix_audit_logs_tenant_timestamp ON audit_logs(tenant_id, timestamp DESC);
CREATE INDEX ix_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX ix_audit_logs_correlation ON audit_logs(correlation_id);
CREATE INDEX ix_audit_logs_user ON audit_logs(tenant_id, user_id, timestamp DESC);

-- GIN index for JSONB queries (optional, for complex queries)
CREATE INDEX ix_audit_logs_after_values ON audit_logs USING GIN (after_values);
```

**PII Redaction via EF Core Metadata (No Reflection)**:

```csharp
// Idevs/Domain/Entities/User.cs
public class User : IEntity<Guid>, ITenantEntity, IAuditableEntity
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    // ... other properties
}

// Idevs.Data/Configuration/UserConfiguration.cs
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasAnnotation("IsPII", true); // Mark as PII for redaction
        
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50)
            .HasAnnotation("IsPII", true);
        
        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .HasAnnotation("IsPII", true);
        
        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .HasAnnotation("IsPII", true);
            
        // Non-PII fields don't need annotation
    }
}
```

### Strategy 2: Application Audit Trail (Operational Events)

**Mechanism**: Serilog with structured logging and enrichers

```csharp
// Program.cs or Startup.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "Idevs")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        new JsonFormatter(),
        path: "logs/audit-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90) // 90 days retention in files
    .WriteTo.Seq(serverUrl: "http://seq:5341") // Optional: Seq for centralized logs
    .CreateLogger();

// Idevs.Web/Middleware/AuditLoggingMiddleware.cs
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    
    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IUserContext userContext)
    {
        using (LogContext.PushProperty("TenantId", tenantContext.TenantId))
        using (LogContext.PushProperty("UserId", userContext.UserId))
        using (LogContext.PushProperty("UserName", userContext.UserName))
        using (LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
        using (LogContext.PushProperty("IpAddress", context.Connection.RemoteIpAddress?.ToString()))
        {
            _logger.LogInformation(
                "HTTP {Method} {Path} started by {UserName} from {IpAddress}",
                context.Request.Method,
                context.Request.Path,
                userContext.UserName,
                context.Connection.RemoteIpAddress);
            
            var sw = Stopwatch.StartNew();
            
            await _next(context);
            
            sw.Stop();
            
            _logger.LogInformation(
                "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
```

**Command/Query Audit Logging**:

```csharp
// Idevs.Application/Decorators/AuditLoggingCommandDecorator.cs
public class AuditLoggingCommandDecorator<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _inner;
    private readonly ILogger<AuditLoggingCommandDecorator<TCommand>> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    
    public AuditLoggingCommandDecorator(
        ICommandHandler<TCommand> inner,
        ILogger<AuditLoggingCommandDecorator<TCommand>> logger,
        ITenantContext tenantContext,
        IUserContext userContext)
    {
        _inner = inner;
        _logger = logger;
        _tenantContext = tenantContext;
        _userContext = userContext;
    }
    
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var commandType = typeof(TCommand).Name;
        
        _logger.LogInformation(
            "Executing command {CommandType} for tenant {TenantId} by user {UserId}",
            commandType,
            _tenantContext.TenantId,
            _userContext.UserId);
        
        var sw = Stopwatch.StartNew();
        var result = await _inner.HandleAsync(command, cancellationToken);
        sw.Stop();
        
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Command {CommandType} succeeded in {ElapsedMs}ms",
                commandType,
                sw.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Command {CommandType} failed with errors: {Errors}",
                commandType,
                string.Join(", ", result.Errors));
        }
        
        return result;
    }
}
```

### Retention Policies

**Configuration-Driven Retention**:

```csharp
// appsettings.json
{
  "AuditLogging": {
    "DatabaseAudit": {
      "DefaultRetentionDays": 365,        // 1 year for standard tenants
      "RegulatedRetentionDays": 2555,     // 7 years for regulated tenants
      "PartitioningEnabled": true,
      "PartitionIntervalMonths": 3        // Quarterly partitions
    },
    "ApplicationLogs": {
      "RetentionDays": 90,                // 90 days for app logs
      "SinkConfigurations": {
        "File": { "Enabled": true },
        "Seq": { "Enabled": true, "Url": "http://seq:5341" },
        "ApplicationInsights": { "Enabled": false }
      }
    }
  }
}
```

**Partition Strategy (PostgreSQL)**:

```sql
-- Create partitioned audit table
CREATE TABLE audit_logs (
    id UUID NOT NULL,
    tenant_id UUID NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    -- ... other columns
) PARTITION BY RANGE (timestamp);

-- Create quarterly partitions
CREATE TABLE audit_logs_2025_q1 PARTITION OF audit_logs
    FOR VALUES FROM ('2025-01-01') TO ('2025-04-01');

CREATE TABLE audit_logs_2025_q2 PARTITION OF audit_logs
    FOR VALUES FROM ('2025-04-01') TO ('2025-07-01');

-- Automated partition management via background job
-- Drop old partitions based on retention policy
DROP TABLE IF EXISTS audit_logs_2023_q1; -- Older than retention window
```

### Background Purge Job

```csharp
// Idevs.Infrastructure/BackgroundJobs/AuditPurgeJob.cs
public class AuditPurgeJob : IHostedService, IDisposable
{
    private Timer? _timer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditPurgeJob> _logger;
    
    public AuditPurgeJob(IServiceProvider serviceProvider, ILogger<AuditPurgeJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }
    
    private async void DoWork(object? state)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdevsDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IOptions<AuditLoggingOptions>>();
        
        var cutoffDate = DateTime.UtcNow.AddDays(-config.Value.DatabaseAudit.DefaultRetentionDays);
        
        // Delete audit logs older than retention policy
        var deletedCount = await context.Database.ExecuteSqlInterpolatedAsync(
            $@"DELETE FROM audit_logs 
               WHERE timestamp < {cutoffDate} 
               AND tenant_id IN (
                   SELECT id FROM tenants WHERE is_regulated = false
               )");
        
        _logger.LogInformation(
            "Purged {DeletedCount} audit logs older than {CutoffDate}",
            deletedCount,
            cutoffDate);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
    
    public void Dispose() => _timer?.Dispose();
}
```

---

## Consequences

### ✅ Positive Consequences

1. **Automatic Audit Capture**
   - EF Core interceptor captures all data changes automatically
   - Developers don't need to manually log audit entries
   - Consistent audit trail across all entities

2. **Compliance Alignment**
   - Meets SOC 2, GDPR, HIPAA requirements out of the box
   - Immutable append-only audit table
   - Tenant-aware logging with proper isolation

3. **Structured and Queryable**
   - JSONB columns enable flexible queries
   - Indexes optimized for common audit queries
   - Correlation IDs link audit entries to requests

4. **PII Protection**
   - Redaction via EF Core metadata (no reflection)
   - Configurable per-property PII marking
   - Consistent redaction across audit logs

5. **Performance Optimized**
   - Interceptor runs in same transaction (consistency)
   - Partitioning prevents table bloat
   - Async logging minimizes impact

6. **Multi-Tier Retention**
   - Standard tenants: 1 year retention (cost-efficient)
   - Regulated tenants: 7 year retention (compliance)
   - Automated purge job prevents manual maintenance

### ⚠️ Negative Consequences (with Mitigations)

1. **Storage Costs**
   - **Risk**: Audit logs can grow to 5-10% of database size
   - **Mitigation 1**: Partitioning with automated pruning
   - **Mitigation 2**: Compression on older partitions
   - **Mitigation 3**: Archive to cold storage (S3/Blob) for long-term retention
   - **Mitigation 4**: JSONB compression in PostgreSQL

2. **Write Performance Impact**
   - **Risk**: Interceptor adds ~20-50ms to SaveChanges
   - **Mitigation 1**: Async logging to minimize blocking
   - **Mitigation 2**: Batch audit entries in single INSERT
   - **Mitigation 3**: Monitor P99 latency; disable if threshold exceeded
   - **Mitigation 4**: Use read replicas for audit queries

3. **Audit Table Contention**
   - **Risk**: High insert rate can cause lock contention
   - **Mitigation 1**: Partitioning reduces contention per partition
   - **Mitigation 2**: Use UNLOGGED tables for non-regulated tenants (if acceptable)
   - **Mitigation 3**: Separate audit database (if needed)

4. **JSONB Query Complexity**
   - **Risk**: Complex queries on JSONB can be slow
   - **Mitigation 1**: GIN indexes on JSONB columns
   - **Mitigation 2**: Materialize common queries into views
   - **Mitigation 3**: Export to analytical database (e.g., ClickHouse) for heavy queries

5. **PII Leakage Risk**
   - **Risk**: Developers forget to mark PII properties
   - **Mitigation 1**: Architecture tests validate PII annotations
   - **Mitigation 2**: Code review checklist includes PII marking
   - **Mitigation 3**: Default to redact all string properties (opt-out model)
   - **Test**:
     ```csharp
     [Fact]
     public void Entities_With_PII_Must_Have_Metadata_Annotations()
     {
         var piiProperties = new[] { "Email", "PhoneNumber", "SSN", "CreditCard" };
         
         foreach (var entityType in _dbContext.Model.GetEntityTypes())
         {
             foreach (var property in entityType.GetProperties())
             {
                 if (piiProperties.Any(pii => property.Name.Contains(pii, StringComparison.OrdinalIgnoreCase)))
                 {
                     var isPII = property.FindAnnotation("IsPII")?.Value as bool?;
                     isPII.ShouldBeTrue($"{entityType.Name}.{property.Name} must be marked as PII");
                 }
             }
         }
     }
     ```

---

## Alternatives Considered

### Alternative 1: Event Sourcing

**Approach**: Store all state changes as immutable events

```csharp
public class OrderCreatedEvent
{
    public Guid OrderId { get; init; }
    public Guid TenantId { get; init; }
    public decimal Total { get; init; }
    public DateTime Timestamp { get; init; }
}
```

**Pros**:
- Complete audit trail by design
- Time-travel queries (replay events)
- Natural fit for CQRS

**Cons**:
- ❌ High complexity (event store, projections, snapshots)
- ❌ Event schema evolution challenges
- ❌ Not suitable for all entities (only aggregates)
- ❌ Requires event handlers for read models
- ❌ Over-engineering for most use cases

**Verdict**: ❌ Rejected - Too complex for general-purpose audit logging

---

### Alternative 2: Database Triggers

**Approach**: Use PostgreSQL triggers to capture changes

```sql
CREATE OR REPLACE FUNCTION audit_trigger_function()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO audit_logs (entity_type, entity_id, action, before_values, after_values)
    VALUES (TG_TABLE_NAME, NEW.id, TG_OP, row_to_json(OLD), row_to_json(NEW));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER orders_audit_trigger
AFTER INSERT OR UPDATE OR DELETE ON orders
FOR EACH ROW EXECUTE FUNCTION audit_trigger_function();
```

**Pros**:
- Captures all changes (even direct SQL)
- No application code needed

**Cons**:
- ❌ No access to application context (user, correlation ID, tenant)
- ❌ Harder to test (database-level logic)
- ❌ Difficult to customize per entity type
- ❌ PII redaction requires database-level logic
- ❌ Not portable across databases (PostgreSQL-specific)

**Verdict**: ❌ Rejected - Lacks application context needed for compliance

---

### Alternative 3: Explicit Logging in Services

**Approach**: Developers manually log audit entries

```csharp
public async Task<Result> CreateOrder(CreateOrderCommand command)
{
    var order = new Order { /* ... */ };
    await _repository.AddAsync(order);
    
    // Manual audit logging
    await _auditService.LogAsync(new AuditEntry
    {
        Action = "Created",
        EntityType = "Order",
        EntityId = order.Id
    });
    
    return Result.Success();
}
```

**Pros**:
- Full control over what is logged
- Can add custom context

**Cons**:
- ❌ Developers must remember to log (error-prone)
- ❌ Inconsistent audit trails
- ❌ High maintenance burden
- ❌ Boilerplate code in every command handler

**Verdict**: ❌ Rejected - Too error-prone, inconsistent

---

### Alternative 4: Change Data Capture (CDC)

**Approach**: Use PostgreSQL logical replication or Debezium

**Pros**:
- No application code changes
- Real-time change streaming

**Cons**:
- ❌ Infrastructure complexity (Kafka, Debezium, connectors)
- ❌ No application context (user, correlation)
- ❌ Requires external systems
- ❌ Over-engineering for audit logging

**Verdict**: ❌ Rejected - Too complex for this use case; better for event streaming

---

## Implementation Guidance

### 1. Register Interceptor

```csharp
// Idevs.Data/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddIdevsData(this IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<IdevsDbContext>((sp, options) =>
    {
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        
        // Register audit interceptor
        options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
    });
    
    // Register interceptor as scoped service
    services.AddScoped<AuditInterceptor>();
    
    return services;
}
```

### 2. Create Migration

```bash
dotnet ef migrations add AddAuditLogsTable -p src/Idevs.Data -s src/Idevs.Web
```

### 3. Configure Serilog

```csharp
// Program.cs
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.With<TenantEnricher>()
    .Enrich.With<CorrelationIdEnricher>()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(new JsonFormatter(), "logs/audit-.json", rollingInterval: RollingInterval.Day));
```

### 4. Add Middleware

```csharp
// Program.cs
app.UseMiddleware<AuditLoggingMiddleware>();
```

### 5. Testing Audit Logging

```csharp
// Tests/Idevs.IntegrationTests/AuditLoggingTests.cs
public class AuditLoggingTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Creating_Entity_Generates_Audit_Log()
    {
        // Arrange
        var order = new Order { TenantId = _tenantId, Total = 100 };
        
        // Act
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        
        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Order" && a.EntityId == order.Id.ToString())
            .FirstOrDefaultAsync();
        
        auditLog.ShouldNotBeNull();
        auditLog.Action.ShouldBe("Added");
        auditLog.TenantId.ShouldBe(_tenantId);
        auditLog.AfterValues.ShouldContain("\"Total\":100");
    }
    
    [Fact]
    public async Task Modifying_Entity_Captures_Before_And_After()
    {
        // Arrange
        var order = new Order { TenantId = _tenantId, Total = 100 };
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        
        // Act
        order.Total = 200;
        await _context.SaveChangesAsync();
        
        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Order" && a.Action == "Modified")
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();
        
        auditLog.ShouldNotBeNull();
        auditLog.BeforeValues.ShouldContain("\"Total\":100");
        auditLog.AfterValues.ShouldContain("\"Total\":200");
        auditLog.ChangedProperties.ShouldContain("Total");
    }
    
    [Fact]
    public async Task PII_Fields_Are_Redacted_In_Audit_Logs()
    {
        // Arrange
        var user = new User 
        { 
            TenantId = _tenantId, 
            Email = "test@example.com",
            PhoneNumber = "555-1234"
        };
        
        // Act
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        
        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "User")
            .FirstOrDefaultAsync();
        
        auditLog.ShouldNotBeNull();
        auditLog.AfterValues.ShouldContain("[REDACTED]");
        auditLog.AfterValues.ShouldNotContain("test@example.com");
        auditLog.AfterValues.ShouldNotContain("555-1234");
    }
}
```

---

## Decision Drivers

1. **Compliance First**: Meets SOC 2, GDPR, HIPAA requirements automatically
2. **Developer Experience**: No manual logging required (interceptor handles it)
3. **Performance**: Minimal overhead (<50ms P99)
4. **Multi-Tenancy**: Tenant isolation built-in
5. **Observability**: Structured logs with correlation IDs
6. **Cost Management**: Retention policies prevent unbounded growth

---

## References

- [ADR-0001: Tenancy Strategy](./ADR-0001-Tenancy-Strategy.md)
- [Threat Model](../threat-model.md)
- [Discovery Summary - Regulated Tenant](../discovery-summary.md#4-regulated-tenant)
- [EF Core Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [Serilog Documentation](https://serilog.net/)
- [PostgreSQL Table Partitioning](https://www.postgresql.org/docs/current/ddl-partitioning.html)
- [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)

---

## Approval

- [ ] Architecture Team
- [ ] Security Team
- [ ] Compliance Team
- [ ] Platform Lead
- [ ] DevOps Team

**Target Approval Date**: 2025-10-10

---

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-04 | 1.0 | Initial draft | Architecture Team |
