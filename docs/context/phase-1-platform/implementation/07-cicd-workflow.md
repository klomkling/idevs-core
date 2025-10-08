# Phase 1: CI/CD Workflow Design

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Design GitHub Actions workflow for automated build, test, and release pipeline.

## Workflow Stages

```text
1. Build → 2. Test → 3. Integration Test → 4. E2E Test (parallel)
                  ↓
5. Coverage → 6. Architecture Test → 7. Package → 8. Publish
```

## Workflow File

**File:** `.github/workflows/ci.yml` (documented, implementation in Phase 6)

See [phase-1-platform-ORIGINAL.md](../phase-1-platform-ORIGINAL.md#7-cicd-workflow-design) lines 635-910 for complete YAML configuration.

### Key Stages

**Stage 1: Build**

```yaml
- name: Build
  run: dotnet build --configuration Release
```

**Stage 2: Unit Tests**

```yaml
- name: Run unit tests
  run: dotnet test --filter "Category!=Integration"
```

**Stage 3: Integration Tests (InMemory)**

```yaml
- name: Run integration tests
  run: dotnet test --filter "Category=Integration&Category!=RequiresPostgreSQL"
```

**Stage 4: E2E Tests (PostgreSQL)**

```yaml
services:
  postgres:
    image: postgres:16
    
- name: Run E2E tests
  run: dotnet test --filter "Category=E2E"
```

**Stage 5: Coverage Check**

```yaml
- name: Check coverage threshold
  run: |
    COVERAGE=$(jq -r '.summary.lineCoverage' ./coverage-report/Summary.json)
    if (( $(echo "$COVERAGE < 80" | bc -l) )); then
      exit 1
    fi
```

## Quality Gates

| Gate | Threshold | Action on Failure |
|------|-----------|-------------------|
| Build | Must succeed | Block merge |
| Unit Tests | 100% pass | Block merge |
| Integration Tests | 100% pass | Block merge |
| Code Coverage | ≥80% | Block merge |
| Architecture Tests | 100% pass | Block merge |
| Code Style | Zero warnings | Block merge |

## Next Steps

- **[Testing Strategy](08-testing-strategy.md)** - Hybrid test approach
- **[NuGet Packaging](09-nuget-packaging.md)** - Package configuration

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: Testing Strategy →](08-testing-strategy.md)
