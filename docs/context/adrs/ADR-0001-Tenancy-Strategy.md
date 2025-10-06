# ADR-0001: Tenancy Strategy

**Status**: Accepted  
**Date**: 2025-10-04  
**Deciders**: Architecture Team, Platform Lead, Security Team  
**Related**: Phase 0 (Discovery), Threat Model

---

## Context

The Idevs framework must support multiple tenancy models to serve diverse customer needs:

- **Small businesses**: Cost-efficient shared infrastructure
- **Enterprise customers**: Dedicated resources with strong isolation
- **Regulated industries**: Enhanced isolation with RLS
- **Retail/field operations**: Offline-first scenarios

### Tenant Personas (from Discovery)

1. **Tiered Multi-Tenant**: Shared database, row-level isolation (cost-optimized)
2. **Dedicated Tenant**: Isolated database or schema (maximum isolation)
3. **Offline-First**: Mobile/field devices with sync requirements
4. **Regulated Tenant**: HIPAA/SOC2/GDPR compliance with audit trails

### Requirements

1. **Data Isolation**: Complete separation of tenant data (zero cross-tenant leakage)
2. **Cost Efficiency**: Shared infrastructure where appropriate
3. **Flexibility**: Support migration from shared → dedicated as customers grow
4. **Performance**: Tenant filtering must not significantly impact query performance
5. **Developer Experience**: Tenant filtering should be automatic (not manual in every query)

---

## Decision

**Implement row-level multi-tenancy with `tenant_id` column as the default strategy, with optional PostgreSQL Row-Level Security (RLS) for enhanced isolation.**

### Default Strategy: Row-Level Tenancy

Every multi-tenant table includes a `tenant_id` column:

```sql
CREATE TABLE orders (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,  -- Foreign key to tenants table
    customer_id UUID NOT NULL,
    total DECIMAL(10,2),
    created_at TIMESTAMPTZ NOT NULL,
    -- Other columns...
    
    CONSTRAINT fk_orders_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);

CREATE INDEX idx_orders_tenant_id ON orders(tenant_id);
```text

### Automatic Filtering

**Layer 1: EF Core Global Query Filters**

```csharp
// DbContext configuration
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply tenant filter to all ITenantEntity implementations
    modelBuilder.Entity<Order>()
        .HasQueryFilter(o => o.TenantId == _tenantContext.TenantId);
    
    modelBuilder.Entity<Customer>()
        .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
    
    // ... apply to all tenant-scoped entities
}
```text

**Layer 2: Repository-Level Filtering**

```csharp
public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class, ITenantEntity
{
    private readonly DbContext _context;
    private readonly ITenantContext _tenantContext;
    
    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Tenant filter applied automatically by global query filter
        return await _context.Set<TEntity>()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
    
    public IQueryable<TEntity> Query()
    {
        // Always returns tenant-filtered query
        return _context.Set<TEntity>().AsQueryable();
    }
}
```text

**Layer 3: Optional PostgreSQL RLS (Enhanced Isolation)**

For regulated industries or enhanced security:

```sql
-- Enable RLS on table
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;

-- Create policy
CREATE POLICY tenant_isolation_policy ON orders
    USING (tenant_id = current_setting('app.current_tenant_id')::UUID);

-- Grant access
GRANT ALL ON orders TO app_role;
```text

Application sets tenant context per connection:

```csharp
// Set tenant context for connection
await connection.ExecuteAsync(
    "SELECT set_config('app.current_tenant_id', @tenantId, false)",
    new { tenantId = _tenantContext.TenantId });
```text

### Tenant Resolution Precedence

```text
1. Explicit Header: X-Tenant-Id (highest priority)
   ↓
2. JWT Token Claim: "tenant_id" claim
   ↓
3. Subdomain: {tenant}.yourdomain.com
   ↓
4. Query Parameter: ?tenantId= (development/testing only)
   ↓
5. Fallback/Error: No tenant resolved → 401 Unauthorized
```text

### Configuration-Based Isolation Levels

```csharp
// appsettings.json
{
  "Tenancy": {
    "IsolationLevel": "RowLevel",  // RowLevel | Schema | Database
    "EnableRLS": false,              // Optional PostgreSQL RLS
    "RequireExplicitTenant": true,  // Reject requests without tenant
    "AllowTenantSwitching": false   // Security: prevent mid-request switching
  }
}
```text

---

## Consequences

### ✅ Positive Consequences

1. **Cost Efficient**
   - Shared database and schema = minimal infrastructure
   - No per-tenant database management overhead
   - Easy to provision new tenants (just INSERT into tenants table)

2. **Flexible Migration Path**
   - Start with shared database
   - Move high-value customers to dedicated later
   - Application code doesn't change (same `ITenantEntity` interface)

3. **Developer Experience**
   - Global query filters = automatic tenant scoping
   - Developers rarely write `WHERE tenant_id = @tenantId`
   - Repository pattern abstracts tenant filtering

4. **Performance**
   - Single connection pool shared across tenants
   - PostgreSQL query planner optimizes `tenant_id` filters efficiently
   - Indexes on `tenant_id` ensure fast lookups

5. **Observability**
   - All tenants visible in single database
   - Easy to monitor cross-tenant metrics
   - Centralized logging and tracing

### ⚠️ Negative Consequences (with Mitigations)

1. **Risk of Data Leakage**
   - **Risk**: Developer forgets to apply tenant filter
   - **Mitigation 1**: Global query filters (Layer 1)
   - **Mitigation 2**: Repository abstraction (Layer 2)
   - **Mitigation 3**: Optional RLS (Layer 3 - PostgreSQL enforces at DB level)
   - **Mitigation 4**: Architecture tests validate filters:

     ```csharp
     [Fact]
     public void All_Entities_With_TenantId_Must_Have_Query_Filter()
     {
         var tenantEntities = typeof(ITenantEntity).Assembly
             .GetTypes()
             .Where(t => typeof(ITenantEntity).IsAssignableFrom(t))
             .Where(t => !t.IsAbstract);
         
         foreach (var entity in tenantEntities)
         {
             var filter = _dbContext.Model.FindEntityType(entity)
                 ?.GetQueryFilter();
             
             filter.ShouldNotBeNull($"{entity.Name} must have a tenant query filter");
         }
     }
     ```

2. **Noisy Neighbor Problem**
   - **Risk**: One tenant's heavy queries impact others
   - **Mitigation 1**: Connection pooling with max connections per tenant
   - **Mitigation 2**: Query timeout policies
   - **Mitigation 3**: Rate limiting at API layer
   - **Mitigation 4**: Dedicated tier for high-volume tenants

3. **Unique Constraints Complexity**
   - **Risk**: `UNIQUE` constraints must be tenant-scoped
   - **Example Problem**:

     ```sql
     -- ❌ Wrong: Global unique email (cross-tenant)
     CREATE UNIQUE INDEX idx_users_email ON users(email);
     
     -- ✅ Correct: Unique per tenant
     CREATE UNIQUE INDEX idx_users_email_per_tenant 
         ON users(tenant_id, email);
     ```

   - **Mitigation**: Code review checklist for unique indexes

4. **Soft-Delete + Unique Index**
   - **Risk**: Soft-deleted records block unique constraints
   - **Example**:

     ```sql
     -- Customer deletes account (soft-delete)
     -- Later tries to re-register with same email → fails!
     ```

   - **Mitigation**: Partial unique indexes:

     ```sql
     CREATE UNIQUE INDEX idx_users_email_active 
         ON users(tenant_id, email) 
         WHERE is_deleted = false;
     ```

5. **Migration Coordination**
   - **Risk**: Schema changes affect all tenants simultaneously
   - **Mitigation 1**: Test migrations on staging tenant first
   - **Mitigation 2**: Use online schema change tools (e.g., `pg_repack`)
   - **Mitigation 3**: Schedule migrations during low-traffic windows

---

## Alternatives Considered

### Alternative 1: Schema-per-Tenant

**Approach**: Each tenant gets its own PostgreSQL schema

```sql
CREATE SCHEMA tenant_acme;
CREATE TABLE tenant_acme.orders (...);

CREATE SCHEMA tenant_xyz;
CREATE TABLE tenant_xyz.orders (...);
```text

**Pros**:

- Stronger isolation than row-level
- Easier to backup/restore individual tenants
- Can use standard `UNIQUE` constraints without `tenant_id`

**Cons**:

- ❌ Connection string must include schema: `SET search_path = tenant_acme`
- ❌ Migration complexity: Must run migration for each schema
- ❌ Cross-tenant reporting requires complex queries across schemas
- ❌ PostgreSQL has limits on number of schemas (~1000s practical limit)

**Verdict**: ❌ Rejected - Too complex for small/medium scale

---

### Alternative 2: Database-per-Tenant

**Approach**: Each tenant gets its own PostgreSQL database

```text
postgresql://host/tenant_acme
postgresql://host/tenant_xyz
```text

**Pros**:

- Maximum isolation
- Easy to backup/restore individual tenants
- Can host tenants on different database servers
- Regulatory compliance easier (physical separation)

**Cons**:

- ❌ Connection pool per database (resource intensive)
- ❌ Must manage hundreds/thousands of databases
- ❌ Cross-tenant reporting nearly impossible
- ❌ Migration complexity: Must run migration for each database
- ❌ Higher infrastructure costs

**Verdict**: ✅ Supported as opt-in for "Dedicated Tenant" persona, but not default

---

### Alternative 3: Discriminator Column (EF Core TPH)

**Approach**: Use EF Core's Table-Per-Hierarchy with discriminator

```csharp
modelBuilder.Entity<Order>()
    .HasDiscriminator<string>("TenantType")
    .HasValue<OrderAcme>("Acme")
    .HasValue<OrderXyz>("Xyz");
```text

**Cons**:

- ❌ Requires separate entity type per tenant (doesn't scale)
- ❌ Code generation complexity
- ❌ Not a standard multi-tenancy pattern

**Verdict**: ❌ Rejected - Not scalable

---

## Implementation Guidance

### 1. Entity Design

```csharp
// Idevs/Domain/Entities/Order.cs
public class Order : IEntity<Guid>, ITenantEntity, IAuditableEntity, ISoftDeletableEntity
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }  // ← Required for multi-tenancy
    
    public Guid CustomerId { get; init; }
    public decimal Total { get; init; }
    
    // Audit fields
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```text

### 2. Migration Template

```csharp
// Migrations/20250104_CreateOrders.cs
public partial class CreateOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "orders",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                tenant_id = table.Column<Guid>(nullable: false),
                customer_id = table.Column<Guid>(nullable: false),
                total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_orders", x => x.id);
                table.ForeignKey("fk_orders_tenant", x => x.tenant_id, "tenants", "id");
            });
        
        // ⚠️ CRITICAL: Always index tenant_id
        migrationBuilder.CreateIndex(
            name: "ix_orders_tenant_id",
            table: "orders",
            column: "tenant_id");
        
        // Composite indexes for common queries
        migrationBuilder.CreateIndex(
            name: "ix_orders_tenant_customer",
            table: "orders",
            columns: new[] { "tenant_id", "customer_id" });
    }
}
```text

### 3. Testing Tenant Isolation

```csharp
// Tests/Idevs.IntegrationTests/TenantIsolationTests.cs
public class TenantIsolationTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Cannot_Access_Other_Tenant_Data()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        
        await using var context1 = CreateContextForTenant(tenant1);
        await context1.Orders.AddAsync(new Order { TenantId = tenant1, Total = 100 });
        await context1.SaveChangesAsync();
        
        await using var context2 = CreateContextForTenant(tenant2);
        
        // Act
        var orders = await context2.Orders.ToListAsync();
        
        // Assert
        orders.ShouldBeEmpty(); // Tenant 2 should not see Tenant 1's orders
    }
    
    [Fact]
    public async Task Global_Query_Filter_Applied_To_All_Tenant_Entities()
    {
        // This test validates that we haven't forgotten to add query filters
        var tenantEntityTypes = typeof(ITenantEntity).Assembly
            .GetTypes()
            .Where(t => typeof(ITenantEntity).IsAssignableFrom(t))
            .Where(t => t.IsClass && !t.IsAbstract);
        
        await using var context = CreateContext();
        
        foreach (var entityType in tenantEntityTypes)
        {
            var filter = context.Model.FindEntityType(entityType)?.GetQueryFilter();
            filter.ShouldNotBeNull($"{entityType.Name} must have a global query filter for tenant isolation");
        }
    }
}
```text

---

## Decision Drivers

1. **Cost Efficiency**: Shared infrastructure is most cost-effective for SaaS
2. **Developer Experience**: Automatic filtering reduces errors
3. **Flexibility**: Can migrate to dedicated databases later
4. **Security**: Multiple layers of defense (EF filters + optional RLS)
5. **Performance**: Proven pattern for millions of tenants (Salesforce, Zendesk, etc.)

---

## References

- [Phase 0: Discovery - Tenant Personas](../phase-0-discovery/phase-0-discovery.md)
- [Threat Model: Multi-Tenant Isolation](../threat-model.md)
- [PostgreSQL Row-Level Security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)
- [Multi-Tenant Data Architecture (Microsoft)](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/approaches/tenant-isolation-strategies)

---

## Approval

- [x] Architecture Team
- [x] Security Team
- [x] Platform Lead
- [ ] Product Owner (pending)

**Approval Date**: 2025-10-04

---

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-04 | 1.0 | Initial version | Architecture Team |
