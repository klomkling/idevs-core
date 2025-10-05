# CQRS Framework Roadmap

**Document Owner**: Platform Team  
**Last Updated**: 2025-10-04  
**Status: Phase 0 - ✅ Complete (2025-10-05)

## Overview

This document provides the master roadmap for the **Idevs** framework (repo: `idevs-core`), a .NET building-block framework for modern SaaS and ERP applications. The framework is built on CQRS principles with multi-tenancy, DDD boundaries, and offline-first capabilities.

**Package Namespace**: `Idevs.*` (e.g., `Idevs`, `Idevs.Application`, `Idevs.Data.PostgreSQL`)

### Mission

Deliver a production-ready, CQRS-centric framework that:
- Reduces time-to-market for SaaS/ERP features by 40%
- Achieves 99.9% uptime for dedicated tenants
- Maintains ≥80% test coverage across all components
- Supports offline-first scenarios with conflict resolution
- Provides comprehensive audit trails for compliance

### Target Framework

- **Primary**: .NET 8.0 LTS
- **Future**: .NET 10.0 LTS (forward-compatibility plan in Phase 1)

## Phase Completion Checklist

### Phase 0: Discovery & Guardrails
**Status: ✅ Complete

- [ ] Stakeholder alignment on scope and personas
- [x] Discovery summary finalized
- [x] Glossary published with standardized terminology
- [x] Threat model baseline established
- [x] ADR-0001: Tenancy Strategy (Proposed/Accepted)
- [x] ADR-0002: Audit Logging (Proposed/Accepted)
- [x] ADR-0003: Soft Delete (Proposed/Accepted)
- [x] ADR-0004: Release Governance (Proposed/Accepted)
- [x] Security and compliance review completed
- [x] Non-functional requirements documented

**Exit Criteria**: All ADRs accepted, stakeholder sign-off on personas and scenarios.

**Completion Notes** (2025-10-05):
- All foundational ADRs accepted: Multi-Tenancy, Audit Logging, Soft Delete, Release Governance, Package Structure
- Solution structure created with Idevs core library and tests
- Build system operational: Directory.Build.props, Directory.Packages.props, .editorconfig
- GitVersion configured for semantic versioning
- CI/CD workflows implemented: build, test (≥80% coverage), release
- Baseline code: Guard class, IIdevsMarker interface (15 tests, 100% branch coverage)
- Documentation: CONTRIBUTING.md, Phase 0 completion summary
- Ready to proceed to Phase 1

See [Phase 0 Completion Summary](phase-0-discovery/phase-0-completion-summary.md) for full details.

---

### Phase 1: Platform Scaffold & Build Infrastructure
**Status**: ⏳ Not Started

- [ ] Repository structure documented (src/, tests/, docs/)
- [ ] Build strategy for .NET 8.0 LTS documented
- [ ] Forward-compatibility plan for .NET 10.0 LTS
- [ ] GitVersion configuration strategy documented
- [ ] Conventional Commits guide linked in README
- [ ] Git Flow branching model documented (main/develop/feature/hotfix/release)
- [ ] CI/CD workflow stages defined (build, test, coverage, pack, release)
- [ ] NuGet packaging conventions documented (package IDs, tags, metadata)
- [ ] Code analyzer and style guidelines documented
- [ ] Coverage reporting approach documented (Coverlet/ReportGenerator)
- [ ] Dependency management strategy (Directory.Packages.props plan)

**Exit Criteria**: Versioning policy approved, CI stages defined, branching model documented.

---

### Phase 2: Domain & Contracts
**Status**: ⏳ Not Started

- [ ] Core entity interfaces defined (IEntity, IAuditableEntity, ISoftDeletableEntity, ITenantEntity)
- [ ] Result patterns documented (Result, Result&lt;T&gt;, PagedResult&lt;T&gt;, ValidationResult)
- [ ] CQRS contracts defined (ICommand, IQuery, ICommandHandler, IQueryHandler)
- [ ] DDD boundaries and aggregate patterns established
- [ ] Repository pattern interfaces (IRepositoryBase, IAdvancedRepository, IUnitOfWork)
- [ ] Specification pattern for composable queries
- [ ] Testing approach documented (TDD, xUnit, Shouldly, NSubstitute)
- [ ] Modern C# feature usage guidelines (primary constructors, collection expressions, pattern matching)
- [ ] No System.Reflection constraint documented with alternatives
- [ ] Soft-delete semantics finalized (ties to ADR-0003)
- [ ] Audit fields and change tracking strategy

**Exit Criteria**: Contracts reviewed, testing plan approved, reflection usage risks addressed.

---

### Phase 3: Application Layer & Execution Pipeline
**Status**: ⏳ Not Started

- [ ] Handler invocation patterns documented (direct injection, optional lightweight dispatcher)
- [ ] Cross-cutting concerns strategy: Middleware, Decorators, EF Interceptors, Action Filters
- [ ] Service layer base classes (ServiceBase, ReadOnlyServiceBase, CqrsServiceBase)
- [ ] Tenant context propagation across pipeline
- [ ] Idempotency and deduplication guidance
- [ ] Error handling with Result pattern
- [ ] Attribute-based DI strategy (no reflection - use source generators or explicit registration)
- [ ] Decorator base classes for validation, logging, caching, performance tracking
- [ ] Circuit breaker and retry policies (via decorators)
- [ ] Dead letter queue for failed commands
- [ ] Performance monitoring via decorators and middleware
- [ ] Cache key strategy for cacheable requests

**Exit Criteria**: Cross-cutting patterns defined, decorator approach approved, tenant context flows validated.

---

### Phase 4: Web Adapters & Sync Endpoints
**Status**: ⏳ Not Started

- [ ] ASP.NET Core base controllers for CQRS endpoints
- [ ] Minimal API patterns for commands and queries
- [ ] Result mapping to HTTP status codes
- [ ] Tenant resolution strategies (header, subdomain, token claim, precedence)
- [ ] Correlation ID middleware
- [ ] Exception handling middleware
- [ ] Rate limiting and throttling middleware
- [ ] GraphQL integration plan (HotChocolate)
- [ ] GraphQL query resolvers to IQuery handlers
- [ ] Pagination, batching, and DataLoader patterns
- [ ] Offline delta sync endpoints (ETags, sync tokens)
- [ ] API versioning strategy

**Exit Criteria**: Tenant resolution strategies documented, HTTP/GraphQL mapping approved.

---

### Phase 5: Infrastructure Extensibility & Persistence
**Status**: ⏳ Not Started

- [ ] Repository implementations with EF Core
- [ ] PostgreSQL-first approach documented
- [ ] Row-level multi-tenancy (tenant_id) with optional RLS
- [ ] Global query filters for soft-delete
- [ ] EF Core interceptors for audit logging
- [ ] Migration policy and schema evolution guidance
- [ ] Caching strategy (read-through, invalidation patterns)
- [ ] Outbox/Inbox pattern for transactional messaging
- [ ] Append-only audit store
- [ ] Connection string management and tenant routing
- [ ] Database index strategy for performance
- [ ] Integration test fixtures with PostgreSQL

**Exit Criteria**: Repository interfaces finalized, EF tenancy/soft-delete validated, audit logging approved.

---

### Phase 6: Documentation, Samples & Release Readiness
**Status**: ⏳ Not Started

- [ ] API documentation complete (XML comments)
- [ ] Usage guides for all major features
- [ ] Sample application: Multi-store retail
- [ ] Sample application: Subscription billing
- [ ] Sample application: Accounting/finance
- [ ] Quickstart guide for new developers
- [ ] Security posture documentation
- [ ] Offline-first guidance and best practices
- [ ] Migration guide (if applicable)
- [ ] NuGet package metadata finalized (README, release notes, description)
- [ ] Release notes template and automation
- [ ] Performance benchmarks and regression gates
- [ ] Documentation completeness review

**Exit Criteria**: Docs reviewed, samples functional, release governance enacted.

---

## Success Metrics

### Code Quality
- [ ] **≥80% Branch Coverage**: Enforced across all components
- [ ] **Zero Compiler Warnings**: Warnings-as-errors enabled
- [ ] **Mutation Testing**: ≥70% mutation score on critical paths
- [ ] **Static Analysis**: Clean SonarQube/CodeQL scans

### Performance
- [ ] **P99 Command Latency**: <150ms under nominal load
- [ ] **Database Query Optimization**: All queries analyzed with execution plans
- [ ] **Cache Hit Ratio**: ≥80% for frequently accessed data
- [ ] **Throughput**: Support 10,000 commands/sec per instance

### Security
- [ ] **ASVS Level 2 Compliance**: All controls verified
- [ ] **Dependency Scanning**: No high/critical vulnerabilities
- [ ] **Secrets Management**: Zero secrets in repository
- [ ] **Penetration Testing**: External security audit passed

### Observability
- [ ] **Structured Logging**: All commands/queries logged with correlation IDs
- [ ] **Distributed Tracing**: End-to-end trace coverage with OpenTelemetry
- [ ] **RED Metrics**: Request rate, error rate, duration tracked
- [ ] **SLO Monitoring**: 99.9% uptime for dedicated tenants, 99.5% for multi-tenant

### Documentation
- [ ] **API Coverage**: 100% public API documented with XML comments
- [ ] **Sample Applications**: All three first-party scenarios covered
- [ ] **Developer Onboarding**: New developers productive within 2 days
- [ ] **Runbooks**: Incident response and operational procedures

## GitVersion & Release Strategy

### Semantic Versioning

Versions calculated automatically by GitVersion based on Git Flow branches and Conventional Commits:

```
Major.Minor.Patch-PreReleaseTag.BuildMetadata
```

**Examples**:
- `1.0.0` - Production release from `main`
- `1.1.0-alpha.23` - Feature in `develop` branch
- `1.0.1-hotfix.5` - Hotfix branch

### Version Bumps

| Commit Type | Example | Version Impact |
|-------------|---------|----------------|
| `feat:` | `feat: add tenant resolution middleware` | Minor bump (x.Y.0) |
| `fix:` | `fix: correct soft-delete filter` | Patch bump (x.y.Z) |
| `feat!:` or `BREAKING CHANGE:` | `feat!: revamp CQRS pipeline` | Major bump (X.0.0) |
| `docs:`, `chore:`, `test:` | `docs: update readme` | No version bump |

### Branch Strategy

```
main (production)
  └── develop (integration)
       ├── feature/* → merge to develop → v1.1.0-alpha.X
       ├── hotfix/* → merge to develop → v1.0.1-alpha.X
       └── release/* → merge to main → v1.1.0
```

**Workflow**:
1. Features developed in `feature/*` branches
2. Merge to `develop` for integration testing → preview packages to GitHub Packages
3. Hotfixes developed in `hotfix/*` branches (like features, but bump patch version)
4. Merge `hotfix/*` to `develop` for testing → preview packages to GitHub Packages
5. Create `release/*` branch when ready for production (includes hotfixes)
6. Merge `release/*` to `main` → publish to NuGet.org with tag
7. All changes flow through `develop` → `release/*` → `main` (no direct merges to main)

## Threat Model Baseline

| Asset | Threat | Mitigation | ASVS Control |
|-------|--------|------------|--------------|
| Tenant Data | Cross-tenant data leakage | Automatic tenant filters, RLS | V4.1.1 |
| Credentials | Secret exposure in logs | PII redaction, secret managers | V7.2.1 |
| API Endpoints | Injection attacks | Input validation, parameterized queries | V5.3.1 |
| Audit Logs | Tampering | Immutable logs, cryptographic signing | V9.2.1 |
| Cache | Cache poisoning | Tenant-aware cache keys | V4.1.2 |

**Full Threat Model**: [threat-model.md](threat-model.md)

## Risk Register

| Risk | Impact | Probability | Mitigation | Owner |
|------|--------|-------------|------------|-------|
| Tenant data isolation breach | Critical | Low | Automated tests, code review, audit | Security Team |
| Performance regression | High | Medium | Benchmarks in CI, load testing | Platform Team |
| Version drift | Medium | Medium | GitVersion automation, branch policies | Platform Team |
| Dependency vulnerability | High | Medium | Automated scanning, update policy | Security Team |
| Breaking API changes | High | Low | Semantic versioning, deprecation policy | Platform Team |
| Incomplete documentation | Medium | High | Docs in CI checks, review gates | DevEx Team |

## Open Issues & Dependencies

### Technical Dependencies
- [ ] Finalize Serilog sink configuration for multi-tenant logging
- [ ] Determine GraphQL schema generation approach (code-first vs schema-first)
- [ ] Select caching provider (Redis vs Memcached vs hybrid)
- [ ] Evaluate AOT compilation support for .NET 10.0

### Organizational Dependencies
- [ ] Secure budget for external security audit (Phase 6)
- [ ] Provision PostgreSQL instances for integration testing
- [ ] Set up GitHub Packages and NuGet.org publishing credentials
- [ ] Establish incident response runbook template

### Third-Party Dependencies
- [ ] Monitor EF Core 9.x for PostgreSQL compatibility
- [ ] Track HotChocolate roadmap for GraphQL features
- [ ] Verify OpenTelemetry .NET SDK stability
- [ ] Assess FluentValidation performance under load

## Release Readiness Checklist

Before declaring the framework "release-ready" (v1.0.0):

### Functional Completeness
- [ ] All Phase 0-6 deliverables completed
- [ ] All acceptance criteria met per phase
- [ ] Sample applications functional and documented

### Quality Gates
- [ ] ≥80% branch coverage achieved
- [ ] Zero high/critical security vulnerabilities
- [ ] Performance benchmarks meet targets (P99 <150ms)
- [ ] Load testing passed (10k commands/sec)

### Documentation
- [ ] API documentation complete
- [ ] User guides for all scenarios
- [ ] Migration guides (if applicable)
- [ ] Security posture statement published

### Operational Readiness
- [ ] Observability dashboards configured
- [ ] Alert rules defined and tested
- [ ] Incident response runbooks created
- [ ] Support escalation paths established

### Compliance
- [ ] ASVS Level 2 controls verified
- [ ] Audit log retention policies enforced
- [ ] GDPR right-to-erasure validated
- [ ] External security audit passed

### Release Process
- [ ] Release notes template finalized
- [ ] NuGet package metadata complete
- [ ] GitHub release automation configured
- [ ] Versioning policy documented and automated

## References

- [Discovery Summary](discovery-summary.md)
- [Glossary](glossary.md)
- [Threat Model](threat-model.md)
- [Observability Blueprint](observability-blueprint.md)
- [Phase 0: Discovery](phase-0-discovery/phase-0-discovery.md)
- [Phase 1: Platform](phase-1-platform/phase-1-platform.md)
- [Phase 2: Domain](phase-2-domain/phase-2-domain.md)
- [Phase 3: Application](phase-3-application/phase-3-application.md)
- [Phase 4: Web](phase-4-web/phase-4-web.md)
- [Phase 5: Infrastructure](phase-5-infrastructure/phase-5-infrastructure.md)
- [Phase 6: Release](phase-6-release/phase-6-release.md)

---

**Status Legend**:
- ✅ **Complete**: All deliverables finished and approved
- 🔄 **In Progress**: Active work ongoing
- ⏳ **Not Started**: Planned but not yet initiated
- ⚠️ **Blocked**: Waiting on dependency or decision

**Next Review**: Weekly during active phases, monthly during stabilization
