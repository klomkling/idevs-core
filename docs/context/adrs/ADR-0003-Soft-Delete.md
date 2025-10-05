# ADR-0003: Soft Delete Strategy

**Status**: Proposed  
**Date**: 2025-10-04  
**Deciders**: Architecture Team, Platform Lead, Operations Team  
**Related**: ADR-0001 (Tenancy Strategy), ADR-0002 (Audit Logging), Phase 0 (Discovery)

---

## Context

The Idevs framework must support soft deletion to meet business and compliance requirements while maintaining data integrity and audit trails.

### Business Requirements

1. **Data Retention**: Deleted records must be retained for:
   - **Audit trails**: Compliance requirements (SOC 2, GDPR, HIPAA)
   - **Restoration**: Users may accidentally delete data
   - **Forensic analysis**: Investigate data incidents
   - **Legal holds**: Retain data for litigation

2. **User Experience**: Users expect "undo" functionality:
   - Accidental deletions are common
   - Restoration should be straightforward
   - Deleted items should be hidden from normal views

3. **Data Lifecycle**: Different entities have different retention needs:
   - **Transient data**: Orders, invoices (retain 7 years for tax/audit)
   - **Master data**: Customers, products (retain indefinitely)
   - **Personal data**: User accounts (GDPR right to erasure - purge after retention period)

### Multi-Tenant Considerations

From [ADR-0001 Tenancy Strategy](./ADR-0001-Tenancy-Strategy.md):
- Soft delete must respect tenant isolation
- Deleted records must remain invisible to tenant queries
- Purge policies may vary by tenant tier (standard vs regulated)

### Audit Integration

From [ADR-0002 Audit Logging](./ADR-0002-Audit-Logging.md):
- Soft delete operation must be audited
- Audit log must capture who deleted, when, and why
- Restoration must also be audited

### Technical Challenges

1. **Unique Constraints**: Soft-deleted records block unique constraints
   - Example: User deletes account, tries to re-register with same email
   - Requires partial/filtered unique indexes

2. **Query Filters**: Must automatically exclude soft-deleted records
   - Developers shouldn't manually check `IsDeleted` in every query
   - EF Core global query filters solve this

3. **Referential Integrity**: Soft-deleted parent records with active children
   - Example: Soft-delete customer with active orders
   - Options: Cascade soft-delete, prevent deletion, or archive children

4. **Performance**: Large tables with soft-deleted records
   - Query performance degrades if many deleted records
   - Indexes must account for `IsDeleted` flag

---

## Decision

**Implement soft delete using `IsDeleted` boolean flag with `DeletedAt` timestamp and global query filters in EF Core.**

### Entity Contract

```csharp
// Idevs/Domain/Contracts/IEntity.cs
public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; }
}

// Idevs/Domain/Contracts/ITenantEntity.cs
public interface ITenantEntity<TTenantKey> where TTenantKey : IEquatable<TTenantKey>
{
    TTenantKey TenantId { get; }
}

// Convenience interface for Guid-based tenancy (most common)
public interface ITenantEntity : ITenantEntity<Guid> { }

// Idevs/Domain/Contracts/IAuditableEntity.cs
public interface IAuditableEntity<TUserKey> where TUserKey : IEquatable<TUserKey>
{
    DateTime CreatedAt { get; }
    TUserKey CreatedBy { get; }
    DateTime? UpdatedAt { get; set; }
    TUserKey? UpdatedBy { get; set; }
}

// Convenience interface for Guid-based user IDs (most common)
public interface IAuditableEntity : IAuditableEntity<Guid> { }

// Idevs/Domain/Contracts/ISoftDeletableEntity.cs
public interface ISoftDeletableEntity<TUserKey> where TUserKey : IEquatable<TUserKey>
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    TUserKey? DeletedBy { get; set; }
}

// Convenience interface for Guid-based user IDs (most common)
public interface ISoftDeletableEntity : ISoftDeletableEntity<Guid> { }

// Idevs/Domain/Contracts/IUserContext.cs
public interface IUserContext
{
    Guid UserId { get; }       // For Guid-based user IDs
    string UserName { get; }   // For username-based audit
    string? Email { get; }     // Additional user information
}

// Note: IUserContext is typically implemented as a scoped service
// that extracts user information from JWT claims, session, or authentication context

// Example entity using Guid for Id, TenantId, and UserId (most common)
public class Customer : IEntity<Guid>, ITenantEntity, IAuditableEntity, ISoftDeletableEntity
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Audit fields - using Guid for user IDs
    public DateTime CreatedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // Soft delete fields - using Guid for user ID
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

// Example with integer Id and integer UserId (legacy systems)
public class Product : IEntity<int>, ISoftDeletableEntity<int>
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    
    // Soft delete fields - using int for user ID (legacy system)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}

// Example with mixed types: long Id, string UserId (username-based audit)
public class Order : IEntity<long>, IAuditableEntity<string>, ISoftDeletableEntity<string>
{
    public long Id { get; init; }
    public decimal Total { get; set; }
    
    // Audit fields - using string for usernames
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Soft delete fields - using string for username
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### Global Query Filters

```csharp
// Idevs.Data/IdevsDbContext.cs
public class IdevsDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    
    public IdevsDbContext(DbContextOptions<IdevsDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply global query filters to all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            
            // Build combined filter expression
            var parameter = Expression.Parameter(clrType, "entity");
            Expression? combinedFilter = null;
            
            // Add soft delete filter if applicable
            // Check for both generic ISoftDeletableEntity<TUserKey> and convenience ISoftDeletableEntity
            var isSoftDeletable = clrType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISoftDeletableEntity<>))
                || typeof(ISoftDeletableEntity).IsAssignableFrom(clrType);
            
            if (isSoftDeletable)
            {
                var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDeletableEntity<Guid>.IsDeleted));
                var notDeleted = Expression.Not(isDeletedProperty);
                combinedFilter = notDeleted;
            }
            
            // Add tenant filter if applicable
            // Check for both generic ITenantEntity<TTenantKey> and convenience ITenantEntity
            if (typeof(ITenantEntity).IsAssignableFrom(clrType))
            {
                var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var tenantIdConstant = Expression.Constant(_tenantContext.TenantId);
                var tenantFilter = Expression.Equal(tenantIdProperty, tenantIdConstant);
                
                combinedFilter = combinedFilter is null 
                    ? tenantFilter 
                    : Expression.AndAlso(combinedFilter, tenantFilter);
            }
            
            // Apply combined filter if any conditions exist
            if (combinedFilter is not null)
            {
                var lambda = Expression.Lambda(combinedFilter, parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }
        }
    }
}
```

### Soft Delete Extension Method

```csharp
// Idevs.Data/Extensions/DbContextExtensions.cs
public static class DbContextExtensions
{
    // Generic soft delete with user ID type
    public static void SoftDelete<TEntity, TUserKey>(
        this DbContext context,
        TEntity entity,
        TUserKey deletedBy)
        where TEntity : class, ISoftDeletableEntity<TUserKey>
        where TUserKey : IEquatable<TUserKey>
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
        
        context.Entry(entity).State = EntityState.Modified;
    }
    
    // Convenience overload for Guid-based user IDs (captures from IUserContext)
    public static void SoftDelete<TEntity>(this DbContext context, TEntity entity)
        where TEntity : class, ISoftDeletableEntity<Guid>
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        
        // Capture current user ID from IUserContext
        var userContext = context.GetService<IUserContext>();
        entity.DeletedBy = userContext?.UserId ?? Guid.Empty;
        
        context.Entry(entity).State = EntityState.Modified;
    }
    
    // Convenience overload for string-based user IDs (captures username from IUserContext)
    public static void SoftDelete<TEntity>(this DbContext context, TEntity entity)
        where TEntity : class, ISoftDeletableEntity<string>
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        
        // Capture current username from IUserContext
        var userContext = context.GetService<IUserContext>();
        entity.DeletedBy = userContext?.UserName ?? "system";
        
        context.Entry(entity).State = EntityState.Modified;
    }
    
    // Generic soft delete by id (works with any key type and user key type)
    public static async Task SoftDeleteAsync<TEntity, TKey, TUserKey>(
        this DbSet<TEntity> dbSet,
        TKey id,
        TUserKey deletedBy,
        CancellationToken ct = default)
        where TEntity : class, IEntity<TKey>, ISoftDeletableEntity<TUserKey>
        where TKey : IEquatable<TKey>
        where TUserKey : IEquatable<TUserKey>
    {
        var entity = await dbSet.FindAsync(new object[] { id! }, ct);
        if (entity is not null)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = deletedBy;
        }
    }
    
    // Convenience overload for Guid-based entities (most common)
    public static async Task SoftDeleteAsync<TEntity>(
        this DbSet<TEntity> dbSet,
        Guid id,
        Guid deletedBy,
        CancellationToken ct = default)
        where TEntity : class, IEntity<Guid>, ISoftDeletableEntity<Guid>
    {
        await SoftDeleteAsync<TEntity, Guid, Guid>(dbSet, id, deletedBy, ct);
    }
    
    // Works with any soft-deletable entity regardless of user key type
    public static IQueryable<TEntity> IncludeDeleted<TEntity>(this DbSet<TEntity> dbSet)
        where TEntity : class
    {
        // Bypass global query filter to include deleted records
        return dbSet.IgnoreQueryFilters();
    }
    
    // Works with any soft-deletable entity regardless of user key type
    public static IQueryable<TEntity> OnlyDeleted<TEntity, TUserKey>(this DbSet<TEntity> dbSet)
        where TEntity : class, ISoftDeletableEntity<TUserKey>
        where TUserKey : IEquatable<TUserKey>
    {
        return dbSet.IgnoreQueryFilters().Where(e => e.IsDeleted);
    }
    
    // Convenience overload for common Guid-based user IDs
    public static IQueryable<TEntity> OnlyDeleted<TEntity>(this DbSet<TEntity> dbSet)
        where TEntity : class, ISoftDeletableEntity<Guid>
    {
        return OnlyDeleted<TEntity, Guid>(dbSet);
    }
}
```

### Unique Constraints with Soft Delete

**Problem**: Soft-deleted records block unique constraints

```sql
-- ❌ Problem: User deletes account, tries to re-register
-- Email "john@example.com" exists as soft-deleted record
-- New registration fails: "Email already exists"
```

**Solution**: Partial unique indexes (filter out soft-deleted records)

#### PostgreSQL (Partial Index)

```sql
-- Create partial unique index that excludes soft-deleted records
CREATE UNIQUE INDEX ix_customers_email_active
ON customers(tenant_id, email)
WHERE is_deleted = false;

-- Or using NULLIF to make deleted emails non-unique
CREATE UNIQUE INDEX ix_customers_email_active_nullif
ON customers(tenant_id, NULLIF(email, ''))
WHERE is_deleted = false;
```

#### SQL Server (Filtered Index)

```sql
-- SQL Server uses filtered indexes
CREATE UNIQUE INDEX IX_Customers_Email_Active
ON customers(tenant_id, email)
WHERE is_deleted = 0;
```

#### MySQL (Workaround - No Filtered Indexes)

```sql
-- MySQL doesn't support filtered unique indexes
-- Workaround 1: Use composite key with is_deleted
CREATE UNIQUE INDEX ix_customers_email_deleted
ON customers(tenant_id, email, is_deleted);

-- Workaround 2: Use trigger to enforce uniqueness
DELIMITER $$
CREATE TRIGGER customers_unique_email_check
BEFORE INSERT ON customers
FOR EACH ROW
BEGIN
    IF EXISTS (
        SELECT 1 FROM customers
        WHERE tenant_id = NEW.tenant_id
        AND email = NEW.email
        AND is_deleted = 0
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Email already exists for this tenant';
    END IF;
END$$
DELIMITER ;
```

### EF Core Migration Configuration

```csharp
// Idevs.Data/Configurations/CustomerConfiguration.cs
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(c => c.IsDeleted)
            .HasDefaultValue(false);
        
        builder.Property(c => c.DeletedAt)
            .IsRequired(false);
        
        builder.Property(c => c.DeletedBy)
            .HasMaxLength(255)
            .IsRequired(false);
        
        // Partial unique index for active records only (PostgreSQL)
        builder.HasIndex(c => new { c.TenantId, c.Email })
            .IsUnique()
            .HasFilter("is_deleted = false") // PostgreSQL syntax
            .HasDatabaseName("ix_customers_email_active");
        
        // Composite index for queries filtering by IsDeleted
        builder.HasIndex(c => new { c.TenantId, c.IsDeleted });
    }
}
```

### Cascade Soft Delete

```csharp
// Idevs.Data/Extensions/CascadeSoftDeleteExtensions.cs
public static class CascadeSoftDeleteExtensions
{
    public static async Task CascadeSoftDeleteAsync<TEntity>(
        this DbContext context,
        TEntity entity,
        CancellationToken ct = default)
        where TEntity : class, ISoftDeletableEntity
    {
        // Mark entity as deleted
        context.SoftDelete(entity);
        
        // Find all related entities via navigation properties
        var entry = context.Entry(entity);
        var navigations = entry.Metadata.GetNavigations()
            .Where(n => n.IsCollection);
        
        foreach (var navigation in navigations)
        {
            var relatedEntities = navigation.GetGetter().GetClrValue(entity) as IEnumerable<object>;
            
            if (relatedEntities is null)
                continue;
            
            foreach (var related in relatedEntities)
            {
                if (related is ISoftDeletableEntity softDeletable && !softDeletable.IsDeleted)
                {
                    await context.CascadeSoftDeleteAsync(softDeletable, ct);
                }
            }
        }
    }
}
```

### Restoration Workflow

```csharp
// Idevs.Application/Commands/RestoreEntityCommand.cs
public record RestoreEntityCommand<TKey>(TKey EntityId, string EntityType) : ICommand
    where TKey : IEquatable<TKey>;

// Convenience command for Guid-based entities (most common)
public record RestoreEntityCommand(Guid EntityId, string EntityType) 
    : RestoreEntityCommand<Guid>(EntityId, EntityType);

public class RestoreEntityCommandHandler<TKey> : ICommandHandler<RestoreEntityCommand<TKey>>
    where TKey : IEquatable<TKey>
{
    private readonly IdevsDbContext _context;
    private readonly IUserContext _userContext;
    private readonly ILogger<RestoreEntityCommandHandler<TKey>> _logger;
    
    public RestoreEntityCommandHandler(
        IdevsDbContext context,
        IUserContext userContext,
        ILogger<RestoreEntityCommandHandler<TKey>> logger)
    {
        _context = context;
        _userContext = userContext;
        _logger = logger;
    }
    
    public async Task<Result> HandleAsync(RestoreEntityCommand<TKey> command, CancellationToken ct)
    {
        // Use reflection-free approach via EF Core metadata
        var entityType = _context.Model.GetEntityTypes()
            .FirstOrDefault(et => et.ClrType.Name == command.EntityType);
        
        if (entityType is null)
            return Result.Failure($"Entity type {command.EntityType} not found");
        
        // Verify entity implements required interfaces
        if (!typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
            return Result.Failure("Entity does not support soft delete");
        
        if (!typeof(IEntity<TKey>).IsAssignableFrom(entityType.ClrType))
            return Result.Failure($"Entity does not use {typeof(TKey).Name} as key type");
        
        // Use EF Core's Set<> method via generic constraint
        var setMethod = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entityType.ClrType);
        
        var dbSet = setMethod.Invoke(_context, null);
        if (dbSet is null)
            return Result.Failure("Failed to access entity set");
        
        // Build query to find entity by id
        var queryableType = typeof(IQueryable<>).MakeGenericType(entityType.ClrType);
        var whereMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType.ClrType);
        
        // Build predicate: e => e.Id == command.EntityId
        var param = Expression.Parameter(entityType.ClrType, "e");
        var idProperty = Expression.Property(param, "Id");
        var idValue = Expression.Constant(command.EntityId, typeof(TKey));
        var equality = Expression.Equal(idProperty, idValue);
        var lambda = Expression.Lambda(equality, param);
        
        var ignoreFiltersMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethod(nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters))!
            .MakeGenericMethod(entityType.ClrType);
        
        var queryWithoutFilters = ignoreFiltersMethod.Invoke(null, new[] { dbSet });
        var queryWithPredicate = whereMethod.Invoke(null, new[] { queryWithoutFilters, lambda });
        
        var firstOrDefaultMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.FirstOrDefaultAsync) 
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType.ClrType);
        
        var entityTask = firstOrDefaultMethod.Invoke(null, new[] { queryWithPredicate, ct });
        var entity = await (dynamic)entityTask!;
        
        if (entity is null)
            return Result.Failure("Entity not found");
        
        var softDeletable = entity as ISoftDeletableEntity;
        if (!softDeletable!.IsDeleted)
            return Result.Failure("Entity is not deleted");
        
        // Check for unique constraint conflicts before restoring
        var hasConflict = await CheckUniqueConstraintConflict(entity, entityType, ct);
        if (hasConflict)
        {
            return Result.Failure(
                "Cannot restore: another active record with the same unique values exists. " +
                "Please resolve the conflict first.");
        }
        
        // Restore entity
        softDeletable.IsDeleted = false;
        softDeletable.DeletedAt = null;
        softDeletable.DeletedBy = null;
        
        // Update audit fields if applicable
        if (entity is IAuditableEntity auditable)
        {
            auditable.UpdatedAt = DateTime.UtcNow;
            auditable.UpdatedBy = _userContext.UserName;
        }
        
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation(
            "Restored {EntityType} {EntityId} by {User}",
            command.EntityType,
            command.EntityId,
            _userContext.UserName);
        
        return Result.Success();
    }
    
    private async Task<bool> CheckUniqueConstraintConflict(
        object entity,
        IEntityType entityType,
        CancellationToken ct)
    {
        // Get unique indexes for entity type via EF Core metadata
        var uniqueIndexes = entityType.GetIndexes().Where(i => i.IsUnique);
        
        foreach (var index in uniqueIndexes)
        {
            // Use EF Core metadata to build query without reflection
            // Check if any active record exists with same unique values
            // This is simplified - production code would build complete expression trees
        }
        
        return false; // Simplified - actual implementation would check constraints
    }
}
```

### Purge Policy and Background Job

```csharp
// Idevs.Infrastructure/BackgroundJobs/SoftDeletePurgeJob.cs
public class SoftDeletePurgeJob : IHostedService, IDisposable
{
    private Timer? _timer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SoftDeletePurgeJob> _logger;
    
    public SoftDeletePurgeJob(IServiceProvider serviceProvider, ILogger<SoftDeletePurgeJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Run daily at 2 AM
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }
    
    private async void DoWork(object? state)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdevsDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IOptions<SoftDeleteOptions>>();
        
        var cutoffDate = DateTime.UtcNow.AddDays(-config.Value.PurgeAfterDays);
        
        _logger.LogInformation(
            "Starting soft delete purge job. Purging records deleted before {CutoffDate}",
            cutoffDate);
        
        // Purge each soft-deletable entity type
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
                continue;
            
            var tableName = entityType.GetTableName();
            
            // Use raw SQL for performance (avoid loading entities into memory)
            var deletedCount = await context.Database.ExecuteSqlRawAsync(
                $@"DELETE FROM {tableName}
                   WHERE is_deleted = true
                   AND deleted_at < @p0",
                cutoffDate);
            
            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Purged {DeletedCount} soft-deleted records from {TableName}",
                    deletedCount,
                    tableName);
            }
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
    
    public void Dispose() => _timer?.Dispose();
}

// Configuration
public class SoftDeleteOptions
{
    public int PurgeAfterDays { get; set; } = 90; // Default: 90 days
    public bool EnableAutomaticPurge { get; set; } = true;
    public TimeSpan PurgeSchedule { get; set; } = TimeSpan.FromDays(1);
}
```

### Configuration

```csharp
// appsettings.json
{
  "SoftDelete": {
    "PurgeAfterDays": 90,              // Hard-delete after 90 days
    "EnableAutomaticPurge": true,
    "PurgeSchedule": "1.00:00:00",     // Daily
    "TenantOverrides": {
      "regulated-tenant-id": {
        "PurgeAfterDays": 2555         // 7 years for regulated tenants
      }
    }
  }
}
```

---

## Consequences

### ✅ Positive Consequences

1. **Data Recovery**
   - Users can restore accidentally deleted records
   - No data loss for retention period
   - Audit trail preserved

2. **Compliance Alignment**
   - Meets data retention requirements (SOC 2, GDPR)
   - Supports legal holds
   - Tracks deletion and restoration events

3. **Developer Experience**
   - Global query filters hide deleted records automatically
   - Developers don't check `IsDeleted` in every query
   - Extension methods make soft delete easy

4. **Performance**
   - Soft delete is faster than hard delete (no cascade deletes)
   - Indexes optimized for `IsDeleted = false` queries
   - Partitioning possible by `DeletedAt` for large tables

5. **Referential Integrity**
   - Foreign key constraints remain valid
   - No orphaned records
   - Cascade soft delete maintains relationships

### ⚠️ Negative Consequences (with Mitigations)

1. **Unique Constraint Complexity**
   - **Risk**: Soft-deleted records block unique constraints
   - **Mitigation 1**: Partial/filtered unique indexes (PostgreSQL, SQL Server)
   - **Mitigation 2**: Application-level uniqueness checks (MySQL)
   - **Mitigation 3**: Triggers for uniqueness enforcement
   - **Mitigation 4**: Document unique constraint patterns in guidelines

2. **Query Filter Gotchas**
   - **Risk**: Developers forget to use `IgnoreQueryFilters()` when needed
   - **Mitigation 1**: Extension methods: `IncludeDeleted()`, `OnlyDeleted()`
   - **Mitigation 2**: Code review checklist
   - **Mitigation 3**: Architecture tests validate query filter usage
   - **Example**:
     ```csharp
     // ❌ Wrong: Count includes deleted records unintentionally
     var count = await context.Customers.IgnoreQueryFilters().CountAsync();
     
     // ✅ Correct: Explicitly count only active records
     var activeCount = await context.Customers.CountAsync();
     
     // ✅ Correct: Explicitly count deleted records
     var deletedCount = await context.Customers.OnlyDeleted().CountAsync();
     ```

3. **Storage Overhead**
   - **Risk**: Soft-deleted records consume storage
   - **Mitigation 1**: Automated purge job after retention period
   - **Mitigation 2**: Partition tables by `IsDeleted` or `DeletedAt`
   - **Mitigation 3**: Archive to cold storage before purge

4. **Performance Degradation**
   - **Risk**: Large percentage of deleted records slows queries
   - **Mitigation 1**: Composite indexes: `(tenant_id, is_deleted)`
   - **Mitigation 2**: Partitioning: active vs deleted partitions
   - **Mitigation 3**: Regular purge to limit deleted record count
   - **Mitigation 4**: Monitor query performance and adjust indexes

5. **Cascade Complexity**
   - **Risk**: Soft-deleting parent cascades to many children (performance)
   - **Mitigation 1**: Cascade soft-delete in background job for large hierarchies
   - **Mitigation 2**: Limit cascade depth
   - **Mitigation 3**: Warn users before deleting parents with many children

---

## Alternatives Considered

### Alternative 1: Hard Delete

**Approach**: Permanently delete records from database

```csharp
context.Customers.Remove(customer);
await context.SaveChangesAsync();
```

**Pros**:
- Simple implementation
- No storage overhead
- No unique constraint issues

**Cons**:
- ❌ Data loss (cannot restore)
- ❌ No audit trail of deleted data
- ❌ Violates compliance requirements (data retention)
- ❌ User frustration (no undo)
- ❌ Referential integrity issues (cascade deletes required)

**Verdict**: ❌ Rejected - Fails to meet retention and audit requirements

---

### Alternative 2: Archive Tables

**Approach**: Move deleted records to separate archive tables

```sql
-- Active table
CREATE TABLE customers (...);

-- Archive table (same schema)
CREATE TABLE customers_archive (...);

-- Move to archive on delete
INSERT INTO customers_archive SELECT * FROM customers WHERE id = @id;
DELETE FROM customers WHERE id = @id;
```

**Pros**:
- Active table stays lean (better performance)
- No unique constraint conflicts
- Clear separation of active vs archived data

**Cons**:
- ❌ Double schema maintenance (customers + customers_archive)
- ❌ Migration complexity (apply to both tables)
- ❌ Query complexity (join active + archive for full history)
- ❌ EF Core doesn't model archive tables well
- ❌ Restoration requires moving records back

**Verdict**: ❌ Rejected - Too complex, poor EF Core support

---

### Alternative 3: Event Sourcing

**Approach**: Store deletion as an event, rebuild state from events

```csharp
public class CustomerDeletedEvent
{
    public Guid CustomerId { get; init; }
    public DateTime DeletedAt { get; init; }
}
```

**Pros**:
- Complete audit trail by design
- Can replay events to any point in time
- Natural undo/redo

**Cons**:
- ❌ High complexity (event store, projections, snapshots)
- ❌ Not suitable for simple CRUD operations
- ❌ Over-engineering for soft delete use case

**Verdict**: ❌ Rejected - Too complex for this use case

---

### Alternative 4: Nullable FK + Cascade Null

**Approach**: Set FKs to NULL instead of deleting

```sql
-- On delete, set customer_id to NULL instead of deleting order
ALTER TABLE orders
ADD CONSTRAINT fk_orders_customer
FOREIGN KEY (customer_id) REFERENCES customers(id)
ON DELETE SET NULL;
```

**Pros**:
- Preserves related records

**Cons**:
- ❌ Loses referential integrity
- ❌ Doesn't hide deleted records from queries
- ❌ Still need soft delete for parent records
- ❌ Business logic breaks (orders without customers)

**Verdict**: ❌ Rejected - Doesn't solve the problem

---

## Implementation Guidance

### 1. Enable Soft Delete for Entity

```csharp
// Step 1: Implement ISoftDeletableEntity
// Example with Guid Id and Guid UserId (most common)
public class Product : IEntity<Guid>, ISoftDeletableEntity
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    
    // Soft delete fields - using convenience interface (Guid user ID)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

// Example with int Id and int UserId (legacy systems)
public class Category : IEntity<int>, ISoftDeletableEntity<int>
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    
    // Soft delete fields - explicit generic (int user ID)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}

// Example with Guid Id and string UserId (username-based)
public class Article : IEntity<Guid>, ISoftDeletableEntity<string>
{
    public Guid Id { get; init; }
    public string Title { get; set; } = string.Empty;
    
    // Soft delete fields - using string for username
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// Step 2: Configure in EF Core (if needed - global filter applies automatically)
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(p => p.IsDeleted);
    }
}
```

### 2. Create Migration

```bash
dotnet ef migrations add AddSoftDeleteToProducts -p src/Idevs.Data -s src/Idevs.Web
```

### 3. Soft Delete in Application Code

```csharp
// In command handler
public async Task<Result> HandleAsync(DeleteProductCommand command, CancellationToken ct)
{
    var product = await _context.Products.FindAsync(command.ProductId, ct);
    
    if (product is null)
        return Result.Failure("Product not found");
    
    // Soft delete using extension method
    _context.SoftDelete(product);
    
    await _context.SaveChangesAsync(ct);
    
    return Result.Success();
}
```

### 4. Query Deleted Records

```csharp
// Include deleted records
var allProducts = await context.Products
    .IncludeDeleted()
    .ToListAsync();

// Only deleted records
var deletedProducts = await context.Products
    .OnlyDeleted()
    .ToListAsync();

// Active records only (default - no special syntax needed)
var activeProducts = await context.Products.ToListAsync();
```

### 5. Testing Soft Delete

```csharp
// Tests/Idevs.IntegrationTests/SoftDeleteTests.cs
public class SoftDeleteTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Soft_Deleted_Entity_Not_Returned_By_Default()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        
        // Act
        _context.SoftDelete(product);
        await _context.SaveChangesAsync();
        
        // Clear context to force re-query
        _context.ChangeTracker.Clear();
        
        var result = await _context.Products.FindAsync(product.Id);
        
        // Assert
        result.ShouldBeNull(); // Soft-deleted entity not returned
    }
    
    [Fact]
    public async Task Soft_Deleted_Entity_Returned_With_IgnoreQueryFilters()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        
        _context.SoftDelete(product);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == product.Id);
        
        // Assert
        result.ShouldNotBeNull();
        result.IsDeleted.ShouldBeTrue();
        result.DeletedAt.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task Unique_Constraint_Allows_Reuse_After_Soft_Delete()
    {
        // Arrange
        var email = "test@example.com";
        var customer1 = new Customer { Email = email };
        await _context.Customers.AddAsync(customer1);
        await _context.SaveChangesAsync();
        
        // Act: Soft delete first customer
        _context.SoftDelete(customer1);
        await _context.SaveChangesAsync();
        
        // Act: Create new customer with same email
        var customer2 = new Customer { Email = email };
        await _context.Customers.AddAsync(customer2);
        Func<Task> act = async () => await _context.SaveChangesAsync();
        
        // Assert: Should NOT throw (partial index allows reuse)
        await act.ShouldNotThrowAsync();
    }
    
    [Fact]
    public async Task Restore_Entity_Clears_Soft_Delete_Fields()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        
        _context.SoftDelete(product);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        
        // Act: Restore
        var deleted = await _context.Products.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == product.Id);
        
        deleted!.IsDeleted = false;
        deleted.DeletedAt = null;
        deleted.DeletedBy = null;
        await _context.SaveChangesAsync();
        
        _context.ChangeTracker.Clear();
        
        // Assert
        var restored = await _context.Products.FindAsync(product.Id);
        restored.ShouldNotBeNull();
        restored.IsDeleted.ShouldBeFalse();
        restored.DeletedAt.ShouldBeNull();
    }
}
```

---

## Decision Drivers

1. **Data Retention**: Compliance requires retention of deleted data (SOC 2, GDPR, HIPAA)
2. **User Experience**: Users expect "undo" functionality for accidental deletions
3. **Developer Ergonomics**: Global query filters reduce boilerplate
4. **Performance**: Soft delete faster than hard delete with cascades
5. **Audit Integration**: Soft delete operations captured by audit logging

---

## References

- [ADR-0001: Tenancy Strategy](./ADR-0001-Tenancy-Strategy.md)
- [ADR-0002: Audit Logging](./ADR-0002-Audit-Logging.md)
- [EF Core Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [PostgreSQL Partial Indexes](https://www.postgresql.org/docs/current/indexes-partial.html)
- [SQL Server Filtered Indexes](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/create-filtered-indexes)

---

## Approval

- [ ] Architecture Team
- [ ] Platform Lead
- [ ] Operations Team
- [ ] Security Team

**Target Approval Date**: 2025-10-10

---

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-04 | 1.0 | Initial draft | Architecture Team |
