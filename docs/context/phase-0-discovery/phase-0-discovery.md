# Phase 0: Discovery & Guardrails

**Phase Owner**: Architecture Team  
**Last Updated**: 2025-10-05  
**Status**: ✅ Complete  
**Completion Date**: 2025-10-05  
**Dependencies**: None (foundational phase)

---

## Purpose

Phase 0 establishes the **foundation** for the **Idevs** framework (repo: `idevs-core`) by defining scope, identifying stakeholders, documenting design principles, and creating initial governance structures. This phase ensures alignment before any implementation begins.

**Key Principle**: *"Measure twice, cut once"* — invest in discovery to avoid costly rework later.

---

## Objectives

### Primary Objectives

1. **Establish Scope and Guardrails**
   - Define what the framework *is* and *is not*
   - Document non-negotiable constraints (PostgreSQL-first, no System.Reflection)
   - Identify boundaries with related projects (idevs-foundation, codex-idevs-core)

2. **Stakeholder Alignment**
   - Identify all stakeholders (technical and business)
   - Define tenant personas with specific needs
   - Document primary scenarios and success criteria

3. **Initialize Governance**
   - Set up ADR (Architecture Decision Record) process
   - Define decision-making authority and review cadence
   - Establish documentation standards

4. **Baseline Security and Observability**
   - Create initial threat model with STRIDE analysis
   - Define observability requirements (metrics, logs, traces)
   - Document compliance requirements (GDPR, SOC2, HIPAA considerations)

---

## Key Activities

### 1. Scope Definition

**Activity**: Document framework boundaries and non-goals

**Deliverables**:

- **discovery-summary.md** ✅ (already completed)
  - Stakeholders and first-party solutions (retail, billing, finance)
  - Tenant personas: Dedicated, Tiered Multi-Tenant, Offline-First, Regulated
  - Primary scenarios with acceptance criteria
  - Business drivers and non-functional requirements

**Key Decisions**:

- ✅ Target .NET 8.0 LTS (with .NET 10.0 forward compatibility documented)
- ✅ CQRS-first architecture without MediatR (direct handler invocation with decorators)
- ✅ PostgreSQL-first persistence strategy
- ✅ Avoid System.Reflection where possible (explicit registration or source generators)

**Non-Goals** (explicitly out of scope):

- ❌ Supporting non-relational primary databases (Postgres only; others via adapters)
- ❌ Built-in UI components (framework is backend-focused)
- ❌ Runtime plugin/module discovery (security and performance risk)
- ❌ Supporting legacy .NET Framework (modern .NET only)

---

### 2. Design Principles & Patterns

**Activity**: Document architectural tenets that guide all design decisions

**Core Principles**:

#### A. CQRS Without MediatR

- **Principle**: Direct handler invocation with decorator pattern for cross-cutting concerns
- **Rationale**:
  - Simpler mental model (no pipeline abstraction)
  - Easier debugging (explicit call chains)
  - Avoid over-engineering for small-to-medium complexity
- **Implementation**: Use standard .NET patterns:
  - Decorators for validation, logging, caching
  - Middleware for ASP.NET Core concerns
  - EF Core interceptors for persistence concerns
  - Action filters for controller-level logic

#### B. Domain-Driven Design (DDD)

- **Principle**: Bounded contexts, aggregates, entities, value objects
- **Enforcement**:
  - Clear aggregate boundaries (documented in phase-2-domain)
  - Repository pattern for aggregate roots only
  - No cross-aggregate transactions (use eventual consistency via outbox)
  - Domain events for inter-aggregate communication

#### C. Multi-Tenancy First

- **Principle**: Every design must consider multi-tenant implications
- **Default Strategy**: Row-level tenancy with `tenant_id` column
- **Isolation Levels**:
  - **Shared DB, Shared Schema**: Default (cost-efficient)
  - **Shared DB, Schema per Tenant**: For regulated industries
  - **Database per Tenant**: For dedicated deployments
  - **PostgreSQL RLS**: Optional additional layer

#### D. Test-Driven Development (TDD)

- **Principle**: Tests are first-class citizens, not an afterthought
- **Coverage Target**: ≥80% line coverage (enforced in CI)
- **Testing Stack**:
  - xUnit for test framework
  - Shouldly for fluent assertions
  - NSubstitute for mocking
  - Testcontainers for integration tests (Postgres)

#### E. Explicit Over Magic

- **Principle**: Prefer explicit code over convention-based discovery
- **Rationale**: Avoids System.Reflection, improves startup performance, better debugging
- **Examples**:
  - ✅ Explicit service registration: `services.AddScoped<IFooHandler, FooHandler>()`
  - ✅ Source generators for boilerplate (considered acceptable)
  - ❌ Assembly scanning for handlers
  - ❌ Attribute-based magic registration

#### F. Observability Built-In

- **Principle**: Telemetry is not optional
- **Requirements**:
  - Structured logging (Serilog) with correlation IDs
  - Distributed tracing (OpenTelemetry) with W3C Trace Context
  - RED metrics (Request rate, Error rate, Duration) for all operations
  - Tenant-aware metrics and logs

---

### 3. Glossary & Terminology

**Activity**: Establish shared vocabulary

**Deliverable**: **glossary.md** ✅ (already completed)

**Key Terms**:

- CQRS: Command Query Responsibility Segregation
- Handler: Class responsible for processing a command or query
- Decorator: Wrapper that adds behavior to a handler (validation, logging, etc.)
- Aggregate: Cluster of domain objects treated as a unit
- Tenant Context: Request-scoped object holding tenant identification
- Soft Delete: Mark record as deleted without physical removal
- Audit Trail: Immutable log of data changes

**Style Guide**:

- Use consistent capitalization (e.g., "Command Handler" not "command handler")
- Define acronyms on first use
- Link to glossary from all technical documents

---

### 4. Architecture Decision Records (ADRs)

**Activity**: Initialize ADR process and create foundational decisions

**Deliverables**: Create 4 initial ADRs in `docs/context/adrs/`

#### ADR-0001: Tenancy Strategy

**Decision**: Row-level multi-tenancy via `tenant_id` column as default, with optional PostgreSQL RLS

**Context**:

- Need to support multiple tenants efficiently
- Balance between cost (shared resources) and isolation (security)
- Must support dedicated deployments for enterprise customers

**Consequences**:

- ✅ Cost-efficient (shared infrastructure)
- ✅ Flexible (can migrate tenant to dedicated later)
- ✅ Performance (single query can serve tenant)
- ⚠️ Requires careful query filtering (global query filters)
- ⚠️ Risk of data leakage if filters missed (mitigated by RLS)

**Alternatives Considered**:

- Schema-per-tenant: More isolation but migration complexity
- Database-per-tenant: Maximum isolation but operational overhead

**Related Documents**:

- [Discovery Summary](../discovery-summary.md#tenant-personas)
- [Threat Model](../threat-model.md#multi-tenant-isolation)

---

#### ADR-0002: Audit Logging Strategy

**Decision**: Structured audit trails via EF Core SaveChanges interceptors + Serilog

**Context**:

- Compliance requirements (SOC2, GDPR, HIPAA)
- Need to track who changed what, when
- Support both real-time monitoring and forensic analysis

**Consequences**:

- ✅ Automatic capture (developers don't need to remember)
- ✅ Immutable append-only log
- ✅ Tenant-aware (every change tagged with tenant_id)
- ⚠️ Storage cost (audit logs grow indefinitely)
- ⚠️ PII considerations (must redact sensitive fields)

**Implementation Pattern**:

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        var changes = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Unchanged);
        
        foreach (var entry in changes)
        {
            var auditEntry = new AuditLog
            {
                TenantId = GetTenantId(),
                UserId = GetUserId(),
                EntityType = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                Timestamp = DateTime.UtcNow,
                Changes = GetChanges(entry)
            };
            
            // Write to audit store
        }
        
        return result;
    }
}
```text

**Related Documents**:

- [Threat Model](../threat-model.md#audit-requirements)
- [Observability Blueprint](../observability-blueprint.md#audit-trail)

---

#### ADR-0003: Soft Delete Strategy

**Decision**: Soft delete with `IsDeleted` flag + global query filters

**Context**:

- Users often "delete" data accidentally
- Regulatory requirements may prohibit permanent deletion
- Need to support undelete functionality
- Must maintain referential integrity

**Consequences**:

- ✅ Data recovery possible
- ✅ Audit trail of deletions
- ✅ Supports compliance (data retention policies)
- ⚠️ Complicates unique constraints (deleted rows still in index)
- ⚠️ Query performance (filter on every query)
- ⚠️ Storage cost (deleted data retained)

**Implementation Pattern**:

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}

// Global query filter
modelBuilder.Entity<MyEntity>()
    .HasQueryFilter(e => !e.IsDeleted);

// Unique constraint handling
modelBuilder.Entity<MyEntity>()
    .HasIndex(e => new { e.TenantId, e.Email })
    .HasFilter("[IsDeleted] = 0");  // SQL Server
    // For Postgres: .HasFilter("\"IsDeleted\" = false")
```text

**Purge Policy**:

- Soft-deleted records retained for 90 days (configurable)
- After retention period, eligible for hard delete (purge job)
- Purge requires explicit approval for production tenants
- Purge is audited with justification

**Related Documents**:

- [Discovery Summary](../discovery-summary.md#compliance-requirements)
- [Phase 5: Infrastructure Plan](../phase-5-infrastructure/phase-5-infrastructure.md)

---

#### ADR-0004: Release Governance & Versioning

**Decision**: Git Flow + GitVersion + Conventional Commits

**Context**:

- Need predictable, automated versioning
- Support multiple release streams (LTS, current)
- Enable hotfixes without disrupting feature development
- Generate release notes automatically

**Branching Model**:

```text
main (production)
  ↑ merge (tagged releases only)
develop (integration)
  ↑ merge
feature/my-feature (topic branch)

hotfix/security-patch
  ↑ merge to develop first, then to main
```text

**Version Bump Rules**:

- **Major (x.0.0)**: Breaking changes to public API
- **Minor (1.x.0)**: New features, backward compatible
- **Patch (1.0.x)**: Bug fixes, no API changes

**Conventional Commits**:

```text
feat: add tenant isolation middleware
fix: resolve soft-delete query filter bug
docs: update ADR-0003 with purge policy
chore: bump dependencies

BREAKING CHANGE: ICommandHandler signature changed
```text

**GitVersion Configuration** (documented, not implemented):

```yaml
# GitVersion.yml (future implementation)
mode: ContinuousDeployment
branches:
  main:
    tag: ''
    increment: Patch
  develop:
    tag: alpha
    increment: Minor
  feature:
    tag: useBranchName
    increment: Inherit
```text

**Hotfix Protocol**:

1. Branch from `develop` (not `main`)
2. Implement fix with tests
3. Merge to `develop` first
4. Cherry-pick or merge to `main` for release
5. Tag with patch version bump
6. **Changed**: Hotfixes follow feature flow (merge to develop, then release branch to main)

**Support Windows**:

- **Current**: Latest major.minor version, full support
- **LTS**: Designated versions, security fixes only (18 months)
- **EOL**: End-of-life versions, no support

**Related Documents**:

- [CQRS Framework Plan](../cqrs-framework-plan.md#versioning-strategy)
- [Phase 1: Platform Plan](../phase-1-platform/phase-1-platform.md)

---

### 5. Threat Modeling

**Activity**: Baseline security analysis using STRIDE

**Deliverable**: **threat-model.md** ✅ (already completed)

**Key Threats**:

1. **Cross-Tenant Data Leakage** (Spoofing, Information Disclosure)
   - Mitigation: Global query filters + optional RLS
   - Validation: Automated tests for tenant isolation

2. **SQL Injection** (Tampering)
   - Mitigation: Parameterized queries only (EF Core default)
   - Validation: Static analysis (Roslyn analyzers)

3. **Insufficient Logging** (Repudiation)
   - Mitigation: Audit interceptors + correlation IDs
   - Validation: Compliance audit

4. **Secrets in Code** (Information Disclosure)
   - Mitigation: No secrets in repo, use Azure Key Vault / AWS Secrets Manager
   - Validation: Pre-commit hooks + secret scanning

**ASVS Checklist** (Level 2 compliance):

- [x] V1: Architecture, Design, and Threat Modeling
- [ ] V2: Authentication (planned in Phase 4)
- [ ] V3: Session Management (planned in Phase 4)
- [ ] V4: Access Control (planned in Phase 3)
- [ ] V5: Validation (planned in Phase 3)
- [ ] V7: Error Handling (planned in Phase 3)
- [ ] V9: Data Protection (planned in Phase 5)

---

### 6. Observability Requirements

**Activity**: Define telemetry strategy

**Deliverable**: **observability-blueprint.md** ✅ (already completed)

**Key Requirements**:

- **Logging**: Structured logs with Serilog, correlation IDs, PII redaction
- **Tracing**: OpenTelemetry with W3C Trace Context, automatic instrumentation
- **Metrics**: RED metrics (Request rate, Error rate, Duration) per command/query
- **Dashboards**: Service health, multi-tenant insights, database performance
- **Alerts**: Critical (page immediately), Warning (investigate), Info (awareness)

**SLO Targets**:

- Availability (Dedicated): 99.9% (43 min downtime/month)
- Availability (Multi-tenant): 99.5% (3.6 hrs downtime/month)
- P99 Latency: <150ms
- Error Rate: <0.1%

---

## Deliverables

| Deliverable | Status | Owner | Notes |
|-------------|--------|-------|-------|
| discovery-summary.md | ✅ Done | Architecture | Stakeholders, personas, scenarios |
| glossary.md | ✅ Done | Architecture | Shared terminology |
| threat-model.md | ✅ Done | Security | STRIDE analysis + ASVS checklist |
| observability-blueprint.md | ✅ Done | Platform | Metrics, logs, traces, dashboards |
| ADR-0001 (Tenancy) | ✅ Done | Architecture | Multi-tenancy strategy (Accepted 2025-10-04) |
| ADR-0002 (Audit) | ✅ Done | Security | Audit logging approach (Accepted 2025-10-05) |
| ADR-0003 (Soft Delete) | ✅ Done | Architecture | Soft delete implementation (Accepted 2025-10-05) |
| ADR-0004 (Versioning) | ✅ Done | Platform | Release governance (Accepted 2025-10-05) |
| Phase 0 Sign-off | ✅ Done | Solo Developer | Self-approved with rationale (2025-10-05) |
| ADR-0005 (DI Strategy) | ✅ Done | Platform | DI container strategy (Accepted 2025-10-05) |

---

## Success Metrics

### Quantitative Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Stakeholder Alignment | 100% sign-off | Survey/meeting approval |
| ADRs Created | 4 baseline ADRs | ADR count in /adrs |
| Documentation Coverage | All sections complete | Checklist validation |
| Threat Categories Addressed | All STRIDE categories | Threat model review |

### Qualitative Metrics

- **Clarity**: Can a new developer understand scope and constraints in <30 minutes?
- **Alignment**: Do stakeholders agree on priorities and non-goals?
- **Traceability**: Can decisions be traced back to drivers and constraints?
- **Actionability**: Are there clear next steps for each subsequent phase?

---

## Risks & Mitigations

### Risk 1: Stakeholder Misalignment

**Impact**: High  
**Probability**: Medium  
**Symptom**: Conflicting priorities, scope creep  
**Mitigation**:

- Hold alignment workshop with all stakeholders
- Document explicit sign-offs
- Create RACI matrix (Responsible, Accountable, Consulted, Informed)
- Schedule monthly steering committee meetings

### Risk 2: Over-Engineering

**Impact**: Medium  
**Probability**: Medium  
**Symptom**: Complex abstractions, slow progress  
**Mitigation**:

- Validate designs against discovery personas and scenarios
- "Two-way door" principle: prefer reversible decisions
- Defer optimization until proven need
- Regular architecture review with "can we simplify?" lens

### Risk 3: Insufficient Security Analysis

**Impact**: High  
**Probability**: Low  
**Symptom**: Vulnerabilities discovered late  
**Mitigation**:

- Threat model review by security team (external if available)
- STRIDE analysis for each trust boundary
- Security champion assigned to each phase
- Penetration testing budget allocated

### Risk 4: Documentation Drift

**Impact**: Medium  
**Probability**: High  
**Symptom**: Docs out of sync with decisions  
**Mitigation**:

- ADR process enforced (no design decision without ADR)
- Documentation updates in PR checklist
- Quarterly documentation review
- Link validation automated in CI

---

## Exit Criteria

Phase 0 is **complete** when:

### Must Have (Blocking)

- [x] **Discovery summary** finalized and approved by stakeholders
- [x] **Glossary** published and cross-referenced in all docs
- [x] **Threat model** baseline approved by security lead
- [x] **Observability requirements** documented with SLOs
- [x] **ADRs 0001-0005** created with status "Accepted" (2025-10-05)
- [x] **Design principles** documented and communicated to team
- [x] **Stakeholder sign-off** obtained (solo developer: documented self-approval 2025-10-05)

### Should Have (Non-Blocking)

- [ ] Architecture review workshop conducted
- [ ] Initial risk register created with owners
- [ ] Onboarding guide for new contributors drafted

### Nice to Have (Future)

- [ ] Demo of discovery findings to broader team
- [ ] Blog post on "Why we're not using MediatR"
- [ ] Comparison matrix with idevs-foundation

---

## Tracking Checklist

Use this checklist to track progress within Phase 0:

### Scope & Guardrails

- [x] Framework scope documented
- [x] Non-goals explicitly stated
- [x] Constraints captured (PostgreSQL, no reflection)
- [x] Relation to other projects clarified

### Stakeholders & Personas

- [x] Stakeholders identified and contacted
- [x] Tenant personas defined (4 types)
- [x] Primary scenarios prioritized
- [x] Acceptance criteria for each scenario

### Design Principles

- [x] CQRS approach decided (no MediatR)
- [x] DDD patterns documented
- [x] Multi-tenancy strategy outlined
- [x] TDD expectations set
- [x] Observability requirements defined

### Governance

- [x] ADR process established
- [x] ADR-0001 through ADR-0005 drafted and accepted
- [x] Review cadence scheduled (weekly during active phases)
- [x] Sign-off workflow defined (solo developer: documented self-approval)

**Notes on Governance**:

- **Git Flow**: Adopted (main/develop branches, feature branches, PRs)
- **GitVersion**: Configured with Conventional Commits for automated versioning
- **CI/CD**: Build, test with ≥80% branch coverage, automated releases
- **Central Package Management**: Directory.Packages.props for version control
- **Solo Developer Process**: As a solo developer project, stakeholder sign-off is documented self-approval with clear rationale and date logging (2025-10-05)

### Security & Compliance

- [x] Threat model baseline created
- [x] STRIDE analysis completed
- [x] ASVS checklist initialized
- [ ] Security champion assigned

### Documentation

- [x] Glossary published
- [x] Discovery summary completed
- [x] Observability blueprint completed
- [x] Cross-links validated (2025-10-05)
- [x] Contribution guide drafted (CONTRIBUTING.md created)

---

## Dependencies & Relationships

### Prerequisites

- None (Phase 0 is the foundation)

### Outputs to Other Phases

- **Phase 1 (Platform)**: Versioning strategy (ADR-0004), build requirements
- **Phase 2 (Domain)**: Design principles, DDD patterns, testing strategy
- **Phase 3 (Application)**: CQRS approach, tenant context requirements
- **Phase 4 (Web)**: Tenant resolution strategy, observability integration
- **Phase 5 (Infrastructure)**: Tenancy model (ADR-0001), audit logging (ADR-0002), soft delete (ADR-0003)
- **Phase 6 (Release)**: Release governance (ADR-0004), documentation standards

### External Dependencies

- Stakeholder availability for sign-off meetings
- Security team review capacity for threat model
- Legal review (optional) for compliance requirements

---

## Review Schedule

| Review Type | Cadence | Attendees | Purpose |
|-------------|---------|-----------|---------|
| **Phase Status Review** | Weekly | Phase owner + team | Track progress, blockers |
| **Architecture Review** | At exit criteria | Architects + stakeholders | Validate decisions |
| **Security Review** | Before sign-off | Security lead + phase owner | Threat model approval |
| **Stakeholder Sign-off** | End of phase | All stakeholders | Final approval |

---

## References

### Internal Documents

- [Discovery Summary](../discovery-summary.md)
- [CQRS Framework Plan](../cqrs-framework-plan.md)
- [Glossary](../glossary.md)
- [Threat Model](../threat-model.md)
- [Observability Blueprint](../observability-blueprint.md)
- [AGENTS.md](../../AGENTS.md)

### External References

- [STRIDE Threat Modeling](https://learn.microsoft.com/en-us/azure/security/develop/threat-modeling-tool-threats)
- [ASVS 4.0](https://owasp.org/www-project-application-security-verification-standard/)
- [Multi-Tenancy Patterns](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/overview)
- [Git Flow](https://nvie.com/posts/a-successful-git-branching-model/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Architecture Decision Records](https://adr.github.io/)

---

## Appendix: Phase 0 Timeline (Example)

```text
Week 1: Kickoff & Discovery
├─ Day 1-2: Stakeholder interviews
├─ Day 3: Discovery summary draft
└─ Day 4-5: Design principles workshop

Week 2: Documentation & Threat Modeling
├─ Day 1-2: Glossary and terminology
├─ Day 3: Threat model baseline
└─ Day 4-5: Observability requirements

Week 3: ADRs & Governance
├─ Day 1: ADR-0001 (Tenancy)
├─ Day 2: ADR-0002 (Audit)
├─ Day 3: ADR-0003 (Soft Delete)
├─ Day 4: ADR-0004 (Versioning)
└─ Day 5: Documentation review

Week 4: Review & Sign-off
├─ Day 1-2: Architecture review
├─ Day 3: Security review
├─ Day 4: Stakeholder presentation
└─ Day 5: Sign-off and Phase 1 kickoff
```text

**Note**: This is an example timeline. Actual duration depends on team size and stakeholder availability.

---

**Next Phase**: [Phase 1: Platform Scaffolding](../phase-1-platform/phase-1-platform.md)

**Document Maintainer**: Architecture Team  
**Last Review Date**: 2025-10-04  
**Next Review Date**: TBD (after ADR creation)

---

## Phase Completion

**Status**: ✅ **COMPLETE**  
**Completion Date**: 2025-10-05  
**Sign-off**: Documented self-approval (solo developer context)

### Summary

Phase 0 has been successfully completed with all exit criteria met:

- ✅ All foundational ADRs accepted (ADR-0001 through ADR-0005)
- ✅ Solution structure created with build system operational
- ✅ CI/CD workflows configured (build, test, release)
- ✅ GitVersion and semantic versioning implemented
- ✅ Baseline code with >80% test coverage (Guard class and tests)
- ✅ Documentation complete with cross-references validated

### Deviations Accepted

1. **Solo Developer Sign-off**: Self-approval documented with rationale (2025-10-05)
2. **NuGet Configuration**: Added NuGet.config to restrict package sources
3. **Warning Suppression**: NU1604 and NU1701 suppressed for CPM compatibility

### Artifacts Delivered

- Solution with `src/Idevs` and `tests/Idevs.Tests` projects
- Central Package Management (Directory.Packages.props)
- Shared build configuration (Directory.Build.props)
- Code style enforcement (.editorconfig)
- GitVersion configuration (GitVersion.yml)
- CI workflow (.github/workflows/ci.yml)
- Release workflow (.github/workflows/release.yml)
- Baseline code: Guard class, IIdevsMarker interface
- Test suite: 15 tests, 100% branch coverage
- CONTRIBUTING.md with development guidelines
- Phase 0 completion summary document

See [Phase 0 Completion Summary](./phase-0-completion-summary.md) for full details.

**Ready to proceed to Phase 1: Core Domain Abstractions**
