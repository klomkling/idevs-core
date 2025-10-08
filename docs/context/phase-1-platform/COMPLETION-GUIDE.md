# Phase 1 Completion Guide

This guide provides a step-by-step checklist for completing Phase 1: Platform Scaffolding.

## Implementation Order

Follow these steps in sequence for optimal results:

### Step 1: Solution Structure (Day 1)

**Objective:** Define project organization and naming conventions

**Tasks:**

1. Create solution file: `dotnet new sln -n idevs-core`
2. Create `src/` and `tests/` directories
3. Document project naming conventions
4. Define dependency flow rules

**Validation:**

- [ ] Solution file exists
- [ ] Folder structure matches documented layout
- [ ] Naming conventions documented

**Reference:** [01-solution-structure.md](implementation/01-solution-structure.md)

### Step 2: SDK & Targeting (Day 1)

**Objective:** Pin .NET SDK version for reproducible builds

**Tasks:**

1. Create `global.json` with SDK version 8.0.100
2. Set `rollForward` to `latestMinor`
3. Configure target framework as `net8.0`
4. Document multi-targeting strategy for future

**Validation:**

- [ ] `global.json` exists
- [ ] `dotnet --version` shows pinned version
- [ ] All projects target `net8.0`

**Reference:** [02-sdk-targeting.md](implementation/02-sdk-targeting.md)

### Step 3: Package Management (Day 2)

**Objective:** Centralize dependency management

**Tasks:**

1. Create `Directory.Packages.props`
2. Enable `ManagePackageVersionsCentrally`
3. List all package versions centrally
4. Remove version attributes from project files

**Validation:**

- [ ] `Directory.Packages.props` exists
- [ ] All package versions centralized
- [ ] `dotnet restore` succeeds

**Reference:** [03-package-management.md](implementation/03-package-management.md)

### Step 4: Build Properties (Day 2)

**Objective:** Share MSBuild configuration across projects

**Tasks:**

1. Create `Directory.Build.props`
2. Configure nullable reference types
3. Set `TreatWarningsAsErrors` to true
4. Configure documentation generation

**Validation:**

- [ ] `Directory.Build.props` exists
- [ ] Warnings treated as errors
- [ ] XML documentation generated

**Reference:** [04-build-properties.md](implementation/04-build-properties.md)

### Step 5: Code Style (Day 3)

**Objective:** Enforce consistent code formatting

**Tasks:**

1. Create `.editorconfig`
2. Configure C# style rules
3. Set up code analyzers
4. Configure IDE warnings

**Validation:**

- [ ] `.editorconfig` exists
- [ ] `dotnet format --verify-no-changes` passes
- [ ] Analyzers configured

**Reference:** [05-code-style.md](implementation/05-code-style.md)

### Step 6: Versioning (Day 3-4)

**Objective:** Automate semantic versioning

**Tasks:**

1. Document `GitVersion.yml` configuration
2. Define branching model (Git Flow)
3. Document Conventional Commits rules
4. Define version bump strategy

**Validation:**

- [ ] GitVersion config documented
- [ ] Branching model documented
- [ ] Commit message format defined

**Reference:** [06-versioning.md](implementation/06-versioning.md)

### Step 7: CI/CD Workflow (Day 4-5)

**Objective:** Design automated build and release pipeline

**Tasks:**

1. Document GitHub Actions workflow
2. Define build, test, coverage stages
3. Configure quality gates (≥80% coverage)
4. Plan NuGet publishing strategy

**Validation:**

- [ ] Workflow YAML documented
- [ ] Quality gates defined
- [ ] Publishing strategy documented

**Reference:** [07-cicd-workflow.md](implementation/07-cicd-workflow.md)

### Step 8: Testing Strategy (Day 5)

**Objective:** Define hybrid testing approach

**Tasks:**

1. Document unit test framework (xUnit)
2. Define integration test strategy
3. Plan E2E tests with PostgreSQL
4. Configure code coverage tools

**Validation:**

- [ ] Test frameworks documented
- [ ] Test strategy defined
- [ ] Coverage tools configured

**Reference:** [08-testing-strategy.md](implementation/08-testing-strategy.md)

### Step 9: NuGet Packaging (Day 6)

**Objective:** Configure package metadata

**Tasks:**

1. Define package metadata template
2. Configure SourceLink
3. Plan package versioning
4. Document publishing process

**Validation:**

- [ ] Package metadata defined
- [ ] SourceLink configured
- [ ] Publishing process documented

**Reference:** [09-nuget-packaging.md](implementation/09-nuget-packaging.md)

### Step 10: Dependency Injection (Day 6)

**Objective:** Define DI strategy and patterns

**Tasks:**

1. Document DI container choice (MEDI)
2. Define registration patterns
3. Document decorator pattern implementation
4. Plan service lifetime management

**Validation:**

- [ ] DI strategy documented
- [ ] Registration patterns defined
- [ ] Decorator pattern documented

**Reference:** [10-dependency-injection.md](implementation/10-dependency-injection.md)

## Validation Checklist

Run these commands to validate your Phase 1 setup:

```bash
# Verify SDK version
dotnet --version

# Restore dependencies
dotnet restore

# Build solution
dotnet build --no-restore

# Check code style
dotnet format --verify-no-changes --no-restore

# Run tests (if any exist)
dotnet test --no-build

# Verify package versions
dotnet list package
```

## Common Issues

### Issue: SDK Version Mismatch

**Symptom:** Build fails with SDK not found error

**Solution:**

1. Check `global.json` has correct SDK version
2. Install required SDK from dotnet.microsoft.com
3. Verify with `dotnet --version`

### Issue: Package Restore Fails

**Symptom:** `dotnet restore` errors

**Solution:**

1. Check `Directory.Packages.props` syntax
2. Verify package versions exist on NuGet.org
3. Clear NuGet cache: `dotnet nuget locals all --clear`

### Issue: Code Style Violations

**Symptom:** `dotnet format` reports violations

**Solution:**

1. Run `dotnet format` to auto-fix
2. Review `.editorconfig` rules
3. Update code to match conventions

## Timeline

**Estimated Duration:** 6 days (1 developer week with buffer)

| Day | Focus | Deliverable |
|-----|-------|-------------|
| 1 | Structure & SDK | Solution structure, global.json |
| 2 | Dependencies & Build | Directory.Packages.props, Directory.Build.props |
| 3 | Style & Versioning | .editorconfig, GitVersion docs |
| 4 | CI/CD Design | Workflow documentation |
| 5 | Testing Strategy | Test framework docs |
| 6 | Packaging & DI | NuGet config, DI patterns |

## Success Criteria

Phase 1 is complete when:

- ✅ All 10 implementation guides reviewed
- ✅ All configuration files created/documented
- ✅ Validation checklist passes
- ✅ Documentation peer-reviewed
- ✅ Exit criteria met (see main [phase-1-platform.md](phase-1-platform.md#exit-criteria))

## Next Phase

Once Phase 1 is complete, proceed to:

**[Phase 2: Domain Model](../phase-2-domain/phase-2-domain.md)** - Define domain entities, value objects, and aggregates

## Support

For questions or issues:

- Review implementation guides in `implementation/`
- Check [REFERENCES.md](REFERENCES.md) for external docs
- Consult [ADR-0005](../adrs/ADR-0005-DI-Container-Strategy.md) for DI decisions
