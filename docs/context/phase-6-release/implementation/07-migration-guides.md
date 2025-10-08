# Phase 6: Migration Guides

[← Back to Phase 6 Overview](../phase-6-release.md)

## Overview

Provide clear upgrade paths for users moving between major versions of Idevs. Migration guides help users understand breaking changes, deprecated APIs, and necessary code updates.

## Migration Guide Template

Each major version should have a migration guide in `docs/migrations/`:

```markdown
# Migration Guide: v1.x to v2.0

This guide helps you upgrade from Idevs v1.x to v2.0.

## Breaking Changes

### 1. Command Handler Interface Signature

**Before (v1.x):**
\`\`\`csharp
public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command);
}
\`\`\`

**After (v2.0):**
\`\`\`csharp
public interface ICommandHandler<TCommand>
{
    Task<CommandResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
\`\`\`

**Migration Steps:**
1. Update handler signatures to return `CommandResult`
2. Add `CancellationToken` parameter
3. Wrap existing logic:
   \`\`\`csharp
   // Old code
   public async Task HandleAsync(CreateUserCommand command)
   {
       await _repository.AddAsync(user);
   }
   
   // New code
   public async Task<CommandResult> HandleAsync(CreateUserCommand command, CancellationToken ct = default)
   {
       await _repository.AddAsync(user, ct);
       return CommandResult.Success();
   }
   \`\`\`

### 2. Tenant Context Registration

**Before (v1.x):**
\`\`\`csharp
services.AddTenantContext<AppTenantContext>();
\`\`\`

**After (v2.0):**
\`\`\`csharp
services.AddIdevsTenancy(options => {
    options.TenantContextType = typeof(AppTenantContext);
    options.TenantResolverType = typeof(HeaderTenantResolver);
});
\`\`\`

## Deprecated APIs

| API | Status | Replacement | Removal Version |
|-----|--------|-------------|-----------------|
| `ICommandBus.Send()` | Deprecated | `ICommandBus.SendAsync()` | v3.0 |
| `TenantContext.Current` | Deprecated | `ITenantAccessor.Tenant` | v3.0 |

## New Features

- Outbox pattern support
- Enhanced multi-tenancy options
- Domain event improvements

## Upgrade Checklist

- [ ] Review breaking changes
- [ ] Update handler signatures
- [ ] Update DI registration
- [ ] Run tests
- [ ] Update integration tests
- [ ] Deploy to staging
- [ ] Verify functionality
```

## Version-Specific Guides

### v0.x to v1.0 Migration

Create `docs/migrations/v0-to-v1.md`:

```markdown
# Migration Guide: v0.x (Preview) to v1.0 (Stable)

## Overview

Version 1.0 is the first stable release. This guide covers breaking changes from preview versions.

## Breaking Changes

### Package Renaming

| Old Package | New Package |
|-------------|-------------|
| `Idevs.Core` | `Idevs` |
| `Idevs.Data` | `Idevs.Infrastructure` |

**Migration:**
\`\`\`bash
# Update package references
dotnet remove package Idevs.Core
dotnet add package Idevs

dotnet remove package Idevs.Data
dotnet add package Idevs.Infrastructure
\`\`\`

### Namespace Changes

**Before:**
\`\`\`csharp
using Idevs.Core.Commands;
using Idevs.Core.Queries;
\`\`\`

**After:**
\`\`\`csharp
using Idevs.Application.Commands;
using Idevs.Application.Queries;
\`\`\`

### DI Registration Simplification

**Before:**
\`\`\`csharp
services.AddIdevsCore();
services.AddIdevsCommands(typeof(Program).Assembly);
services.AddIdevsQueries(typeof(Program).Assembly);
\`\`\`

**After:**
\`\`\`csharp
services.AddIdevs(config => {
    config.AddCommandsFromAssembly(typeof(Program).Assembly);
    config.AddQueriesFromAssembly(typeof(Program).Assembly);
});
\`\`\`

## Upgrade Steps

1. **Update packages:**
   \`\`\`bash
   dotnet add package Idevs --version 1.0.0
   dotnet add package Idevs.Infrastructure.PostgreSQL --version 1.0.0
   \`\`\`

2. **Fix namespaces:**
   - Find/replace `Idevs.Core` → `Idevs`
   - Find/replace `Idevs.Data` → `Idevs.Infrastructure`

3. **Update DI registration** in `Program.cs`

4. **Run tests** to identify runtime issues

5. **Update documentation** references
```

## Breaking Change Communication

### CHANGELOG.md Format

Document breaking changes prominently:

```markdown
# Changelog

## [2.0.0] - 2024-06-01

### ⚠️ BREAKING CHANGES

- **Command Handlers:** Return type changed to `CommandResult`. See [migration guide](docs/migrations/v1-to-v2.md).
- **Tenancy:** Registration API redesigned. Update `AddTenantContext` calls.

### Added

- Outbox pattern support via `IOutboxRepository`
- Multi-database tenant isolation strategies

### Changed

- Improved query performance with compiled expressions

### Fixed

- Race condition in tenant resolution
```

## Automated Migration Tools

### Roslyn Analyzer

Create analyzer to detect deprecated API usage:

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IdevsDeprecationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "IDEVS001",
        title: "Deprecated API usage",
        messageFormat: "{0} is deprecated. Use {1} instead.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }

    // ... implementation
}
```

### Code Fixer

Provide automatic fixes where possible:

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class IdevsDeprecationCodeFixProvider : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Update to new API",
                createChangedDocument: c => UpdateApiUsage(context.Document, diagnostic, c),
                equivalenceKey: nameof(IdevsDeprecationCodeFixProvider)
            ),
            diagnostic
        );
    }

    // ... implementation
}
```

## Database Migration Guides

### Entity Framework Migrations

For infrastructure package updates:

```markdown
## Database Schema Changes

### New Tables

- `OutboxMessages` - Stores outbox pattern messages

### Modified Tables

- `Tenants` - Added `IsolationStrategy` column

### Migration Steps

\`\`\`bash
# Add migration
dotnet ef migrations add UpgradeToV2 -p src/Idevs.Infrastructure.PostgreSQL

# Review generated migration
# Edit if necessary

# Apply migration
dotnet ef database update -p src/Idevs.Infrastructure.PostgreSQL
\`\`\`

### Data Migration Scripts

For complex data transformations, provide SQL scripts:

\`\`\`sql
-- Migrate existing tenant data
UPDATE "Tenants"
SET "IsolationStrategy" = 'Schema'
WHERE "IsolationStrategy" IS NULL;
\`\`\`
```

## API Compatibility Matrix

Document compatibility across versions:

| Feature | v1.0 | v1.1 | v2.0 |
|---------|------|------|------|
| Basic CQRS | ✅ | ✅ | ✅ |
| Multi-tenancy | ✅ | ✅ | ✅ (Enhanced) |
| Outbox Pattern | ❌ | ❌ | ✅ |
| Event Sourcing | ❌ | Preview | ✅ |

## Deprecation Policy

1. **Warning:** API marked `[Obsolete]` with message pointing to replacement
2. **Deprecation Period:** Minimum 1 major version (e.g., deprecated in v2.0, removed in v3.0)
3. **Removal:** Only in major version releases

## Version Support

| Version | Status | Support End |
|---------|--------|-------------|
| v2.x | Current | TBD |
| v1.x | Maintenance | 6 months after v2.0 |
| v0.x | Unsupported | - |

## Testing Upgrades

Recommended testing strategy:

```bash
# 1. Create test branch
git checkout -b test-upgrade

# 2. Update packages
dotnet add package Idevs --version 2.0.0

# 3. Build (expect errors)
dotnet build

# 4. Fix compilation errors

# 5. Run full test suite
dotnet test

# 6. Run integration tests
dotnet test tests/Integration

# 7. Deploy to staging environment

# 8. Run smoke tests

# 9. Merge to main branch
```

## Support Resources

- **Migration Issues:** [GitHub Discussions](https://github.com/idevs/idevs-core/discussions)
- **Bug Reports:** [GitHub Issues](https://github.com/idevs/idevs-core/issues)
- **Community Help:** [Stack Overflow tag `idevs`](https://stackoverflow.com/questions/tagged/idevs)

## Next Steps

- **[Performance Benchmarks](08-performance-benchmarks.md)** - Performance regression testing

---

[← Back to Phase 6 Overview](../phase-6-release.md) | [Next: Performance Benchmarks →](08-performance-benchmarks.md)
