# Threat Model

**Document Owner**: Security & Compliance Team  
**Last Updated**: 2025-10-04  
**Status**: Baseline Established  
**ASVS Level**: Level 2 (Standard)

## Purpose

This document identifies security threats, assets, trust boundaries, and mitigations for the **Idevs** framework (repo: `idevs-core`). It serves as the baseline for security verification activities and guides secure development practices throughout all phases.

## Scope & Assets

### In-Scope Assets

| Asset | Sensitivity | Impact if Compromised |
|-------|-------------|----------------------|
| **Tenant Data** | Critical | Data breach, GDPR violations, reputation damage, legal liability |
| **Credentials & Secrets** | Critical | Unauthorized access, privilege escalation, lateral movement |
| **Audit Logs** | High | Evidence tampering, compliance violations, inability to detect breaches |
| **API Tokens (JWT)** | High | Impersonation, unauthorized operations, data exfiltration |
| **Configuration Data** | Medium | Information disclosure, system manipulation |
| **Source Code** | Medium | Intellectual property theft, vulnerability discovery |
| **Database Credentials** | Critical | Full data access, ransomware, data destruction |
| **Cache Data** | Medium | Information disclosure, cache poisoning |

### Out-of-Scope

- End-user applications consuming the framework (consumer responsibility)
- Infrastructure security (network, OS hardening) - deployment-specific
- Physical security of data centers
- Social engineering attacks on end users

## Trust Boundaries & Attack Surface

### Trust Boundaries

```
┌─────────────────────────────────────────────────────────────┐
│ External (Untrusted)                                        │
│  - HTTP/HTTPS Requests                                      │
│  - GraphQL Queries                                          │
│  - Offline Sync Clients                                     │
└─────────────────┬───────────────────────────────────────────┘
                  │ 🔒 Authentication & Authorization
┌─────────────────▼───────────────────────────────────────────┐
│ API Gateway / Web Layer (ASP.NET Core)                     │
│  - Middleware Pipeline                                      │
│  - Controllers & Minimal APIs                               │
│  - GraphQL Resolvers                                        │
└─────────────────┬───────────────────────────────────────────┘
                  │ 🔒 Tenant Context Validation
┌─────────────────▼───────────────────────────────────────────┐
│ Application Layer (Trusted)                                 │
│  - Command Handlers                                         │
│  - Query Handlers                                           │
│  - Decorators (Validation, Logging)                         │
└─────────────────┬───────────────────────────────────────────┘
                  │ 🔒 Tenant Filtering
┌─────────────────▼───────────────────────────────────────────┐
│ Persistence Layer                                           │
│  - EF Core + Interceptors                                   │
│  - PostgreSQL Database                                      │
│  - Cache (Redis/Memory)                                     │
└─────────────────────────────────────────────────────────────┘
```

### Attack Surface

| Entry Point | Exposed To | Risk Level |
|-------------|-----------|------------|
| HTTP/HTTPS Endpoints | Internet | High |
| GraphQL Endpoint | Internet | High |
| Database Connection | Internal Network | Medium |
| Cache Access | Internal Network | Low |
| Audit Log Storage | Internal Network | Low |
| Configuration Files | Deployment Pipeline | Medium |

## Threat Analysis (STRIDE)

### Spoofing (Identity)

| Threat | Likelihood | Impact | Mitigation | Status |
|--------|-----------|--------|------------|--------|
| **T1.1**: Attacker impersonates legitimate user | High | Critical | JWT authentication, short token lifetime, refresh token rotation | ✅ Planned |
| **T1.2**: Attacker forges tenant identifier | Medium | Critical | Signed tenant claims in JWT, tenant validation at middleware | ✅ Planned |
| **T1.3**: Session hijacking via XSS | Medium | High | HttpOnly cookies, SameSite=Strict, CSP headers, input sanitization | ✅ Planned |
| **T1.4**: API key leakage in logs/errors | Low | High | PII redaction, secret masking in logs, structured logging | ✅ Planned |

### Tampering (Data Integrity)

| Threat | Likelihood | Impact | Mitigation | Status |
|--------|-----------|--------|------------|--------|
| **T2.1**: SQL injection via unsanitized input | Medium | Critical | Parameterized queries (EF Core), input validation, FluentValidation | ✅ Planned |
| **T2.2**: Audit log modification | Low | High | Immutable audit logs, cryptographic signing, append-only store | ✅ Planned |
| **T2.3**: Command replay attacks | Medium | Medium | Idempotency keys, timestamp validation, nonce tracking | ✅ Planned |
| **T2.4**: Man-in-the-middle attacks | Low | High | TLS 1.2+, HSTS headers, certificate pinning (optional) | ✅ Planned |
| **T2.5**: Cache poisoning | Low | Medium | Tenant-aware cache keys, cache expiration, cache validation | ✅ Planned |

### Repudiation (Non-repudiation)

| Threat | Likelihood | Impact | Mitigation | Status |
|--------|-----------|--------|------------|--------|
| **T3.1**: User denies performing action | Medium | Medium | Comprehensive audit logging with user ID, tenant ID, correlation ID | ✅ Planned |
| **T3.2**: Missing audit trail for critical ops | Low | High | Mandatory audit for commands, EF Core interceptors | ✅ Planned |
| **T3.3**: Clock skew manipulation | Low | Low | UTC timestamps, NTP synchronization guidance | ✅ Planned |

### Information Disclosure (Confidentiality)

| Threat | Likelihood | Impact | Mitigation | Status |
|--------|-----------|--------|------------|--------|
| **T4.1**: Cross-tenant data leakage | High | Critical | Automatic tenant filters, RLS, repository-level enforcement | ✅ Planned |
| **T4.2**: PII exposure in logs | High | High | PII redaction, structured logging, log scrubbing | ✅ Planned |
| **T4.3**: Error messages revealing system details | Medium | Low | Generic error messages to clients, detailed logs server-side only | ✅ Planned |
| **T4.4**: Secrets in configuration files | Medium | Critical | Secret managers (Azure Key Vault, AWS Secrets Manager), never commit secrets | ✅ Planned |
| **T4.5**: Sensitive data in cache | Medium | Medium | Encrypt cache entries, short TTL, tenant-aware keys | ⏳ Planned |
| **T4.6**: Database backup exposure | Low | Critical | Encrypted backups, access controls, regular rotation | 📋 Guidance |

### Denial of Service (Availability)

| Threat | Likelihood | Impact | Mitigation | Status |
|--------|-----------|--------|------------|--------|
| **T5.1**: API flooding | High | High | Rate limiting per tenant/IP, throttling, CAPTCHA (optional) | ✅ Planned |
| **T5.2**: Expensive queries | Medium | Medium | Query timeouts, pagination enforcement, query complexity analysis | ✅ Planned |
| **T5.3**: Resource exhaustion (memory/CPU) | Medium | High | Bulkhead pattern, circuit breakers, resource limits per tenant | ✅ Planned |
| **T5.4**: Cache flooding | Low | Medium | Cache size limits, LRU eviction, tenant quotas | ✅ Planned |
| **T5.5**: Slowloris/slow read attacks | Medium | Medium | Connection timeouts, reverse proxy protection (nginx/CloudFlare) | 📋 Guidance |

### Elevation of Privilege (Authorization)

| Threat | Likelihood | Impact | Mitigation | Status |
|--------|-----------|--------|------------|--------|
| **T6.1**: Unauthorized access to other tenant's data | High | Critical | Tenant context enforcement, automatic filtering, integration tests | ✅ Planned |
| **T6.2**: Privilege escalation via API | Medium | High | Policy-based authorization, least privilege, role validation | ✅ Planned |
| **T6.3**: Insecure direct object reference (IDOR) | High | High | Authorization checks on every request, tenant ownership verification | ✅ Planned |
| **T6.4**: Bypass validation via raw SQL | Low | High | Avoid raw SQL, use EF Core, code review for SQL queries | ✅ Planned |

## Mitigation Strategies

### Multi-Tenant Data Isolation

**Primary Defense**: Automatic tenant filtering at multiple layers

```csharp
// Layer 1: Middleware - Resolve tenant from request
app.UseMiddleware<TenantResolutionMiddleware>();

// Layer 2: DbContext - Global query filter
modelBuilder.Entity<Order>().HasQueryFilter(e => e.TenantId == _currentTenant.Id);

// Layer 3: Repository - Explicit filtering
public async Task<Order> GetByIdAsync(int id)
{
    return await _context.Orders
        .Where(o => o.TenantId == _currentTenant.Id)
        .FirstOrDefaultAsync(o => o.Id == id);
}

// Layer 4: PostgreSQL RLS (Optional)
CREATE POLICY tenant_isolation ON orders
    USING (tenant_id = current_setting('app.current_tenant')::uuid);
```

**Validation**: Integration tests attempting cross-tenant access must fail.

### Authentication & Authorization

**Strategy**: JWT bearer tokens with short lifetime

```csharp
// JWT Claims
{
    "sub": "user-id",
    "tenant_id": "tenant-uuid",
    "role": "admin",
    "exp": 1234567890,  // 15 minutes
    "iat": 1234567000
}

// Authorization Policies
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTenant", policy => 
        policy.RequireClaim("tenant_id"));
    
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("admin"));
});
```

**Token Refresh**: Short-lived access tokens (15 min) + long-lived refresh tokens (7 days) with rotation.

### Input Validation & Sanitization

**Strategy**: Validate at multiple layers

```csharp
// Layer 1: FluentValidation
public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().MaxLength(100);
        RuleFor(x => x.Amount).GreaterThan(0).LessThan(1000000);
    }
}

// Layer 2: Domain Invariants
public class Order
{
    public void AddItem(OrderItem item)
    {
        if (item.Quantity <= 0)
            throw new DomainException("Quantity must be positive");
    }
}

// Layer 3: Database Constraints
modelBuilder.Entity<Order>()
    .Property(o => o.Amount)
    .HasPrecision(18, 2)
    .IsRequired();
```

### Secrets Management

**Strategy**: Never commit secrets; use external secret managers

```csharp
// ❌ NEVER DO THIS
var connectionString = "Server=prod;Password=secret123;";

// ✅ DO THIS
var connectionString = configuration["ConnectionStrings:Default"]; // From Key Vault

// Development: User Secrets
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;..."

// Production: Azure Key Vault / AWS Secrets Manager
builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultUrl),
    new DefaultAzureCredential()
);
```

### Audit Logging

**Strategy**: Immutable, comprehensive audit trail

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var auditEntries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => new AuditLog
            {
                TenantId = _currentTenant.Id,
                UserId = _currentUser.Id,
                CorrelationId = _correlationId.Value,
                Action = e.State.ToString(),
                EntityType = e.Entity.GetType().Name,
                EntityId = e.Property("Id").CurrentValue,
                Timestamp = DateTime.UtcNow,
                Changes = JsonSerializer.Serialize(GetChanges(e))
            });
        
        // Store in append-only audit table
        eventData.Context.Set<AuditLog>().AddRange(auditEntries);
        
        return base.SavedChanges(eventData, result);
    }
}
```

### Logging Privacy (PII Redaction)

**Strategy**: Automatic PII scrubbing

```csharp
public class PiiRedactionEnricher : ILogEventEnricher
{
    private static readonly Regex EmailRegex = new(@"\b[\w\.-]+@[\w\.-]+\.\w+\b");
    private static readonly Regex CreditCardRegex = new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b");
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.MessageTemplate.Text.Contains("@") || 
            logEvent.MessageTemplate.Text.Any(char.IsDigit))
        {
            var message = logEvent.RenderMessage();
            message = EmailRegex.Replace(message, "[EMAIL_REDACTED]");
            message = CreditCardRegex.Replace(message, "[CC_REDACTED]");
            
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty("RedactedMessage", message));
        }
    }
}
```

### Supply Chain Security

**Strategy**: Dependency scanning and vetting

```bash
# Check for known vulnerabilities
dotnet list package --vulnerable --include-transitive

# Audit dependencies
dotnet-outdated

# Policy: Only MIT/BSD/Apache licenses
# Reject packages with unclear licensing
```

**Process**:
1. Vet all new dependencies before adding to `Directory.Packages.props`
2. Automated vulnerability scanning in CI
3. Monthly dependency update review
4. Document any exceptions with justification

## ASVS Level 2 Checklist

### V1: Architecture, Design and Threat Modeling

- [ ] **V1.1.1**: Document security architecture with trust boundaries (this document)
- [ ] **V1.1.2**: Establish secure development lifecycle (SDLC) with security gates in CI
- [ ] **V1.4.1**: Document tenant isolation strategy and data segregation (ADR-0001)
- [ ] **V1.4.2**: Test cross-tenant access controls (integration tests)
- [ ] **V1.11.1**: Define and enforce business logic security requirements (domain invariants)

### V2: Authentication

- [ ] **V2.1.1**: Use cryptographically strong, industry-standard authentication (JWT with RS256)
- [ ] **V2.1.2**: Allow users to change authentication credentials securely
- [ ] **V2.2.1**: Implement anti-automation controls for authentication (rate limiting)
- [ ] **V2.2.3**: Implement exponential backoff after repeated authentication failures
- [ ] **V2.3.1**: Use strong, random session identifiers (JWT with secure random)
- [ ] **V2.7.1**: Verify passwords against known weak passwords (integration with consumer apps)

### V3: Session Management

- [ ] **V3.2.1**: Use framework-managed session tokens (JWT)
- [ ] **V3.2.2**: Session tokens use secure, cryptographic random generation
- [ ] **V3.3.1**: Logout invalidates session token server-side (token revocation list)
- [ ] **V3.3.2**: Set absolute session timeout (JWT exp claim, max 24 hours)
- [ ] **V3.3.3**: Set idle timeout for sensitive operations (refresh token expiry)

### V4: Access Control

- [ ] **V4.1.1**: Enforce least privilege access control decisions
- [ ] **V4.1.2**: Use centralized authorization mechanism (policy-based authorization)
- [ ] **V4.1.3**: Deny all access by default (fail-closed)
- [ ] **V4.1.5**: Verify authorization on every request (authorization decorators/middleware)
- [ ] **V4.2.1**: Verify tenant context on every data access (automatic filtering)
- [ ] **V4.2.2**: Test that users cannot access data outside their authorization

### V5: Validation, Sanitization and Encoding

- [ ] **V5.1.1**: Validate all input against positive whitelist (FluentValidation)
- [ ] **V5.1.2**: Validate and sanitize all data from external systems
- [ ] **V5.1.3**: Enforce type safety for all input validation
- [ ] **V5.2.1**: Sanitize all output to prevent injection attacks
- [ ] **V5.3.1**: Use parameterized queries for all database access (EF Core)
- [ ] **V5.3.4**: Prevent SQL injection via ORMs (EF Core with no raw SQL)

### V7: Error Handling and Logging

- [ ] **V7.1.1**: Do not log credentials or sensitive data
- [ ] **V7.1.2**: Do not log session tokens
- [ ] **V7.2.1**: Log all authentication, access control, and validation failures
- [ ] **V7.2.2**: Log all administrative actions with context (user, tenant, time)
- [ ] **V7.3.1**: Ensure logs are protected from injection attacks (structured logging)
- [ ] **V7.3.2**: Ensure log entries include correlation IDs for tracing

### V9: Data Protection

- [ ] **V9.1.1**: Use TLS for all client connectivity (HTTPS only)
- [ ] **V9.1.2**: Use TLS 1.2+ with strong cipher suites
- [ ] **V9.1.3**: Enforce HSTS header (max-age=31536000)
- [ ] **V9.2.1**: Encrypt data at rest containing sensitive information (database encryption)
- [ ] **V9.2.2**: Use authenticated encryption where applicable
- [ ] **V9.4.1**: Protect audit logs from unauthorized access and modification

### V12: Files and Resources

- [ ] **V12.1.1**: Validate file upload types against whitelist
- [ ] **V12.1.2**: Enforce maximum file upload size
- [ ] **V12.5.1**: Validate file downloads are authorized for tenant
- [ ] **V12.6.1**: Do not expose directory listings

### V13: API and Web Service

- [ ] **V13.1.1**: Use identical authentication for all API paths
- [ ] **V13.1.3**: Require API keys/tokens for API access
- [ ] **V13.1.4**: Validate authorization on every API request
- [ ] **V13.2.1**: Use RESTful or GraphQL with proper authorization
- [ ] **V13.2.3**: Implement rate limiting to prevent abuse
- [ ] **V13.4.1**: Ensure GraphQL query depth limiting (prevent DoS)
- [ ] **V13.4.2**: Disable GraphQL introspection in production (optional)

## Operational Security Controls

### Security Headers

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    context.Response.Headers.Add("Strict-Transport-Security", 
        "max-age=31536000; includeSubDomains");
    
    await next();
});
```

### CSRF Protection

- Use anti-forgery tokens for state-changing operations
- Validate `Origin` and `Referer` headers
- Use `SameSite=Strict` for cookies

### TLS Configuration

- TLS 1.2+ only (disable TLS 1.0, 1.1)
- Strong cipher suites only (forward secrecy)
- Valid, trusted certificate from CA
- HSTS enabled with long max-age

### Key Rotation

- Database credentials: Rotate quarterly
- JWT signing keys: Rotate annually or on compromise
- API keys: Rotate on personnel changes
- Encryption keys: Follow org key management policy

## Security Testing Strategy

### Automated Testing

```csharp
// Integration test: Cross-tenant access
[Fact]
public async Task GetOrder_WithDifferentTenantId_ReturnsNotFound()
{
    // Arrange
    var tenant1Order = await CreateOrderForTenant("tenant-1");
    SetCurrentTenant("tenant-2");
    
    // Act
    var result = await _handler.HandleAsync(new GetOrderQuery { Id = tenant1Order.Id });
    
    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Error.ShouldBe("Order not found");
}
```

### Manual Security Testing

- [ ] Penetration testing by external firm (annually)
- [ ] OWASP Top 10 verification (each release)
- [ ] Code review for security issues (every PR)
- [ ] Dependency vulnerability scan (weekly in CI)

## Incident Response

### Detection

- Monitor for:
  - Repeated authentication failures
  - Cross-tenant access attempts
  - Unusual API usage patterns
  - High-value data access spikes
  - Audit log anomalies

### Response Plan

1. **Identify**: Detect and verify security incident
2. **Contain**: Isolate affected systems/tenants
3. **Eradicate**: Remove threat, patch vulnerabilities
4. **Recover**: Restore services, verify integrity
5. **Learn**: Post-mortem, update defenses

### Notification

- **Dedicated Tenants**: Notify within 4 hours
- **Regulated Tenants**: Follow contractual SLAs (e.g., 1 hour)
- **All Tenants**: Notify within 24 hours if data breach

## Review & Maintenance

- **Frequency**: Quarterly threat model review
- **Triggers**: New features, architecture changes, security incidents
- **Owner**: Security & Compliance Team
- **Approvers**: Platform Lead, Security Lead, Compliance Lead

## References

- [OWASP ASVS 4.0](https://owasp.org/www-project-application-security-verification-standard/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [Discovery Summary](discovery-summary.md)
- [CQRS Framework Plan](cqrs-framework-plan.md)
- [ADR-0001: Tenancy Strategy](adrs/ADR-0001-tenancy-strategy.md)
- [ADR-0002: Audit Logging](adrs/ADR-0002-audit-logging.md)

---

**Next Review Date**: 2026-01-04  
**Sign-Off Required**: Security Lead, Platform Lead, Compliance Lead
