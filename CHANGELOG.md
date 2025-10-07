# Changelog

All notable changes to the Idevs Framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- TBD

### Changed

- TBD

---

## [0.1.0] - 2025-10-08

### Added

**Phase 1: Platform Foundation**

- 🎯 **Result Pattern** for type-safe error handling without exceptions
  - `Result` and `Result<T>` classes with success/failure semantics
  - Seven semantic error types: Validation, NotFound, Conflict, Unauthorized, Forbidden, Failure, Unexpected
  - `Error` record with code, message, type, and optional details
  - Result combination and deconstruction support

- 📦 **CQRS Abstractions** for command/query separation
  - `ICommand` and `ICommand<TResponse>` marker interfaces
  - `IQuery<TResponse>` marker interface
  - `ICommandHandler<TCommand>` and `ICommandHandler<TCommand, TResponse>` interfaces
  - `IQueryHandler<TQuery, TResponse>` interface
  - All handlers return `Task<Result>` or `Task<Result<T>>`

- 🎭 **Decorator Infrastructure** for cross-cutting concerns
  - `CommandHandlerDecoratorBase<TCommand>` base class
  - `CommandHandlerDecoratorBase<TCommand, TResponse>` base class
  - `QueryHandlerDecoratorBase<TQuery, TResponse>` base class
  - `LoggingCommandHandlerDecorator<TCommand>` - Logs execution with duration
  - `ValidationCommandHandlerDecorator<TCommand>` - FluentValidation integration
  - `MetricsCommandHandlerDecorator<TCommand>` - Performance tracking

- ⚙️ **Core Service Interfaces**
  - `ITenantContext<TTenantId>` - Multi-tenant context abstraction
  - `ICurrentUser<TUserId>` - Current user abstraction
  - `IUnitOfWork` - Transaction management interface
  - `IMetrics` - Application metrics interface
  - Default implementations: `DefaultTenantContext`, `DefaultCurrentUser`, `NoOpMetrics`

- 📦 **Configuration System**
  - `IdevsOptions` - Global framework configuration
  - `DecoratorOptions` - Enable/disable decorators globally or per-handler
  - Options pattern integration via `Microsoft.Extensions.Options`

- 🔌 **Dependency Injection Extensions**
  - `AddIdevs(Action<IdevsOptions>)` - Register framework services
  - `AddCommandHandler<TCommand, THandler>(Action<DecoratorOptions>)` - Register command handlers with decorator pipeline
  - `AddQueryHandler<TQuery, TResponse, THandler>(Action<DecoratorOptions>)` - Register query handlers with decorator pipeline
  - Zero reflection, fully explicit registration
  - Per-handler decorator configuration overrides

- ✅ **Comprehensive Testing**
  - 48 unit tests with 90.1% line coverage, 70% branch coverage
  - Tests for Result pattern, CQRS abstractions, decorators, and DI
  - Integration tests for decorator pipeline composition
  - Test helpers and validators

- 📚 **Comprehensive Documentation**
  - Phase 1 implementation plan and completion summary
  - Architecture decision records
  - DI pipeline and decorator composition notes
  - Testing strategy documentation
  - User guide with quick start and examples
  - Updated root README with Phase 1 status

### Changed

- ⬆️ **Upgraded to .NET 9.0** from .NET 8.0
  - All projects target `net9.0`
  - Updated packages to .NET 9 compatible versions
  - CI workflows updated to use .NET 9 SDK

- 📦 **Package Management**
  - Updated Microsoft.Extensions.* packages to 9.0.0
  - Updated FluentValidation to 11.9.2
  - Replaced Newtonsoft.Json with System.Text.Json (built-in)

- 🔧 **GitVersion Configuration**
  - Added semantic version bumping based on conventional commits
  - Configured initial version to start at 0.1.0

- 📝 **Documentation**
  - Updated project status to show Phase 1 complete
  - Added quick start guide with code examples
  - Updated .NET badge to 9.0

### Removed

- N/A

### Fixed

- MSB4044 build error caused by empty Version parameter in CI (#1)
- Cross-platform compatibility issues with PowerShell commands in CI
- Markdownlint configuration conflicts between repository and workflow
- Multiple markdown formatting issues (1,256 auto-fixed, 84 manually resolved)
- Missing fenced code block language specifiers (MD040)

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
- **[0.1.0]** - 2025-10-08 - Phase 1: Platform Foundation (Result, CQRS, Decorators)
- **[0.0.1]** - 2025-10-05 - Phase 0: Discovery & Guardrails

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
**Last Updated**: 2025-10-08
