# Phase 1: Platform Scaffolding

> **Status:** Planning  
> **Phase Owner:** Platform Engineering Team  
> **Last Updated:** 2025-10-08  
> **Dependencies:** Phase 0 (Discovery & Guardrails)

## Overview

Phase 1 establishes the platform foundation for the Idevs framework by defining build strategy, versioning, CI/CD workflows, packaging conventions, and development tooling. This phase ensures reproducible builds and automated quality gates before implementation begins.

**Key Principle:** *"Automate everything that can be automated"* — establish CI/CD early to prevent technical debt.

## Objectives

### Primary Goals

1. **Build Strategy** - .NET 8.0 LTS configuration, solution structure, project organization
2. **Versioning & Branching** - GitVersion + Conventional Commits, Git Flow model
3. **CI/CD Pipeline** - Automated build, test, coverage, package, and release workflow
4. **Development Tools** - Code analyzers, style guidelines, testing infrastructure

## Implementation Guides

Detailed implementation steps for each component:

### Foundation & Configuration

- **[Solution Structure](implementation/01-solution-structure.md)** - Project organization, naming conventions, dependency rules
- **[SDK & Targeting](implementation/02-sdk-targeting.md)** - global.json, target framework strategy
- **[Package Management](implementation/03-package-management.md)** - Central package management with Directory.Packages.props
- **[Build Properties](implementation/04-build-properties.md)** - Shared MSBuild configuration via Directory.Build.props
- **[Code Style & Analyzers](implementation/05-code-style.md)** - EditorConfig rules, analyzer configuration

### Versioning & Release

- **[Versioning Strategy](implementation/06-versioning.md)** - GitVersion configuration, branching model, conventional commits
- **[CI/CD Workflow](implementation/07-cicd-workflow.md)** - GitHub Actions pipeline design, quality gates
- **[NuGet Packaging](implementation/09-nuget-packaging.md)** - Package metadata, publishing configuration

### Testing & Quality

- **[Testing Strategy](implementation/08-testing-strategy.md)** - Hybrid approach (InMemory + PostgreSQL), test frameworks
- **[Dependency Injection](implementation/10-dependency-injection.md)** - DI strategy, decorator pattern implementation

## Key Deliverables

| Deliverable | Location | Purpose |
|-------------|----------|---------|
| Solution structure | `idevs-core.sln` | Project organization |
| SDK pinning | `global.json` | Reproducible builds |
| Build configuration | `Directory.Build.props` | Shared MSBuild settings |
| Package management | `Directory.Packages.props` | Centralized dependencies |
| Code style | `.editorconfig` | Consistent formatting |
| Versioning config | `GitVersion.yml` | Automated versioning |
| CI/CD workflow | `.github/workflows/` | Automated pipeline |

## Success Metrics

- ✅ All projects build successfully with `dotnet build`
- ✅ Code style enforced via EditorConfig
- ✅ ≥80% code coverage achieved
- ✅ CI/CD pipeline validates every PR
- ✅ NuGet packages generated automatically
- ✅ GitVersion calculates semantic versions correctly

## Exit Criteria

### Must Have (Blocking)

- [ ] Solution structure defined and documented
- [ ] `global.json` with .NET 8 SDK pinned
- [ ] `Directory.Build.props` configured
- [ ] `Directory.Packages.props` with CPM enabled
- [ ] `.editorconfig` with code style rules
- [ ] GitVersion.yml documented
- [ ] CI/CD workflow documented
- [ ] Branching model documented (Git Flow)
- [ ] Conventional Commits guidelines documented

### Should Have (Non-Blocking)

- [ ] Architecture tests project scaffolded
- [ ] Code coverage tooling documented
- [ ] NuGet package metadata templates
- [ ] Local development setup guide

### Nice to Have (Future)

- [ ] Docker Compose for local PostgreSQL
- [ ] Pre-commit hooks for code style
- [ ] GitHub PR/Issue templates
- [ ] Performance benchmarks (BenchmarkDotNet)

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Version conflicts | High | Central package management, lock files |
| Inconsistent builds | High | Pin SDK version, deterministic builds |
| Excessive build time | Medium | Cache NuGet packages, parallelize tests |
| Dependency vulnerabilities | High | Dependabot, regular security audits |

## Dependencies

### Prerequisites

- **Phase 0:** Design principles, ADR-0004 (Release Governance)

### Outputs to Other Phases

- **Phase 2 (Domain):** Solution structure, testing framework
- **Phase 3 (Application):** DI configuration, decorator patterns
- **Phase 4 (Web):** ASP.NET Core setup, middleware configuration
- **Phase 5 (Infrastructure):** EF Core setup, migrations strategy
- **Phase 6 (Release):** CI/CD workflow, NuGet publishing

## Quick Start

For local development setup:

```bash
# Install .NET 8 SDK
dotnet --version  # Verify 8.0.x

# Clone and build
git clone https://github.com/yourorg/idevs-core.git
cd idevs-core
dotnet restore
dotnet build
dotnet test
```

See [Local Development Setup Guide](implementation/01-solution-structure.md#local-setup) for details.

## Reference

- [COMPLETION-GUIDE.md](COMPLETION-GUIDE.md) - Implementation checklist and validation
- [REFERENCES.md](REFERENCES.md) - External documentation and tools

## Next Steps

1. Review [Solution Structure](implementation/01-solution-structure.md) for project layout
2. Set up [SDK & Targeting](implementation/02-sdk-targeting.md) for reproducible builds
3. Configure [Package Management](implementation/03-package-management.md) for centralized dependencies
4. Implement [Code Style](implementation/05-code-style.md) for consistent formatting

---

**Next Phase:** [Phase 2: Domain Model](../phase-2-domain/phase-2-domain.md)
