# Idevs Framework

A .NET building-block framework for modern SaaS and ERP applications. It powers first-party solutions—such as multi-store retail management, subscription billing platforms, and accounting/finance suites—and remains open source for the wider community to extend.

**Brand**: `Idevs` (from idevs.work - "I am a developer")  
**Packages**: `Idevs.*` namespace (e.g., `Idevs`, `Idevs.Application`, `Idevs.Data.PostgreSQL`)  
**Repository**: `idevs-core` (GitHub repo and solution name)

# Repository Guidelines

The Idevs framework ships as a C# library published to GitHub Packages and NuGet with GitVersion-managed releases.

## Project Structure & Module Organization

- `src/` hosts class-library projects; mirror each in `tests/` targeting the latest .NET LTS (`net8.0`).
- Keep design briefs in `docs/context/` and leave `bin/`, `obj/`, `.nupkg` out of git.

## Shared Build Configuration

- Use root files `Directory.Build.props` (shared MSBuild settings: nullable, warnings-as-errors, LangVersion) and `Directory.Packages.props` (central package versions; only temporary `VersionOverride` when needed).
- Lean on SDK formatting defaults (`dotnet format`) and avoid `.editorconfig` churn.
- Vet NuGet dependencies for permissive licenses (MIT/BSD/Apache); reject anything unclear before adding to `Directory.Packages.props`.

## Branching Strategy

- Open every PR against `develop` and keep it releasable.
- Merge `develop` into `main` only for reviewed production releases, forward-merging fixes from `develop` to preserve GitVersion history.

## Build & Test Commands

- `dotnet restore` — fetch dependencies.
- `dotnet build -c Release` — compile and surface warnings.
- `dotnet test -c Release` — run suites; add coverage collectors when needed.
- `dotnet pack src/Idevs/Idevs.csproj -c Release -o ./artifacts` — produce `.nupkg`.

## Coding Style, DDD & TDD Practices

- Model features along DDD boundaries (domain, application services, infrastructure).
- Work in a TDD loop (red → green → refactor) and commit once tests pass.
- Prefer modern C# features (primary constructors, collection expressions, pattern matching, required members) and avoid reflection-heavy patterns by leaning on interfaces, generics, or source generators.
- Stick to .NET defaults: 4-space indent, PascalCase types, camelCase locals, nullable references enabled, warnings as errors.
- Build persistence on EF Core with code-first migrations; target PostgreSQL first, keeping provider-specific logic (SQL Server/MySQL) behind interfaces.

## Testing Guidelines

- Use xUnit with Shouldly and NSubstitute; name files `<TypeUnderTest>Tests.cs` and methods `Method_State_Expectation`.
- Keep integration fixtures in `tests/Idevs.IntegrationTests/Fixtures/`, substituting external services with doubles.
- Target ≥80% branch coverage and document intentional gaps.

## Security & Hardening Practices

- Treat security work as first-class: follow OWASP ASVS where applicable and document threat considerations in PRs.
- Never commit secrets; rely on user-secrets, environment variables, or managed secret stores during development and CI.
- Validate and sanitize all inputs at the Application layer; favor FluentValidation rules plus domain invariants.
- Default to secure communication (HTTPS, TLS 1.2+) and encrypt sensitive data at rest when infrastructure adapters support it.
- Keep dependencies patched; run `dotnet list package --outdated` before releases and remediate high-risk advisories promptly.

## Versioning & Release Flow

- Restore tools with `dotnet tool restore`, then preview versions via `dotnet tool run gitversion`.
- Merges into `develop` publish patch packages to GitHub Packages; promoting `develop` → `main` publishes to NuGet.org and tags the release.
- Conventional Commits keep GitVersion accurate.

## Commit & Pull Request Guidelines

- Commit with Conventional prefixes (`feat:`, `fix:`, `chore:`) and add linked issues, `dotnet test` output, plus any GitVersion overrides.
- Require at least one reviewer and passing CI before merging or tagging.

## Agent-Specific Workflow

- Record design decisions and consumption patterns in `docs/context/` for downstream agents.
