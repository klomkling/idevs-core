# Documentation Restructuring Master Plan

> **Status:** Planning
> **Created:** 2025-10-08
> **Last Updated:** 2025-10-08

## Executive Summary

This plan outlines the comprehensive restructuring of all `idevs-core` documentation to improve maintainability, readability, and discoverability. The restructuring follows the successful pattern established with Phase 6.

### Goals

- Break large monolithic files into focused, modular documents
- Keep all files under 450 lines for lint-friendliness
- Limit heading depth to H3 for better navigation
- Establish consistent structure across all documentation
- Improve cross-linking and discoverability
- Pass all markdownlint validation

### Success Metrics

- ✅ All files < 450 lines
- ✅ Max heading depth H3
- ✅ 100% markdownlint pass rate
- ✅ Clear navigation between documents
- ✅ Consistent folder structure

## Current State Analysis

### Documentation Inventory

| Category | File | Lines | Priority | Status |
|----------|------|-------|----------|--------|
| **Phase Plans** |
| Phase 6 | `phase-6-release.md` | 127 | - | ✅ Complete |
| Phase 1 | `phase-1-platform.md` | 1,799 | High | 📋 Planned |
| Phase 5 | `phase-5-infrastructure.md` | 1,654 | High | 📋 Planned |
| Phase 4 | `phase-4-web.md` | 1,582 | High | 📋 Planned |
| Phase 3 | `phase-3-application.md` | 1,544 | High | 📋 Planned |
| Phase 2 | `phase-2-domain.md` | 1,359 | High | 📋 Planned |
| Phase 0 | `phase-0-discovery.md` | 764 | Medium | 📋 Planned |
| **Architecture** |
| Observability | `observability-blueprint.md` | 2,387 | Critical | 📋 Planned |
| Threat Model | `threat-model.md` | 519 | Medium | 📋 Planned |
| CQRS Plan | `cqrs-framework-plan.md` | ~600 | Medium | 📋 Planned |
| **ADRs** |
| ADR-003 | `ADR-0003-Soft-Delete.md` | 1,153 | Medium | 📋 Planned |
| ADR-004 | `ADR-0004-Release-Governance.md` | 1,074 | Medium | 📋 Planned |
| ADR-002 | `ADR-0002-Audit-Logging.md` | 899 | Low | 📋 Planned |
| ADR-005 | `ADR-0005-DI-Container-Strategy.md` | 671 | Low | 📋 Planned |
| ADR-001 | `ADR-0001-Tenancy-Strategy.md` | 489 | Low | 📋 Planned |
| **Reference** |
| Glossary | `glossary.md` | ~800 | Low | 📋 Planned |
| Discovery | `discovery-summary.md` | ~600 | Low | 📋 Planned |

### Total Scope

- **17 documents** requiring restructuring
- **~20,000 lines** to reorganize
- **Estimated effort:** 6-8 weeks (1-2 docs per week)

## Restructuring Strategy

### Standard Pattern

Each large document follows this template:

```
docs/context/{topic}/
├── {topic}.md                    # Concise overview (< 200 lines)
├── COMPLETION-GUIDE.md           # How to complete/implement
├── REFERENCES.md                 # External links and resources
└── implementation/
    ├── 01-{section}.md          # Focused implementation guide
    ├── 02-{section}.md
    ├── 03-{section}.md
    └── ...
```

### File Size Guidelines

| File Type | Max Lines | Max Heading |
|-----------|-----------|-------------|
| Overview | 200 | H2 |
| Implementation | 450 | H3 |
| Reference | 300 | H2 |
| Completion | 200 | H3 |

### Naming Conventions

- **Main file:** Same as folder name (e.g., `phase-1-platform.md`)
- **Implementation:** Numbered with descriptive names (e.g., `01-abstractions.md`)
- **Support docs:** ALL CAPS (e.g., `COMPLETION-GUIDE.md`, `REFERENCES.md`)

## Phase-by-Phase Roadmap

### Phase 1: Core Implementation Plans (Weeks 1-6)

Restructure the 6 phase implementation plans following Phase 6 pattern.

#### Week 1: Phase 1 - Platform Foundation

**File:** `phase-1-platform.md` (1,799 lines)

**Proposed structure:**

```
phase-1-platform/
├── phase-1-platform.md           # Overview
├── COMPLETION-GUIDE.md
├── REFERENCES.md
└── implementation/
    ├── 01-abstractions.md        # Core abstractions (ICommand, IQuery, IEvent)
    ├── 02-result-pattern.md      # Result<T>, Error handling
    ├── 03-validation.md          # FluentValidation integration
    ├── 04-domain-primitives.md   # EntityId, ValueObject
    ├── 05-repository-pattern.md  # IRepository, specifications
    └── 06-dependency-injection.md # DI container strategy
```

**Estimated lines:**
- Overview: ~150
- Implementations: 6 × ~280 = ~1,680
- Support: ~200

#### Week 2: Phase 2 - Domain Layer

**File:** `phase-2-domain.md` (1,359 lines)

**Proposed structure:**

```
phase-2-domain/
├── phase-2-domain.md
├── COMPLETION-GUIDE.md
└── implementation/
    ├── 01-aggregate-roots.md
    ├── 02-domain-events.md
    ├── 03-value-objects.md
    ├── 04-domain-services.md
    └── 05-specifications.md
```

#### Week 3: Phase 3 - Application Layer

**File:** `phase-3-application.md` (1,544 lines)

**Proposed structure:**

```
phase-3-application/
├── phase-3-application.md
├── COMPLETION-GUIDE.md
└── implementation/
    ├── 01-command-handlers.md
    ├── 02-query-handlers.md
    ├── 03-command-bus.md
    ├── 04-query-bus.md
    ├── 05-validation-pipeline.md
    └── 06-dto-mapping.md
```

#### Week 4: Phase 4 - Web Layer

**File:** `phase-4-web.md` (1,582 lines)

**Proposed structure:**

```
phase-4-web/
├── phase-4-web.md
├── COMPLETION-GUIDE.md
└── implementation/
    ├── 01-api-controllers.md
    ├── 02-middleware.md
    ├── 03-exception-handling.md
    ├── 04-authentication.md
    ├── 05-authorization.md
    └── 06-swagger-integration.md
```

#### Week 5: Phase 5 - Infrastructure

**File:** `phase-5-infrastructure.md` (1,654 lines)

**Proposed structure:**

```
phase-5-infrastructure/
├── phase-5-infrastructure.md
├── COMPLETION-GUIDE.md
└── implementation/
    ├── 01-ef-core-setup.md
    ├── 02-postgresql-provider.md
    ├── 03-migrations.md
    ├── 04-repository-impl.md
    ├── 05-unit-of-work.md
    └── 06-caching.md
```

#### Week 6: Phase 0 - Discovery

**File:** `phase-0-discovery.md` (764 lines)

**Proposed structure:**

```
phase-0-discovery/
├── phase-0-discovery.md
├── COMPLETION-GUIDE.md
└── implementation/
    ├── 01-requirements-analysis.md
    ├── 02-architecture-decisions.md
    ├── 03-technology-choices.md
    └── 04-project-setup.md
```

### Phase 2: Architecture Documents (Weeks 7-8)

#### Week 7: Observability Blueprint 🚨 PRIORITY

**File:** `observability-blueprint.md` (2,387 lines - LARGEST!)

**Proposed structure:**

```
observability/
├── observability-blueprint.md    # Overview
├── IMPLEMENTATION-GUIDE.md
└── implementation/
    ├── 01-logging-strategy.md
    ├── 02-structured-logging.md
    ├── 03-metrics-collection.md
    ├── 04-distributed-tracing.md
    ├── 05-health-checks.md
    ├── 06-alerting-rules.md
    └── 07-dashboard-setup.md
```

**Rationale:** This is the largest file and likely has the most diverse content.

#### Week 8: Threat Model & CQRS Plan

**Files:**
- `threat-model.md` (519 lines)
- `cqrs-framework-plan.md` (~600 lines)

**Proposed structures:**

```
threat-model/
├── threat-model.md
└── implementation/
    ├── 01-threat-analysis.md
    ├── 02-mitigation-strategies.md
    └── 03-security-controls.md

cqrs-framework/
├── cqrs-framework-plan.md
└── implementation/
    ├── 01-architecture-overview.md
    ├── 02-command-pipeline.md
    ├── 03-query-pipeline.md
    └── 04-event-handling.md
```

### Phase 3: ADRs (Weeks 9-10)

Restructure Architecture Decision Records with consistent format.

#### Week 9: Large ADRs

**Files:**
- `ADR-0003-Soft-Delete.md` (1,153 lines)
- `ADR-0004-Release-Governance.md` (1,074 lines)

**Standard ADR structure:**

```
adrs/ADR-XXXX-{topic}/
├── ADR-XXXX-{topic}.md           # Decision summary
└── implementation/
    ├── 01-context.md             # Problem statement
    ├── 02-decision.md            # Chosen approach
    ├── 03-implementation.md      # How to implement
    ├── 04-consequences.md        # Trade-offs
    └── 05-examples.md            # Code examples
```

#### Week 10: Medium ADRs

**Files:**
- `ADR-0002-Audit-Logging.md` (899 lines)
- `ADR-0005-DI-Container-Strategy.md` (671 lines)
- `ADR-0001-Tenancy-Strategy.md` (489 lines)

### Phase 4: Reference Documents (Week 11)

#### Week 11: Glossary & Discovery Summary

**Files:**
- `glossary.md` (~800 lines)
- `discovery-summary.md` (~600 lines)

These may just need cleanup and categorization rather than full restructuring.

## Implementation Guidelines

### Per-Document Workflow

1. **Analyze** (30 min)
   - Read through document
   - Identify major sections
   - Map to implementation files
   - Create file outline

2. **Create structure** (15 min)
   - Create folder
   - Create overview file
   - Create implementation folder
   - Create placeholder files

3. **Extract content** (2-4 hours)
   - Copy sections to implementation files
   - Rewrite headings to H3 max
   - Add cross-links
   - Ensure each file < 450 lines

4. **Polish** (1 hour)
   - Add navigation links
   - Create COMPLETION-GUIDE
   - Update references
   - Fix formatting

5. **Validate** (30 min)
   - Run markdownlint
   - Fix linting issues
   - Verify links
   - Check file sizes

6. **Commit** (15 min)
   - Stage files
   - Write descriptive commit message
   - Push to feature branch

**Total time per document:** ~4-6 hours

### Quality Checklist

For each restructured document:

- [ ] Overview file < 200 lines
- [ ] All implementation files < 450 lines
- [ ] Max heading depth H3
- [ ] COMPLETION-GUIDE created
- [ ] Cross-links added
- [ ] Navigation clear
- [ ] Markdownlint passes
- [ ] Committed with conventional commit
- [ ] Peer reviewed (optional)

## Tooling & Automation

### Linting

```bash
# Lint specific folder
npx markdownlint-cli docs/context/phase-X/**/*.md

# Lint all docs
npx markdownlint-cli docs/context/**/*.md

# Auto-fix simple issues
npx markdownlint-cli --fix docs/context/**/*.md
```

### Line Counting

```bash
# Count lines in file
wc -l docs/context/phase-X/phase-X.md

# Count lines in folder
find docs/context/phase-X -name "*.md" -exec wc -l {} \; | awk '{sum+=$1} END {print sum}'
```

### Link Validation

```bash
# Check for broken links (if tool available)
markdown-link-check docs/context/phase-X/**/*.md
```

## Risk Management

| Risk | Impact | Mitigation |
|------|--------|------------|
| Content loss during restructuring | High | Work on feature branches, commit often |
| Broken links | Medium | Use relative paths, validate with tools |
| Inconsistent structure | Medium | Follow template strictly, use checklist |
| Merge conflicts | Low | Work on one doc at a time, merge regularly |
| Time overruns | Low | Set realistic expectations, prioritize critical docs |

## Timeline Summary

| Week | Focus | Deliverable |
|------|-------|-------------|
| 1 | Phase 1 Platform | Restructured phase-1-platform/ |
| 2 | Phase 2 Domain | Restructured phase-2-domain/ |
| 3 | Phase 3 Application | Restructured phase-3-application/ |
| 4 | Phase 4 Web | Restructured phase-4-web/ |
| 5 | Phase 5 Infrastructure | Restructured phase-5-infrastructure/ |
| 6 | Phase 0 Discovery | Restructured phase-0-discovery/ |
| 7 | Observability | Restructured observability/ |
| 8 | Threat Model + CQRS | Restructured threat-model/, cqrs-framework/ |
| 9 | Large ADRs | Restructured ADR-003, ADR-004 |
| 10 | Medium ADRs | Restructured ADR-001, ADR-002, ADR-005 |
| 11 | Reference Docs | Cleaned glossary, discovery-summary |

**Total Duration:** 11 weeks (~3 months)

## Success Criteria

At completion:

- ✅ All 17 target documents restructured
- ✅ 100% markdownlint compliance
- ✅ All files should be < 450 lines but if it contains examples code blocks allow limit to < 1000 lines
- ✅ Consistent folder structure
- ✅ Cross-linking complete
- ✅ Navigation clear and intuitive
- ✅ COMPLETION-GUIDE for each major section
- ✅ All changes committed and documented

## Next Steps

1. **Review this plan** with team/stakeholders
2. **Create tracking issue** on GitHub
3. **Set up feature branch** for restructuring work
4. **Begin with Week 1** (Phase 1 Platform)
5. **Track progress** weekly
6. **Adjust timeline** as needed

## References

- [Phase 6 Restructuring](phase-6-release/RESTRUCTURE-PLAN.md) - Successful pattern
- [Markdown Style Guide](https://www.markdownguide.org/basic-syntax/)
- [Markdownlint Rules](https://github.com/DavidAnson/markdownlint/blob/main/doc/Rules.md)

---

**Ready to start?** Begin with [Week 1: Phase 1 Platform](#week-1-phase-1---platform-foundation)
