# Discovery Summary

**Document Owner**: Product & Platform Teams  
**Last Updated**: 2025-10-04  
**Status**: Active Development

## Purpose

This document captures the results of the discovery phase for the **Idevs** framework (repo: `idevs-core`), a .NET building-block framework for modern SaaS and ERP applications. It establishes stakeholder alignment, defines tenant personas, outlines primary scenarios, and documents business drivers and technical constraints.

## Stakeholders

### Product Owner
- **Responsibilities**: 
  - Shapes multi-solution roadmap (retail, billing, accounting)
  - Defines tenant onboarding priorities and offline-first requirements
  - Aligns feature roadmap with business objectives
  - Prioritizes scenarios based on first-party solution needs

### Platform Engineering
- **Responsibilities**:
  - Owns infrastructure automation and GitHub workflows
  - Manages GitVersion governance and semantic versioning
  - Provides observability tooling and monitoring infrastructure
  - Ensures build and deployment pipeline reliability

### Security & Compliance
- **Responsibilities**:
  - Ensures ASVS Level 2 adherence across all components
  - Defines audit logging coverage and retention policies
  - Validates incident response readiness and threat mitigation
  - Reviews tenant isolation strategies and data protection controls

### Developer Experience
- **Responsibilities**:
  - Represents consumer teams integrating WebAPI and GraphQL endpoints
  - Advocates for clean APIs and intuitive abstractions
  - Provides feedback on DX friction points and integration patterns
  - Documents consumption patterns and usage examples

### Operations
- **Responsibilities**:
  - Manages deployment, tenant configuration, and monitoring
  - Handles support rotations and incident escalation
  - Maintains SLOs and monitors service health
  - Coordinates tenant-specific communications during incidents

## First-Party Solution Focus

The **Idevs** framework powers these first-party SaaS and ERP solutions:

### Multi-Store Retail Management
- Inventory tracking across multiple locations
- Point-of-sale (POS) integration with offline support
- Multi-currency pricing and promotion management
- Real-time stock synchronization between stores and warehouse
- Customer loyalty programs and gift card management

### Subscription Billing Platforms
- Recurring billing with flexible pricing models (flat, tiered, usage-based)
- Subscription lifecycle management (trials, upgrades, downgrades, cancellations)
- Invoice generation and payment processing
- Revenue recognition and deferred revenue tracking
- Dunning management and payment retry logic

### Accounting & Finance Suites
- General ledger and chart of accounts management
- Accounts payable and receivable automation
- Multi-currency support with real-time exchange rates
- Financial reporting and compliance (GAAP, IFRS)
- Bank reconciliation and cash flow management
- Audit trail for all financial transactions

## Tenant Personas

### 1. Dedicated Tenant

**Profile**: Enterprise customers with strict data isolation and compliance requirements.

**Characteristics**:
- Isolated database or dedicated schema
- Custom SLA guarantees (99.9%+ uptime)
- Fine-grained audit trails with extended retention
- Custom compliance controls (HIPAA, SOC 2, GDPR)
- Dedicated support channels

**Needs**:
- Guaranteed performance with no noisy neighbor issues
- Data residency controls (specific geographic regions)
- Custom backup and disaster recovery policies
- Ability to schedule maintenance windows

**Risks**:
- Higher infrastructure costs
- Complexity in multi-tenant deployment automation
- Schema migration coordination challenges

**Priority**: High (Revenue impact, contract obligations)

### 2. Tiered Multi-Tenant

**Profile**: Small to medium businesses sharing infrastructure with logical isolation.

**Characteristics**:
- Shared database with row-level security
- Standard SLA (99.5% uptime)
- Cost-optimized infrastructure
- Configurable soft-delete retention policies
- Throttling and rate limiting per tenant tier

**Needs**:
- Predictable pricing and transparent resource allocation
- Self-service tenant configuration
- Ability to upgrade tiers seamlessly
- Fair resource allocation to prevent tenant starvation

**Risks**:
- Tenant data leakage through misconfiguration
- Performance degradation from resource contention
- Complexity in tenant-level billing and metering

**Priority**: High (Volume, revenue scale)

### 3. Offline-First Client

**Profile**: Mobile or field devices requiring resilient operation without connectivity.

**Characteristics**:
- Retail associates with mobile POS devices
- Field technicians with tablets
- Warehouse staff with handheld scanners
- Remote kiosks with intermittent connectivity

**Needs**:
- Delta sync endpoints with conflict detection
- Retry-aware throttling policies
- Offline data caching with automatic sync on reconnect
- Optimistic concurrency with conflict resolution strategies
- Background sync with progress indication

**Risks**:
- Sync conflicts with concurrent modifications
- Data loss during network failures
- Increased complexity in state management
- Version compatibility between client and server

**Priority**: Critical (Core use case for retail and field solutions)

### 4. Regulated Tenant

**Profile**: Organizations subject to strict regulatory compliance (healthcare, finance, government).

**Characteristics**:
- Extended audit retention (7+ years)
- Immutable audit logs with cryptographic verification
- Data residency enforcement
- Mandatory encryption at rest and in transit
- Regular compliance reporting and attestation

**Needs**:
- Automated compliance reporting (SOC 2, HIPAA, GDPR)
- Rapid incident notifications with detailed forensics
- Role-based access control (RBAC) with least privilege
- Audit log export for regulatory review
- Data anonymization and pseudonymization capabilities

**Risks**:
- High operational overhead for compliance maintenance
- Audit log storage costs
- Complexity in right-to-erasure implementation (GDPR)

**Priority**: High (Regulatory requirements, market access)

## Primary Scenarios

### Scenario 1: Data Isolation Guarantees

**Description**: Ensure complete data isolation between tenants across all layers (database, cache, logs, backups).

**Success Criteria**:
- [ ] Zero cross-tenant data leakage incidents
- [ ] Tenant filters applied automatically at repository level
- [ ] Query plan analysis confirms tenant predicate pushdown
- [ ] Audit logs include tenant context in all entries
- [ ] Cache keys include tenant identifiers

**Acceptance Tests**:
- Attempt to query another tenant's data (should return empty results)
- Verify tenant_id filters in all generated SQL queries
- Test soft-delete filters with multi-tenant data
- Validate cache isolation under concurrent tenant load

### Scenario 2: Resilient Command Processing

**Description**: Commands execute reliably with idempotency, retries, and graceful degradation.

**Success Criteria**:
- [ ] Commands complete within P99 latency targets (<150ms nominal load)
- [ ] Duplicate command submissions are idempotent
- [ ] Transient failures trigger automatic retries with exponential backoff
- [ ] Circuit breaker prevents cascade failures
- [ ] Dead letter queue captures failed commands for manual review

**Acceptance Tests**:
- Submit duplicate commands with same idempotency key (verify single execution)
- Introduce database transient errors (verify automatic retry)
- Simulate downstream service failure (verify circuit breaker activation)
- Measure command processing latency under load

### Scenario 3: Offline Sync and Conflict Resolution

**Description**: Offline clients synchronize changes with conflict detection and resolution.

**Success Criteria**:
- [ ] Delta feeds include only changed entities since last sync token
- [ ] Concurrent modifications detected via optimistic concurrency (ETags, version numbers)
- [ ] Conflict resolution strategies configurable (last-write-wins, manual, custom)
- [ ] Sync resumable after interruption (sync tokens preserved)
- [ ] Background sync with progress tracking and error reporting

**Acceptance Tests**:
- Modify same entity offline on two clients, sync both (verify conflict detection)
- Interrupt sync midway, resume (verify resumption from last checkpoint)
- Sync large dataset, measure throughput and resource usage
- Test sync with stale sync token (verify full resync or delta from oldest available)

### Scenario 4: Audit Trails and Compliance Reporting

**Description**: Comprehensive audit logging with tenant awareness and compliance export.

**Success Criteria**:
- [ ] All commands and queries logged with tenant, user, correlation IDs
- [ ] Audit events include before/after snapshots for data changes
- [ ] PII redacted automatically in logs per data classification policy
- [ ] Audit logs immutable and cryptographically signed
- [ ] Export capability for compliance audits (CSV, JSON, PDF)
- [ ] Retention policy enforced with automated purging after defined period

**Acceptance Tests**:
- Execute commands, verify audit entries with all required metadata
- Attempt to modify audit log (verify immutability)
- Export audit trail for specific tenant and date range
- Verify PII redaction in exported logs

## Business Drivers

### Velocity
- **Goal**: Reduce time-to-market for new SaaS/ERP features by 40%
- **Measure**: Feature delivery cycle time from concept to production
- **Enablers**: Pre-built CQRS handlers, repository patterns, multi-tenancy abstractions, decorator patterns

### Reliability
- **Goal**: Achieve 99.9% uptime for dedicated tenants, 99.5% for tiered multi-tenant
- **Measure**: Service availability, mean time to recovery (MTTR)
- **Enablers**: Circuit breakers, retries, idempotency, health checks, chaos testing

### Observability
- **Goal**: Mean time to detection (MTTD) < 5 minutes for critical incidents
- **Measure**: Alert latency, dashboard coverage, trace completeness
- **Enablers**: Structured logging (Serilog), distributed tracing (OpenTelemetry), RED metrics

### Performance
- **Goal**: P99 command latency < 150ms under nominal load
- **Measure**: Command processing time, database query duration, cache hit ratio
- **Enablers**: Repository-level caching, optimized EF Core queries, database indexing

### Cost Efficiency
- **Goal**: Reduce infrastructure costs by 30% through resource optimization
- **Measure**: Cost per tenant, database storage growth rate, compute utilization
- **Enablers**: Multi-tenant shared infrastructure, tiered resource allocation, soft-delete purging

### Developer Experience
- **Goal**: New developers productive within 2 days
- **Measure**: Time to first contribution, API satisfaction score
- **Enablers**: Clear abstractions, comprehensive documentation, usage examples, strong typing

## Non-Functional Goals

### Security
- ASVS Level 2 compliance across all components
- Secrets never committed to repository (use managed secret stores)
- Input validation at application layer with FluentValidation
- Encryption in transit (TLS 1.2+) and at rest (database encryption)
- Supply chain security (dependency scanning, SCA tools)

### Scalability
- Horizontal scaling with stateless components
- Per-tenant configuration overrides (rate limits, feature flags)
- Database read replicas for query load distribution
- Caching strategy to reduce database load

### Maintainability
- ≥80% branch coverage with xUnit, Shouldly, NSubstitute
- Modular pipeline components with clear extension points
- Comprehensive XML documentation for public APIs
- Automated dependency updates with security advisory monitoring

## Constraints and Rules

### Technical Constraints

#### No System.Reflection (Where Possible)
- **Rationale**: Avoid runtime reflection for performance and ahead-of-time (AOT) compilation compatibility
- **Alternatives**: 
  - Incremental source generators for service registration
  - Explicit registration with open generics
  - Compile-time code generation (e.g., Mapperly for object mapping)
- **Exceptions**: Limited reflection allowed in test fixtures and developer tools

#### PostgreSQL-First EF Core Approach
- **Rationale**: PostgreSQL as primary database; SQL Server/MySQL support as optional extensions
- **Implications**:
  - Leverage PostgreSQL-specific features (JSONB, row-level security, generated columns)
  - Code-first migrations targeting PostgreSQL
  - Provider-specific logic abstracted behind interfaces for alternate databases
- **Testing**: Integration tests use PostgreSQL (not in-memory provider)

### Process Constraints

#### Git Flow Branching Model
- `main` branch: production-ready releases only
- `develop` branch: integration branch for features
- `feature/*`, `hotfix/*`, `release/*` branches per Git Flow conventions
- Pull requests require approval and passing CI before merge

#### Conventional Commits
- Commit messages follow Conventional Commits specification
- Format: `<type>(<scope>): <subject>`
- Types: `feat`, `fix`, `docs`, `chore`, `test`, `refactor`, `perf`, `ci`
- Enables GitVersion to calculate semantic versions automatically

#### Coverage Target
- ≥80% branch coverage required for all new code
- Coverage reports published in CI artifacts
- Coverage gates enforced in pull request checks

## Open Questions

### Hosting Strategy
- [ ] Finalize guidance for single-tenant vs shared deployments
- [ ] Determine Kubernetes vs serverless deployment model
- [ ] Define infrastructure-as-code (IaC) tooling (Terraform, Pulumi, Bicep)

### Legal & Compliance
- [ ] Confirm audit log retention periods per jurisdiction
- [ ] Validate GDPR right-to-erasure with soft-delete strategy
- [ ] Determine data residency requirements by region

### Offline Sync Format
- [ ] Finalize delta sync payload format (custom, JSON Patch, OData)
- [ ] Validate offline sync format with pilot consumer applications
- [ ] Define conflict resolution UI patterns for end users

### Incident Management
- [ ] Establish incident notification SLAs with operations and compliance
- [ ] Define tenant-specific communication protocols during outages
- [ ] Create runbooks for common incident scenarios

## Acceptance Checklist

- [ ] **Personas Finalized**: All four tenant personas documented with needs, risks, priorities
- [ ] **Scenarios Prioritized**: Primary scenarios defined with success criteria and acceptance tests
- [ ] **Non-Functional Goals Documented**: Business drivers and non-functional goals captured with measurable outcomes
- [ ] **Constraints Validated**: Technical and process constraints reviewed and approved by stakeholders
- [ ] **Open Questions Tracked**: Open questions logged with owners and target resolution dates
- [ ] **Stakeholder Sign-Off**: Product Owner, Platform Engineering, Security, and Operations approve discovery findings

## References

- [CQRS Framework Plan](cqrs-framework-plan.md)
- [Threat Model](threat-model.md)
- [Glossary](glossary.md)
- [ADR-0001: Tenancy Strategy](adrs/ADR-0001-tenancy-strategy.md)
- [ADR-0002: Audit Logging](adrs/ADR-0002-audit-logging.md)
- [ADR-0003: Soft Delete](adrs/ADR-0003-soft-delete.md)

---

**Next Steps**: Proceed to Phase 0 to establish guardrails, baseline threat model, and initialize ADRs.
