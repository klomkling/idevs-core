# GitHub Actions CI/CD Workflows

This directory contains the GitHub Actions workflows for automated validation and quality checks.

## 📋 Workflows

### 1. Documentation Validation (`docs-validation.yml`)

**Triggers:**

- Push to `main`, `develop`, `feature/**`, `release/**` branches (docs changes only)
- Pull requests to `main` or `develop` (docs changes only)

**Jobs:**

#### Markdown Lint

- Validates markdown syntax and formatting
- Uses `markdownlint-cli` with custom configuration
- Allows long lines (MD013), HTML (MD033), and duplicate headings at different levels (MD024)

#### Link Check

- Validates all internal and external links in markdown files
- Uses `markdown-link-check` with retry logic
- Ignores localhost URLs
- 20s timeout with 3 retries on failures

#### Documentation Structure Validation

- **Phase Documents**: Verifies all required phase documents exist
  - Phase 0: Discovery & Guardrails
  - Phase 1: Platform Scaffold & Build Infrastructure  
  - Phase 2: Domain & Contracts
- **ADR Documents**: Checks for at least 5 ADR documents
- **Folder Structure**: Validates phase folder structure per Rule `O0ZNnt24TgX8z0X4uQpRsH`
  - Each phase must have its own folder
  - Each phase document must be in the correct location

#### Documentation Quality Checks

- **TODO/FIXME Detection**: Warns about unresolved markers
- **Document Length**: Validates Phase 2 document is 1,000-3,000 lines
- **References Validation**: Checks for REFERENCES-VALIDATED.md

---

### 2. .NET Build and Validation (`dotnet-build.yml`)

**Triggers:**

- Push to `main`, `develop`, `feature/**`, `release/**` branches (code changes only)
- Pull requests to `main` or `develop` (code changes only)

**Jobs:**

#### Build

- Builds all .NET projects in Debug and Release configurations
- Runs unit tests
- Targets .NET 8.0

#### Reflection Check (ADR-0005)

- **System.Reflection Usage**: Fails if `using System.Reflection` is found
  - Per Rule `XpX1E0XvkNNXHUR3OBEP2A`: "Do not use System.Reflection in DotNet Project if possible"
  - Per ADR-0005: Use explicit registration and source generators instead
- **MediatR Detection**: Fails if MediatR references are found
  - Per CQRS Framework Plan: Use direct handler invocation instead
- **Type Checking**: Warns if excessive `typeof()`/`GetType()` calls (>10)

#### Code Quality

- **C# Version**: Checks for C# 12.0 or latest
- **Nullable Reference Types**: Validates `<Nullable>enable</Nullable>`
- **TreatWarningsAsErrors**: Recommends enabling for stricter quality

#### Multi-Tenancy Validation (ADR-0001)

- **ITenantEntity**: Checks for tenant entity implementations
- **TenantId Properties**: Validates tenant isolation in entities

---

## 🎯 ADR Compliance

### ADR-0001: Tenancy Strategy

- Multi-tenancy check validates `ITenantEntity` usage
- Ensures `TenantId` is present in all multi-tenant entities

### ADR-0002: Audit Logging

- Future: Will validate `IAuditableEntity` implementation
- Future: Will check for EF Core interceptor usage

### ADR-0003: Soft Delete

- Future: Will validate `ISoftDeletableEntity` implementation
- Future: Will check for global query filter usage

### ADR-0004: Release Governance

- Git Flow branch naming convention enforced by workflow triggers
- Conventional Commits validation (future enhancement)

### ADR-0005: DI Container Strategy

- **Critical**: Fails build if `System.Reflection` is used
- Validates no MediatR usage (direct handler invocation required)
- Enforces explicit registration patterns

---

## 🔒 Rule Compliance

### Rule: O0ZNnt24TgX8z0X4uQpRsH
>
> "In the context folder, each implementation plan should have its own folder to facilitate easy extension of each phase."

**Validation:**

- `docs-validation.yml` → `Check Phase Folder Structure` job
- Verifies each phase has dedicated folder: `docs/context/phase-X-name/`
- Ensures phase document is in correct location: `phase-X-name/phase-X-name.md`

### Rule: XpX1E0XvkNNXHUR3OBEP2A
>
> "Do not use System.Reflection in DotNet Project if possible"

**Validation:**

- `dotnet-build.yml` → `Reflection Check` job
- Scans all `.cs` files in `src/` for `using System.Reflection`
- Fails build if reflection imports are found
- Recommends source generators and explicit registration

---

## 🚀 Running Workflows Locally

### Documentation Validation

```bash
# Install dependencies
npm install -g markdownlint-cli markdown-link-check

# Run markdown lint
markdownlint '**/*.md' --ignore node_modules

# Run link check
find . -name '*.md' -not -path './node_modules/*' | while read file; do
  markdown-link-check "$file"
done
```

### .NET Build and Validation

```bash
# Build all projects
dotnet restore
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Check for reflection usage
grep -r "using System.Reflection" src/ --include="*.cs"

# Check for MediatR usage
grep -r "MediatR\|IMediator" src/ --include="*.cs" --include="*.csproj"
```

---

## 📊 Success Criteria

### Documentation Workflow Success

✅ All markdown files pass linting  
✅ All links resolve successfully  
✅ All required phase documents exist  
✅ Phase folder structure is correct  
✅ Document lengths are within acceptable ranges

### Build Workflow Success

✅ All projects build in Debug and Release  
✅ All unit tests pass  
✅ No System.Reflection usage detected  
✅ No MediatR dependencies found  
✅ C# 12 and nullable reference types enabled  
✅ Multi-tenancy patterns properly implemented

---

## 🔄 Continuous Improvement

### Future Enhancements

- [ ] Add code coverage reporting (target: 100% for domain logic)
- [ ] Add mutation testing for domain entities
- [ ] Add conventional commit validation
- [ ] Add automatic changelog generation
- [ ] Add source generator compilation checks
- [ ] Add PostgreSQL integration test validation
- [ ] Add security scanning (SAST/DAST)
- [ ] Add dependency vulnerability scanning

---

## 📝 Notes

- **Phase 2 Focus**: Current workflows emphasize documentation and architectural compliance
- **Phase 3+**: Will add infrastructure, application layer, and integration test validations
- **Performance**: Workflows use job parallelization where possible
- **Caching**: Future optimization will add NuGet package caching

---

## 🆘 Troubleshooting

### Markdown Lint Failures

- Check `.markdownlint.json` configuration
- Common issues: line length (disabled), HTML usage (allowed), heading levels

### Link Check Failures

- Verify internal relative paths are correct
- External links may timeout - check `.markdown-link-check.json` timeout settings
- GitHub rate limiting may affect external link checks

### Reflection Check Failures

- Remove `using System.Reflection` statements
- Use explicit DI registration instead
- Consider source generators for type discovery

### Build Failures

- Ensure .NET 8 SDK is installed
- Run `dotnet restore` before building
- Check for compilation errors in recent changes

---

**Last Updated**: 2025-10-04  
**Phase**: Phase 2 - Domain & Contracts  
**Status**: ✅ Active and Enforced
