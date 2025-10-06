# Changelog

All notable changes to the Idevs Framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- .NET solution structure with `Idevs` core library and `Idevs.Tests` test project
- Guard class with `NotNull<T>` and `NotNullOrWhiteSpace` methods using .NET 8 helpers
- IIdevsMarker interface for assembly identification
- Comprehensive test suite with 14 tests achieving 100% branch coverage
- Central Package Management via Directory.Packages.props
- Shared build configuration with Directory.Build.props
- Code style enforcement via .editorconfig
- GitVersion configuration for semantic versioning
- CI workflow with build, test, and coverage gating (≥80%)
- Release workflow for GitHub Packages and NuGet.org publishing
- Dependabot configuration for automated dependency updates
- CONTRIBUTING.md with development guidelines
- Phase 0 implementation plan and execution log
- Phase 0 completion summary document

### Changed

- Accepted ADR-0002, ADR-0003, ADR-0004, and ADR-0005 (previously proposed)
- Normalized formatting across all ADRs
- Updated Phase 0 status to Complete in all documentation
- Updated project status in README with Phase 0 deliverables
- Updated context documentation with accurate phase tracking

### Documentation

- Phase 0: ✅ Complete (Discovery & Guardrails)
- Phase 1: Platform Scaffold & Build Infrastructure
- Phase 2: Domain & Contracts
- Phase 3: Application Layer & Execution Pipeline
- Phase 4: Web Adapters & Sync Endpoints
- Phase 5: Infrastructure Extensibility & Persistence
- Phase 6: Documentation, Samples & Release Readiness
- ADR-0001: ✅ Accepted - Tenancy Strategy (PostgreSQL RLS)
- ADR-0002: ✅ Accepted - Audit Logging (EF Core Interceptors)
- ADR-0003: ✅ Accepted - Soft Delete (Query Filters)
- ADR-0004: ✅ Accepted - Release Governance (Git Flow, GitVersion)
- ADR-0005: ✅ Accepted - DI Container Strategy (No Reflection)

---

## [0.0.1] - 2025-10-05

### Added

- Initial commit with comprehensive documentation framework
- Repository structure and Git configuration
- Documentation tree with 40 files
- CI/CD workflow definitions

### Changed

- N/A

### Deprecated

- N/A

### Removed

- N/A

### Fixed

- N/A

### Security

- N/A

---

## Template for Future Releases

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- New features or capabilities

### Changed
- Changes to existing functionality

### Deprecated
- Features that will be removed in upcoming releases

### Removed
- Features that have been removed

### Fixed
- Bug fixes

### Security
- Security improvements or vulnerability fixes
```

---

## Version History

- **[Unreleased]** - Active development on `develop` branch
- **[0.0.1]** - 2025-10-05 - Initial repository setup

---

## Guidelines for Updating This Changelog

1. **Add entries to [Unreleased]** as you work
2. **Group changes** by type: Added, Changed, Deprecated, Removed, Fixed, Security
3. **Use present tense**: "Add feature" not "Added feature"
4. **Link to issues/PRs** when applicable: `(#123)` or `([PR #45])`
5. **Move [Unreleased] to versioned section** when releasing
6. **Follow semantic versioning**:
   - MAJOR: Breaking changes
   - MINOR: New features (backwards compatible)
   - PATCH: Bug fixes (backwards compatible)

### Example Entry Format

```markdown
### Added
- Add `Result<T>` pattern for error handling (#42)
- Add PostgreSQL RLS support for multi-tenancy ([PR #55])
- Add validation behavior decorator

### Fixed
- Fix null reference in tenant context resolution (#67)
- Fix EF Core query filter for soft deletes
```

---

**Maintained by**: idevs.work team  
**Last Updated**: 2025-10-05
