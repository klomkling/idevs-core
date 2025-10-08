# Phase 2 Domain - Cleanup & Restructuring Plan

> **Status:** Ready to Execute  
> **Created:** 2025-10-08  
> **Estimated Time:** 4-6 hours

## Executive Summary

This plan addresses discrepancies between the phase-2-domain folder's current state and the DOCUMENTATION-RESTRUCTURING-PLAN.md requirements. The cleanup involves:

1. **Archiving** old/tracking files (7 files)
2. **Splitting** oversized implementation files (6 files → 12+ files)
3. **Enforcing** 450-line limit and H3 max heading depth
4. **Updating** all cross-references and navigation links

## Current Issues

### ❌ Line Count Violations

| File | Current Lines | Limit | Excess |
|------|---------------|-------|--------|
| `02-value-objects.md` | 742 | 450 | +292 |
| `05-aggregates-entities.md` | 863 | 450 | +413 |
| `06-tenant-user-context.md` | 738 | 450 | +288 |
| `07-repository-uow.md` | 672 | 450 | +222 |
| `08-domain-events.md` | 690 | 450 | +240 |
| `09-specifications.md` | 800 | 450 | +350 |

**Total violations:** 6 files, 1,805 excess lines

### ❌ Structure Issues

- Multiple overview files (phase-2-domain.md, phase-2-domain-NEW.md, README.md)
- Old 1,359-line file not archived
- 6 tracking files cluttering root
- No clear navigation between split files

### ❌ Plan Compliance

The original plan expected:
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

But we have 9 implementation files with different naming and scope.

## Proposed Structure (After Cleanup)

```
phase-2-domain/
├── phase-2-domain.md              # Main overview (133 lines) ✅
├── README.md                      # Detailed guide (376 lines) ✅
├── COMPLETION-GUIDE.md            # Checklist (433 lines) ✅
├── REFERENCES.md                  # Resources (393 lines) ✅
├── archive/                       # Historical files
│   ├── phase-2-domain-ORIGINAL.md
│   ├── CHANGELOG.md
│   ├── COMPLETION-SUMMARY.md
│   ├── LINT-REPORT.md
│   ├── PHASE-2-COMPLETION.md
│   ├── REFERENCES-VALIDATED.md
│   └── REVIEW-SIGNOFF.md
└── implementation/
    ├── README.md                  # Implementation index
    ├── 01-entity-interfaces.md    # (390 lines) ✅
    ├── 02-value-objects-base.md   # Base + Email + Money (~370 lines)
    ├── 02-value-objects-examples.md # Address + DateRange + Tests (~370 lines)
    ├── 03-result-pattern-core.md  # Result<T> + Error (~315 lines)
    ├── 03-result-pattern-extensions.md # Paged + Validation (~315 lines)
    ├── 04-cqrs-contracts-basic.md # Interfaces (~310 lines)
    ├── 04-cqrs-contracts-advanced.md # Decorators + Tests (~310 lines)
    ├── 05-aggregates-base.md      # Entity + AggregateRoot + Guard (~430 lines)
    ├── 05-aggregates-examples.md  # Customer + Order + Tests (~430 lines)
    ├── 06-tenant-context.md       # Tenant-related (~370 lines)
    ├── 06-user-context.md         # User-related (~370 lines)
    ├── 07-repository-pattern.md   # IRepository + Specs (~335 lines)
    ├── 07-unit-of-work.md         # IUnitOfWork + Events (~335 lines)
    ├── 08-domain-events-contracts.md # Interfaces (~345 lines)
    ├── 08-domain-events-impl.md   # Dispatcher + Handlers (~345 lines)
    ├── 09-specifications-pattern.md # ISpecification + Base (~400 lines)
    ├── 09-specifications-examples.md # Domain specs + Tests (~400 lines)
    └── archive/                   # Original oversized files
        ├── 02-value-objects.md
        ├── 03-result-patterns.md
        ├── 04-cqrs-contracts.md
        ├── 05-aggregates-entities.md
        ├── 06-tenant-user-context.md
        ├── 07-repository-uow.md
        ├── 08-domain-events.md
        └── 09-specifications.md
```

**Result:** 17 implementation files, all under 450 lines ✅

## Execution Steps

The plan is broken into 21 steps, organized as follows:

### Phase 1: Setup & Baseline (Steps 1-6)

1. **Create working branch** - Isolate changes
2. **Create plan folder** - Satisfy context rule
3. **Create archive directories** - Prepare storage
4. **Move tracking files** - Clean up root
5. **Archive old overview** - Promote new version
6. **Verify root files** - Ensure structure

**Duration:** 30 minutes

### Phase 2: Split Files (Steps 7-14)

Split each oversized file into logical parts:

7. **02-value-objects** → base + examples
8. **05-aggregates-entities** → base + examples
9. **06-tenant-user-context** → tenant + user
10. **07-repository-uow** → repository + unit-of-work
11. **08-domain-events** → contracts + impl
12. **09-specifications** → pattern + examples
13. **03-result-patterns** → core + extensions
14. **04-cqrs-contracts** → basic + advanced

**Duration:** 2-3 hours (each file ~20-30 min)

### Phase 3: Polish & Validate (Steps 15-21)

15. **Add navigation blocks** - Cross-link split files
16. **Update cross-references** - Fix all internal links
17. **Create implementation index** - Add README.md
18. **Validate line counts** - Ensure all ≤450
19. **Run markdownlint** - Enforce H3 + rules
20. **Verify links** - Check all anchors resolve
21. **Finalize & PR** - Conventional commits

**Duration:** 1-2 hours

## Success Criteria

At completion:

- ✅ All files ≤ 450 lines
- ✅ Max heading depth H3
- ✅ 100% markdownlint pass
- ✅ Old files archived, not deleted
- ✅ Clear navigation between parts
- ✅ All cross-references updated
- ✅ Implementation index created
- ✅ Conventional commits used

## Splitting Strategy

### Logical Split Points

For each file, split along natural boundaries:

| File | Split Point | Rationale |
|------|-------------|-----------|
| value-objects | After Money | Base types vs Examples |
| aggregates | After Guard | Foundation vs Usage |
| tenant-user | After ITenantContext | Separate concerns |
| repository-uow | After IRepository | Repository vs Transaction |
| domain-events | After interfaces | Contracts vs Implementation |
| specifications | After base class | Pattern vs Usage |
| result-patterns | After Result<T> | Core vs Extensions |
| cqrs-contracts | After handlers | Basics vs Advanced |

### Navigation Pattern

Each split file gets top/bottom navigation:

```markdown
> **Navigation:** [Index](README.md) • [Part 1](XX-topic-part1.md) • [Part 2](XX-topic-part2.md)

# Content here...

---

**Next:** [Part 2: Examples](XX-topic-examples.md)
```

## Risk Management

| Risk | Mitigation |
|------|------------|
| Content loss | Work on feature branch, commit often |
| Broken links | Use sed/rg for bulk updates, verify with link checker |
| Merge conflicts | Complete in single session, merge promptly |
| Time overrun | Prioritize critical files first (02, 05, 06) |

## Tools Required

- `git` - Version control
- `rg` (ripgrep) - Link scanning
- `sd` - Bulk replacements
- `npx markdownlint-cli` - Linting
- `npx markdown-link-check` - Link validation

## Quick Start

To begin execution:

```bash
cd /Users/sarawut/GitHub/Packages/nuget/warp-idevs-core
git checkout -b docs/phase-2-domain-restructure
export ROOT=/Users/sarawut/GitHub/Packages/nuget/warp-idevs-core/docs/context/phase-2-domain

# Follow steps 1-21 in sequence
# Track progress by marking todos complete
```

## Timeline

- **Setup:** 30 min
- **Splitting:** 2-3 hours (8 files)
- **Polish:** 1-2 hours
- **Total:** 4-6 hours

Recommend completing in one focused session to avoid merge conflicts.

## Related Documents

- [DOCUMENTATION-RESTRUCTURING-PLAN.md](../DOCUMENTATION-RESTRUCTURING-PLAN.md) - Master plan
- [Phase 6 Pattern](../phase-6-release/RESTRUCTURE-PLAN.md) - Reference example
- [README.md](README.md) - Current comprehensive guide

---

**Status:** Ready to execute. All steps documented in agent todo list.  
**Next Action:** Run step 1 (Create working branch)
