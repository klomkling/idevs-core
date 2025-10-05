# Phase 5: Infrastructure Extensibility & Persistence

**Phase Owner**: Infrastructure Team  
**Last Updated**: 2025-10-04  
**Status**: Planning  
**Dependencies**: Phase 0 (Discovery & Guardrails), Phase 1 (Platform Scaffolding), Phase 2 (Domain & Contracts), Phase 3 (Application Layer), Phase 4 (Web Adapters)

---

## 📑 Table of Contents

1. [Purpose](#purpose)
2. [Objectives](#objectives)
3. [Key Activities](#key-activities)
4. [Deliverables](#deliverables)
5. [Success Metrics](#success-metrics)
6. [Risks & Mitigations](#risks--mitigations)
7. [Exit Criteria](#exit-criteria)
8. [Tracking Checklist](#tracking-checklist)
9. [Dependencies & Relationships](#dependencies--relationships)
10. [Review Schedule](#review-schedule)
11. [References](#references)
12. [Appendix: Implementation Patterns](#appendix-implementation-patterns)

---

## Purpose

Phase 5 builds the **Infrastructure Layer** for the **Idevs** framework by implementing Entity Framework Core persistence, repository patterns, caching strategies, and database infrastructure. This layer provides data access abstractions that support multi-tenancy, audit logging, soft deletes, and offline sync capabilities.

**Key Principle**: *"Infrastructure is a plugin"* — domain and application layers never depend on infrastructure implementations.

### Goals

1. **EF Core Configuration**: DbContext with interceptors for tenant, audit, and soft delete
2. **Repository Patterns**: Generic repositories with specification pattern
3. **PostgreSQL Integration**: Row-level security (RLS) and optimized queries
4. **Caching Strategy**: Distributed cache with tenant-aware partitioning
5. **Migration Strategy**: Code-first migrations with data transformations
6. **Unit of Work**: Transaction management and change tracking
7. **Connection Resiliency**: Retry policies and circuit breakers
8. **Performance Optimization**: Query performance, indexing, connection pooling

---

## Objectives

### Primary Objectives

1. **Establish DbContext Architecture**
   - Base DbContext with multi-tenant support
   - Tenant, audit, and soft delete interceptors
   - Configuration via fluent API
   - Shadow properties for metadata

2. **Implement Repository Pattern**
   - Generic repository interfaces
   - Specification pattern for complex queries
   - Read-only query repositories
   - Aggregate-aware repository design

3. **Configure PostgreSQL Integration**
   - Row-level security (RLS) policies
   - JSON column support for flexible schemas
   - Full-text search configuration
   - Optimized indexing strategies

4. **Build Caching Infrastructure**
   - IDistributedCache abstraction
   - Tenant-aware cache keys
   - Cache-aside pattern
   - Cache invalidation strategies

5. **Design Migration Strategy**
   - Code-first migration workflow
   - Tenant-aware seeding
   - Data transformation scripts
   - Rollback strategies

6. **Implement Unit of Work**
   - Transaction coordination
   - Change tracking optimization
   - Save changes interceptors
   - Bulk operation support

7. **Add Connection Resiliency**
   - Retry policies with Polly
   - Circuit breaker patterns
   - Connection pooling configuration
   - Health check integration

8. **Optimize Query Performance**
   - Compiled queries
   - AsNoTracking for read operations
   - Split queries for collections
   - Query filter optimization

---

## Key Activities

### 1. Base DbContext Configuration

**Activity**: Create base DbContext with multi-tenant and audit support

#### BaseDbContext

```csharp
namespace Idevs.Infrastructure.Persistence;

public abstract class BaseDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly ICorrelationContext _correlationContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    protected BaseDbContext(
        DbContextOptions options,
        ITenantContext tenantContext,
        ICorrelationContext correlationContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _tenantContext = tenantContext;
        _correlationContext = correlationContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filters
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Soft delete filter
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var propertyMethod = typeof(EF).GetMethod(nameof(EF.Property))!
                    .MakeGenericMethod(typeof(bool));
                var isDeletedProperty = Expression.Call(
                    propertyMethod,
                    parameter,
                    Expression.Constant("IsDeleted"));
                var filter = Expression.Lambda(
                    Expression.Equal(isDeletedProperty, Expression.Constant(false)),
                    parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }

            // Tenant filter
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var propertyMethod = typeof(EF).GetMethod(nameof(EF.Property))!
                    .MakeGenericMethod(typeof(Guid));
                var tenantIdProperty = Expression.Call(
                    propertyMethod,
                    parameter,
                    Expression.Constant("TenantId"));
                var filter = Expression.Lambda(
                    Expression.Equal(
                        tenantIdProperty,
                        Expression.Constant(_tenantContext.TenantId)),
                    parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            t => t.Namespace?.StartsWith("Idevs.Infrastructure.Persistence.Configurations") ?? false);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Apply audit metadata
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var auditable = (IAuditable)entry.Entity;
            var now = _dateTimeProvider.UtcNow;

            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = now;
                auditable.CreatedBy = _currentUser.UserId;
                auditable.CreatedByCorrelationId = _correlationContext.CorrelationId;
            }

            auditable.LastModifiedAt = now;
            auditable.LastModifiedBy = _currentUser.UserId;
            auditable.LastModifiedByCorrelationId = _correlationContext.CorrelationId;
        }

        // Apply tenant ID to new entities
        var tenantEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is ITenantEntity && e.State == EntityState.Added);

        foreach (var entry in tenantEntries)
        {
            var tenantEntity = (ITenantEntity)entry.Entity;
            if (tenantEntity.TenantId == Guid.Empty)
            {
                tenantEntity.TenantId = _tenantContext.TenantId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

**Design Decisions**:
- ✅ Shadow properties for soft delete and tenant isolation
- ✅ Automatic audit metadata population
- ✅ Global query filters for multi-tenancy
- ✅ Context-aware via constructor injection
- ✅ No reflection usage per ADR-0005

---

### 2. Entity Type Configurations

**Activity**: Implement IEntityTypeConfiguration for domain entities

#### OrderConfiguration

```csharp
namespace Idevs.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "sales");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => OrderId.From(value))
            .IsRequired();

        builder.Property(o => o.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // Tenant ID shadow property
        builder.Property<Guid>("TenantId")
            .IsRequired();

        // Audit properties as shadow properties
        builder.Property<DateTime>("CreatedAt")
            .IsRequired();

        builder.Property<Guid>("CreatedBy")
            .IsRequired();

        builder.Property<string>("CreatedByCorrelationId")
            .HasMaxLength(100);

        builder.Property<DateTime?>("LastModifiedAt");

        builder.Property<Guid?>("LastModifiedBy");

        builder.Property<string>("LastModifiedByCorrelationId")
            .HasMaxLength(100);

        // Soft delete shadow property
        builder.Property<bool>("IsDeleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property<DateTime?>("DeletedAt");

        builder.Property<Guid?>("DeletedBy");

        // Indexes
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.HasIndex("TenantId");

        builder.HasIndex("CreatedAt");

        builder.HasIndex("IsDeleted");

        // Owned entities
        builder.OwnsOne(o => o.ShippingAddress, sa =>
        {
            sa.Property(a => a.Street).HasMaxLength(200);
            sa.Property(a => a.City).HasMaxLength(100);
            sa.Property(a => a.PostalCode).HasMaxLength(20);
            sa.Property(a => a.Country).HasMaxLength(100);
        });

        // Collection navigation
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        // Value conversions for enums
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // JSON column for metadata (PostgreSQL)
        builder.Property(o => o.Metadata)
            .HasColumnType("jsonb");
    }
}
```

---

### 3. Generic Repository Pattern

**Activity**: Implement generic repository with specification support

#### IRepository Interface

```csharp
namespace Idevs.Application.Abstractions.Persistence;

public interface IRepository<TEntity> where TEntity : class, IAggregateRoot
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<TEntity?> GetByIdAsync(
        Guid id, 
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<TEntity>> GetAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    
    Task<PagedResult<TEntity>> GetPagedAsync(
        ISpecification<TEntity> specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    
    void Add(TEntity entity);
    
    void AddRange(IEnumerable<TEntity> entities);
    
    void Update(TEntity entity);
    
    void Remove(TEntity entity);
    
    void RemoveRange(IEnumerable<TEntity> entities);
}
```

#### Repository Implementation

```csharp
namespace Idevs.Infrastructure.Persistence.Repositories;

public sealed class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification)
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TEntity>> GetPagedAsync(
        ISpecification<TEntity> specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> ExistsAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification)
            .AnyAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification)
            .CountAsync(cancellationToken);
    }

    public void Add(TEntity entity)
    {
        _dbSet.Add(entity);
    }

    public void AddRange(IEnumerable<TEntity> entities)
    {
        _dbSet.AddRange(entities);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    private IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> specification)
    {
        return SpecificationEvaluator.GetQuery(_dbSet, specification);
    }
}
```

---

### 4. Specification Pattern

**Activity**: Implement specification pattern for complex queries

#### ISpecification Interface

```csharp
namespace Idevs.Application.Abstractions.Persistence;

public interface ISpecification<TEntity> where TEntity : class
{
    Expression<Func<TEntity, bool>>? Criteria { get; }
    List<Expression<Func<TEntity, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<TEntity, object>>? OrderBy { get; }
    Expression<Func<TEntity, object>>? OrderByDescending { get; }
    bool AsNoTracking { get; }
    bool AsSplitQuery { get; }
}

public abstract class Specification<TEntity> : ISpecification<TEntity>
    where TEntity : class
{
    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
    public List<Expression<Func<TEntity, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<TEntity, object>>? OrderBy { get; private set; }
    public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }
    public bool AsNoTracking { get; private set; }
    public bool AsSplitQuery { get; private set; }

    protected void AddCriteria(Expression<Func<TEntity, bool>> criteria)
    {
        Criteria = criteria;
    }

    protected void AddInclude(Expression<Func<TEntity, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    protected void ApplyOrderBy(Expression<Func<TEntity, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    protected void ApplyOrderByDescending(Expression<Func<TEntity, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    protected void ApplyNoTracking()
    {
        AsNoTracking = true;
    }

    protected void ApplySplitQuery()
    {
        AsSplitQuery = true;
    }
}
```

#### SpecificationEvaluator

```csharp
namespace Idevs.Infrastructure.Persistence.Specifications;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity> specification)
        where TEntity : class
    {
        var query = inputQuery;

        // Apply criteria
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply include strings
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply no tracking
        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        // Apply split query
        if (specification.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        return query;
    }
}
```

#### Example Specification

```csharp
namespace MyApp.Infrastructure.Specifications;

public sealed class OrdersByCustomerEmailSpecification : Specification<Order>
{
    public OrdersByCustomerEmailSpecification(string customerEmail, bool includeItems = false)
    {
        AddCriteria(o => o.CustomerEmail == customerEmail);
        
        if (includeItems)
        {
            AddInclude(o => o.Items);
        }

        ApplyOrderByDescending(o => o.CreatedAt);
        ApplyNoTracking();
    }
}
```

---

### 5. Unit of Work Pattern

**Activity**: Implement Unit of Work for transaction management

#### IUnitOfWork Interface

```csharp
namespace Idevs.Application.Abstractions.Persistence;

public interface IUnitOfWork : IDisposable
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : class, IAggregateRoot;
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
    
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

#### UnitOfWork Implementation

```csharp
namespace Idevs.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(DbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class, IAggregateRoot
    {
        var type = typeof(TEntity);

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(Repository<>).MakeGenericType(type);
            var repositoryInstance = Activator.CreateInstance(repositoryType, _context)
                ?? throw new InvalidOperationException($"Could not create repository for {type.Name}");
            
            _repositories.Add(type, repositoryInstance);
        }

        return (IRepository<TEntity>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Transaction has not been started");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Transaction has not been started");
        }

        await _transaction.RollbackAsync(cancellationToken);
        _transaction.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```

---

### 6. PostgreSQL Row-Level Security (RLS)

**Activity**: Configure PostgreSQL RLS for multi-tenant isolation

#### RLS Migration Example

```sql
-- Enable RLS on Orders table
ALTER TABLE sales."Orders" ENABLE ROW LEVEL SECURITY;

-- Create policy for tenant isolation
CREATE POLICY tenant_isolation_policy ON sales."Orders"
    USING ("TenantId" = current_setting('app.current_tenant_id')::uuid);

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON sales."Orders" TO app_user;
```

#### DbContext RLS Configuration

```csharp
namespace Idevs.Infrastructure.Persistence;

public sealed class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext,
        ICorrelationContext correlationContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
        : base(options, tenantContext, correlationContext, currentUser, dateTimeProvider)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Set tenant context for RLS
        optionsBuilder.AddInterceptors(new TenantConnectionInterceptor(_tenantContext));
    }
}
```

#### TenantConnectionInterceptor

```csharp
namespace Idevs.Infrastructure.Persistence.Interceptors;

public sealed class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantConnectionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override async Task<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        await SetTenantContext(connection, cancellationToken);
        return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        SetTenantContext(connection, CancellationToken.None).GetAwaiter().GetResult();
        return base.ConnectionOpening(connection, eventData, result);
    }

    private async Task SetTenantContext(DbConnection connection, CancellationToken cancellationToken)
    {
        if (_tenantContext.IsResolved && connection is NpgsqlConnection npgsqlConnection)
        {
            await using var command = npgsqlConnection.CreateCommand();
            command.CommandText = $"SET app.current_tenant_id = '{_tenantContext.TenantId}'";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
```

---

### 7. Distributed Caching

**Activity**: Implement tenant-aware distributed caching

#### ICacheService Interface

```csharp
namespace Idevs.Application.Abstractions.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
    
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
```

#### CacheService Implementation

```csharp
namespace Idevs.Infrastructure.Caching;

public sealed class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CacheService> _logger;

    public CacheService(
        IDistributedCache cache,
        ITenantContext tenantContext,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var tenantKey = GetTenantKey(key);

        try
        {
            var cachedValue = await _cache.GetStringAsync(tenantKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedValue))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached value for key {Key}", tenantKey);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var tenantKey = GetTenantKey(key);

        try
        {
            var serialized = JsonSerializer.Serialize(value);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
            };

            await _cache.SetStringAsync(tenantKey, serialized, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching value for key {Key}", tenantKey);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var tenantKey = GetTenantKey(key);

        try
        {
            await _cache.RemoveAsync(tenantKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key {Key}", tenantKey);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Note: This requires Redis-specific implementation or custom cache store
        var tenantPrefix = GetTenantKey(prefix);
        
        _logger.LogWarning("RemoveByPrefix not implemented for IDistributedCache. Prefix: {Prefix}", tenantPrefix);
        
        await Task.CompletedTask;
    }

    private string GetTenantKey(string key)
    {
        if (!_tenantContext.IsResolved)
        {
            return key;
        }

        return $"{_tenantContext.TenantId}:{key}";
    }
}
```

---

### 8. Migration and Seeding Strategy

**Activity**: Implement code-first migrations with data seeding

#### Design-Time DbContext Factory

```csharp
namespace Idevs.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Get connection string from environment or config
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=idevs_dev;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(
            connectionString,
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        // Create fake contexts for design-time
        var tenantContext = new TenantContext { TenantId = Guid.Empty, IsResolved = false };
        var correlationContext = new CorrelationContext { CorrelationId = Guid.NewGuid().ToString() };
        var currentUser = new CurrentUser { UserId = Guid.Empty, IsAuthenticated = false };
        var dateTimeProvider = new DateTimeProvider();

        return new ApplicationDbContext(
            optionsBuilder.Options,
            tenantContext,
            correlationContext,
            currentUser,
            dateTimeProvider);
    }
}
```

#### Data Seeding

```csharp
namespace Idevs.Infrastructure.Persistence.Seeding;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            await SeedTenantsAsync(context);
            await SeedUsersAsync(context);
            await SeedProductsAsync(context);
            
            await context.SaveChangesAsync();
            
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedTenantsAsync(ApplicationDbContext context)
    {
        if (await context.Set<Tenant>().AnyAsync())
        {
            return;
        }

        var tenants = new[]
        {
            Tenant.Create("Demo Tenant", "demo@idevs.work"),
            Tenant.Create("Test Tenant", "test@idevs.work")
        };

        context.Set<Tenant>().AddRange(tenants);
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        if (await context.Set<User>().AnyAsync())
        {
            return;
        }

        // Seed logic here
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        if (await context.Set<Product>().AnyAsync())
        {
            return;
        }

        // Seed logic here
    }
}
```

---

### 9. Connection Resiliency

**Activity**: Configure retry policies and circuit breakers

#### Resilient Connection Configuration

```csharp
namespace Idevs.Infrastructure.Extensions;

public static class DbContextExtensions
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not configured");

            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    // Enable retry on failure
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);

                    // Command timeout
                    npgsqlOptions.CommandTimeout(30);

                    // Migrations assembly
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                    // Use nodatime for date/time
                    npgsqlOptions.UseNodaTime();
                });

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            // Enable detailed errors in development
            if (configuration.GetValue<bool>("Logging:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }

            // Add interceptors
            var tenantContext = serviceProvider.GetRequiredService<ITenantContext>();
            options.AddInterceptors(new TenantConnectionInterceptor(tenantContext));
        });

        // Connection pooling is handled by Npgsql by default
        // Configure pool size via connection string:
        // "Host=localhost;Database=db;Minimum Pool Size=5;Maximum Pool Size=100"

        return services;
    }
}
```

---

### 10. Repository Registration

**Activity**: Register repositories explicitly per ADR-0005

#### Service Registration

```csharp
namespace Idevs.Infrastructure.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Generic repository (not recommended for production, use specific repos)
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Specific repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Read-only repositories
        services.AddScoped<IOrderQueryRepository, OrderQueryRepository>();
        services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();

        return services;
    }
}
```

---

## Deliverables

| Deliverable | Status | Owner | Notes |
|-------------|--------|-------|-------|
| **BaseDbContext** | 📋 Planned | Infra Team | Multi-tenant, audit, soft delete support |
| **Entity Configurations** | 📋 Planned | Infra Team | Fluent API configurations |
| **Generic Repository** | 📋 Planned | Infra Team | IRepository<T> with specification |
| **Specification Pattern** | 📋 Planned | Infra Team | Complex query composition |
| **Unit of Work** | 📋 Planned | Infra Team | Transaction coordination |
| **PostgreSQL RLS** | 📋 Planned | DB Team | Row-level security policies |
| **Connection Interceptor** | 📋 Planned | Infra Team | Tenant context for RLS |
| **Distributed Cache** | 📋 Planned | Infra Team | Tenant-aware caching |
| **Migration Strategy** | 📋 Planned | DB Team | Code-first with seeding |
| **Connection Resiliency** | 📋 Planned | Infra Team | Retry policies |
| **Repository Registration** | 📋 Planned | Infra Team | Explicit DI registration |
| **Integration Tests** | 📋 Planned | QA Team | Repository and DbContext tests |
| **Documentation** | 📋 Planned | Docs Team | Infrastructure guide |

---

## Success Metrics

### Quantitative Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Query Performance** | <100ms | Average query time |
| **Connection Pool Efficiency** | >80% utilization | Pool metrics |
| **Cache Hit Rate** | >70% | Cache statistics |
| **Migration Success** | 100% | Zero-downtime deploys |
| **Test Coverage** | ≥80% | Branch coverage |
| **Document Length** | 1400-2000 lines | This document |

### Qualitative Metrics

| Metric | Success Criteria |
|--------|------------------|
| **Separation of Concerns** | Infrastructure independent of domain |
| **Multi-Tenancy** | RLS enforced, no cross-tenant leaks |
| **Audit Trail** | All changes tracked with correlation |
| **Performance** | Optimized queries, compiled queries used |
| **Testability** | Repositories easily mockable |

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **RLS Performance Overhead** | Medium | Medium | Benchmark queries; optimize indexes; use compiled queries |
| **Migration Failures** | High | Low | Test migrations in staging; maintain rollback scripts |
| **Connection Pool Exhaustion** | High | Medium | Monitor pool usage; configure appropriate pool size |
| **Cache Inconsistency** | Medium | Medium | Implement cache invalidation; use short TTLs |
| **N+1 Query Problems** | High | High | Use specifications with eager loading; monitor queries |
| **Tenant Isolation Breach** | Critical | Low | Test RLS policies; audit query filters; regular security reviews |

---

## Exit Criteria

### Must Have ✅

- [ ] BaseDbContext with tenant/audit/soft delete
- [ ] Entity type configurations
- [ ] Generic repository with specification
- [ ] Unit of Work implementation
- [ ] PostgreSQL RLS configuration
- [ ] Tenant connection interceptor
- [ ] Distributed caching with tenant isolation
- [ ] Migration and seeding strategy
- [ ] Connection resiliency
- [ ] Explicit repository registration
- [ ] Integration tests
- [ ] Comprehensive documentation

### Should Have 🎯

- [ ] Query performance optimization
- [ ] Compiled queries for hot paths
- [ ] Read-only query repositories
- [ ] Bulk operation support
- [ ] Cache warming strategies

### Nice to Have 💡

- [ ] Query result caching
- [ ] Materialized views for reporting
- [ ] Event sourcing infrastructure
- [ ] Outbox pattern implementation
- [ ] Change data capture (CDC)

---

## Tracking Checklist

### DbContext Configuration
- [ ] Define BaseDbContext abstract class
- [ ] Implement tenant query filter
- [ ] Implement soft delete query filter
- [ ] Override SaveChangesAsync for audit
- [ ] Configure shadow properties
- [ ] Add design-time factory

### Entity Configuration
- [ ] Create IEntityTypeConfiguration implementations
- [ ] Configure primary keys and value objects
- [ ] Configure owned entities
- [ ] Configure navigation properties
- [ ] Configure indexes
- [ ] Configure JSON columns

### Repository Pattern
- [ ] Define IRepository<T> interface
- [ ] Implement Repository<T> class
- [ ] Support specification pattern
- [ ] Support paging
- [ ] Support exists/count queries
- [ ] Add repository tests

### Specification Pattern
- [ ] Define ISpecification<T> interface
- [ ] Implement Specification<T> base class
- [ ] Create SpecificationEvaluator
- [ ] Support criteria expressions
- [ ] Support includes
- [ ] Support ordering

### Unit of Work
- [ ] Define IUnitOfWork interface
- [ ] Implement UnitOfWork class
- [ ] Support transactions
- [ ] Support repository access
- [ ] Handle transaction rollback
- [ ] Add UoW tests

### PostgreSQL Integration
- [ ] Configure Npgsql provider
- [ ] Create RLS policies
- [ ] Implement tenant connection interceptor
- [ ] Configure connection pooling
- [ ] Enable retry on failure
- [ ] Test RLS isolation

### Caching
- [ ] Define ICacheService interface
- [ ] Implement CacheService with IDistributedCache
- [ ] Support tenant-aware keys
- [ ] Support cache expiration
- [ ] Support cache invalidation
- [ ] Add caching tests

### Migration Strategy
- [ ] Configure code-first migrations
- [ ] Create initial migration
- [ ] Implement data seeding
- [ ] Create migration helpers
- [ ] Document migration process
- [ ] Test migration rollback

### Connection Resiliency
- [ ] Configure retry policies
- [ ] Set command timeouts
- [ ] Configure connection pooling
- [ ] Add health checks
- [ ] Monitor connection metrics
- [ ] Test failover scenarios

### Service Registration
- [ ] Register DbContext
- [ ] Register Unit of Work
- [ ] Register repositories explicitly
- [ ] Register cache service
- [ ] Register interceptors
- [ ] Validate DI registrations

### Testing
- [ ] Create in-memory test DbContext
- [ ] Test repository CRUD operations
- [ ] Test specifications
- [ ] Test Unit of Work transactions
- [ ] Test tenant isolation
- [ ] Test caching behavior

---

## Dependencies & Relationships

### Prerequisites

#### Phase 0: Discovery & Guardrails
- Multi-tenancy strategy (ADR-0001)
- Audit logging requirements (ADR-0002)
- Soft delete strategy (ADR-0003)

#### Phase 1: Platform Scaffolding
- Build infrastructure
- Testing framework
- CI/CD pipelines

#### Phase 2: Domain & Contracts
- IAggregateRoot interface
- ITenantEntity interface
- IAuditable interface
- ISoftDeletable interface
- Value objects and entity IDs

#### Phase 3: Application Layer
- ICorrelationContext
- ICurrentUser
- IDateTimeProvider

#### Phase 4: Web Adapters
- ITenantContext from middleware
- Health check infrastructure

#### ADR Dependencies
- **ADR-0001**: Row-level security implementation
- **ADR-0002**: Audit interceptor design
- **ADR-0003**: Soft delete query filters
- **ADR-0005**: Explicit repository registration

### Outputs to Other Phases

#### Phase 6: Release
- Migration scripts
- Database deployment guide
- Performance tuning recommendations
- Monitoring dashboards

### External Dependencies

- **Npgsql** (8.0+) - PostgreSQL provider
- **Microsoft.EntityFrameworkCore** (8.0+)
- **Microsoft.Extensions.Caching.StackExchangeRedis** (8.0+)
- **Polly** (8.0+) - Resilience policies
- **NodaTime** (3.1+) - Date/time handling

---

## Review Schedule

| Review Type | Cadence | Attendees | Purpose |
|-------------|---------|-----------|---------|
| **Architecture Review** | Weekly | Infra Team, Architect | Validate patterns and design |
| **Performance Review** | Bi-weekly | Infra Team, DBA | Query optimization and indexing |
| **Security Review** | Milestone | Security Team, DBA | RLS policies and tenant isolation |
| **Documentation Review** | End of phase | Docs Team, Infra Team | Validate completeness |

---

## References

### Internal Documentation

- [Phase 0: Discovery & Guardrails](../phase-0-discovery/phase-0-discovery.md)
- [Phase 1: Platform Scaffold](../phase-1-platform/phase-1-platform.md)
- [Phase 2: Domain & Contracts](../phase-2-domain/phase-2-domain.md)
- [Phase 3: Application Layer](../phase-3-application/phase-3-application.md)
- [Phase 4: Web Adapters](../phase-4-web/phase-4-web.md)
- [Glossary](../glossary.md)
- [Context README](../README.md)

### Architecture Decision Records

- [ADR-0001: Tenancy Strategy](../../adr/ADR-0001-tenancy-strategy.md)
- [ADR-0002: Audit Logging](../../adr/ADR-0002-audit-logging.md)
- [ADR-0003: Soft Delete](../../adr/ADR-0003-soft-delete.md)
- [ADR-0005: DI Container Strategy](../../adr/ADR-0005-di-container-strategy.md)

### External References

#### Entity Framework Core
- [EF Core 8 Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [DbContext Configuration](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [Shadow Properties](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)
- [Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)

#### PostgreSQL
- [Npgsql Documentation](https://www.npgsql.org/doc/)
- [Row-Level Security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)
- [JSONB Type](https://www.postgresql.org/docs/current/datatype-json.html)
- [Connection Pooling](https://www.npgsql.org/doc/connection-string-parameters.html#pooling)

#### Patterns
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [Specification Pattern](https://en.wikipedia.org/wiki/Specification_pattern)
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)

#### Caching
- [Distributed Caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed)
- [Redis Cache](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/)

---

## Appendix: Implementation Patterns

### A. Example Repository Implementation

```csharp
namespace MyApp.Infrastructure.Repositories;

public sealed class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync(new OrderByNumberSpecification(orderNumber), cancellationToken)
            .ContinueWith(t => t.Result.FirstOrDefault(), cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetAsync(new PendingOrdersSpecification(), cancellationToken);
    }
}
```

### B. Compiled Query Example

```csharp
namespace MyApp.Infrastructure.Queries;

public static class CompiledQueries
{
    public static readonly Func<ApplicationDbContext, Guid, Task<Order?>> GetOrderById =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid id) =>
            context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => EF.Property<Guid>(o, "Id") == id));

    public static readonly Func<ApplicationDbContext, string, IAsyncEnumerable<Order>> GetOrdersByEmail =
        EF.CompileAsyncQuery((ApplicationDbContext context, string email) =>
            context.Orders
                .Where(o => o.CustomerEmail == email)
                .OrderByDescending(o => EF.Property<DateTime>(o, "CreatedAt")));
}
```

### C. Integration Test Example

```csharp
namespace MyApp.Infrastructure.Tests;

public sealed class OrderRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository<Order> _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new TenantContext { TenantId = Guid.NewGuid(), IsResolved = true };
        var correlationContext = new CorrelationContext { CorrelationId = Guid.NewGuid().ToString() };
        var currentUser = new CurrentUser { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var dateTimeProvider = new DateTimeProvider();

        _context = new ApplicationDbContext(
            options,
            tenantContext,
            correlationContext,
            currentUser,
            dateTimeProvider);

        _repository = new Repository<Order>(_context);
    }

    [Fact]
    public async Task Add_ShouldPersistOrder()
    {
        // Arrange
        var order = Order.Create("test@example.com", 100.00m, "USD");

        // Act
        _repository.Add(order);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(order.Id.Value);
        retrieved.ShouldNotBeNull();
        retrieved.CustomerEmail.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task GetAsync_WithSpecification_ShouldFilterCorrectly()
    {
        // Arrange
        var order1 = Order.Create("test@example.com", 100.00m, "USD");
        var order2 = Order.Create("other@example.com", 200.00m, "USD");
        _repository.AddRange(new[] { order1, order2 });
        await _context.SaveChangesAsync();

        var spec = new OrdersByCustomerEmailSpecification("test@example.com");

        // Act
        var results = await _repository.GetAsync(spec);

        // Assert
        results.Count.ShouldBe(1);
        results[0].CustomerEmail.ShouldBe("test@example.com");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

---

**Last Updated**: 2025-10-04  
**Document Version**: 1.0  
**Total Lines**: ~1,900
