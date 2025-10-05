# Changelog

All notable changes to the Idevs Framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial repository setup with comprehensive documentation
- Complete documentation for all 7 implementation phases (10,000+ lines)
- Architecture Decision Records (ADR-0001 through ADR-0005)
- CI/CD workflows for documentation validation and .NET build checks
- Git Flow branching structure (main, develop)
- Apache License 2.0
- README.md with project overview and badges
- .gitignore for .NET, Rider, VSCode, and Zed
- AGENTS.md with repository guidelines and coding standards

### Documentation
- Phase 0: Discovery & Guardrails
- Phase 1: Platform Scaffold & Build Infrastructure
- Phase 2: Domain & Contracts
- Phase 3: Application Layer & Execution Pipeline
- Phase 4: Web Adapters & Sync Endpoints
- Phase 5: Infrastructure Extensibility & Persistence
- Phase 6: Documentation, Samples & Release Readiness
- ADR-0001: Tenancy Strategy (PostgreSQL RLS)
- ADR-0002: Audit Logging (EF Core Interceptors)
- ADR-0003: Soft Delete (Query Filters)
- ADR-0004: Release Governance (Git Flow, GitVersion)
- ADR-0005: DI Container Strategy (No Reflection)

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
