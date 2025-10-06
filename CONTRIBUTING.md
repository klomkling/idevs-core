# Contributing to Idevs Framework

Thank you for your interest in contributing to the **Idevs** framework! This document provides guidelines and best practices for contributing to this project.

## 📋 Table of Contents

- [Development Workflow](#development-workflow)
- [Code Standards](#code-standards)
- [Build and Test](#build-and-test)
- [Pull Request Process](#pull-request-process)
- [Architecture Decision Records](#architecture-decision-records)

---

## Development Workflow

### Git Flow Branching Strategy

We follow **Git Flow** with `main` and `develop` as permanent branches:

```
main (production releases only)
  ↑
develop (integration branch)
  ↑
feature/*, hotfix/*, release/*
```

#### Creating a Feature Branch

```bash
# Ensure you're on the latest develop
git checkout develop
git pull origin develop

# Create your feature branch
git checkout -b feature/your-feature-name

# Work on your feature...
git add .
git commit -m "feat: add your feature description"

# Push and create PR
git push -u origin feature/your-feature-name
```

**Branch Naming Conventions**:
- `feature/` - New features or enhancements
- `hotfix/` - Emergency bug fixes
- `release/` - Release preparation branches
- `docs/` - Documentation-only changes

### Conventional Commits

All commits **must** follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `chore`: Maintenance tasks (dependencies, tooling)
- `test`: Adding or updating tests
- `refactor`: Code changes that neither fix bugs nor add features
- `perf`: Performance improvements
- `ci`: CI/CD changes
- `build`: Build system changes

**Examples**:
```bash
feat(core): add Guard class for input validation
fix(data): resolve soft-delete query filter issue
docs: update README with quick start guide
test(core): add comprehensive Guard tests
chore: bump Microsoft.Extensions.DependencyInjection to 8.0.1
```

**Breaking Changes**:
```bash
feat!: change ICommandHandler signature

BREAKING CHANGE: ICommandHandler now requires CancellationToken parameter
```

### Test-Driven Development (TDD)

We follow the **Red → Green → Refactor** cycle:

1. **Red**: Write a failing test first
2. **Green**: Write minimum code to make the test pass
3. **Refactor**: Improve code while keeping tests green

**Coverage Requirements**:
- **≥80% branch coverage** enforced in CI
- Tests must be added for all new features
- Bug fixes should include regression tests

---

## Code Standards

### Target Framework

- **Primary**: .NET 8.0 LTS (`net8.0`)
- **SDK**: .NET 9.x SDK (for tooling, but target remains `net8.0`)

### C# Language Features

**Prefer Modern C# Features**:
- ✅ Primary constructors
- ✅ Collection expressions (`[]`, `[item1, item2]`)
- ✅ Pattern matching (`is`, `switch` expressions)
- ✅ Required members (`required` keyword)
- ✅ Init-only properties (`init` accessor)
- ✅ Records for immutable data

**Avoid**:
- ❌ `System.Reflection` (use explicit registration or source generators)
- ❌ Magic strings and convention-based discovery
- ❌ Assembly scanning for service registration

### Code Style

- **Indentation**: 4 spaces (SDK default)
- **Naming**:
  - PascalCase for types, methods, properties
  - camelCase for local variables, parameters
  - `_camelCase` for private fields
- **Nullability**: Enabled (nullable reference types)
- **Warnings**: Treated as errors
- **Analyzers**: Enabled at latest level

### Central Package Management

All package versions are managed in `Directory.Packages.props`. **Do not** add `Version` attributes to `<PackageReference>` elements.

```xml
<!-- ❌ Wrong -->
<PackageReference Include="Shouldly" Version="4.2.1" />

<!-- ✅ Correct -->
<PackageReference Include="Shouldly" />
```

To add a new package:

1. Add the version to `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="NewPackage" Version="1.2.3" />
   ```

2. Reference it in your project without version:
   ```xml
   <PackageReference Include="NewPackage" />
   ```

---

## Build and Test

### Prerequisites

- **.NET SDK 9.x** (installs `net8.0` targeting support)
- **Git** 2.30+
- **PostgreSQL 15+** (for integration tests, optional for unit tests)
- **Docker** (optional, for Testcontainers)

### Local Development Commands

```bash
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release with CI flags)
dotnet build -c Release -p:ContinuousIntegrationBuild=true

# Run tests
dotnet test

# Run tests with coverage
dotnet test -c Release \
  -p:CollectCoverage=true \
  -p:CoverletOutputFormat=opencover \
  -p:Threshold=80 \
  -p:ThresholdType=branch \
  -p:ThresholdStat=total

# Pack NuGet packages
dotnet pack -c Release -o ./artifacts/packages

# Check GitVersion (requires: dotnet tool restore)
dotnet tool restore
dotnet gitversion
```

### Running Specific Tests

```bash
# Run tests in a specific project
dotnet test tests/Idevs.Tests/

# Run tests matching a filter
dotnet test --filter "FullyQualifiedName~Guard"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

---

## Pull Request Process

### Before Opening a PR

1. **Sync with develop**:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout your-feature-branch
   git rebase develop
   ```

2. **Run local checks**:
   ```bash
   dotnet restore
   dotnet build
   dotnet test
   dotnet format --verify-no-changes
   ```

3. **Ensure coverage meets threshold** (≥80%)

4. **Update documentation** (if applicable):
   - README.md for user-facing changes
   - XML comments for public APIs
   - ADRs for design decisions
   - Context docs for significant changes

### PR Checklist

When opening a PR, ensure:

- [ ] **Title** follows Conventional Commits format
- [ ] **Description** clearly explains the change and motivation
- [ ] **Tests** are added for new functionality or bug fixes
- [ ] **Coverage** meets ≥80% branch coverage threshold
- [ ] **Documentation** updated (README, ADRs, XML comments)
- [ ] **Build** passes (CI will verify)
- [ ] **No merge conflicts** with `develop`
- [ ] **Commits** are clean and follow Conventional Commits
- [ ] **ADRs** referenced (if design decisions were made)

### PR Template

```markdown
## Description
Brief summary of what this PR accomplishes.

## Type of Change
- [ ] feat: New feature
- [ ] fix: Bug fix
- [ ] docs: Documentation update
- [ ] refactor: Code refactoring
- [ ] test: Test additions/updates
- [ ] chore: Maintenance

## Related Issues
Closes #123

## Testing
Describe how this was tested:
- Unit tests added for X
- Manual testing performed: Y

## Checklist
- [ ] Tests pass locally
- [ ] Coverage ≥80%
- [ ] Documentation updated
- [ ] Follows code standards
```

### Review Process

- All PRs **must** target the `develop` branch
- CI checks must pass (build, tests, coverage)
- For solo developer: Self-review with checklist
- For team: At least one approval required

### Merging

- **Squash and merge** preferred for feature branches
- **Create merge commit** for release branches
- Delete source branch after merge

---

## Architecture Decision Records

Significant design decisions must be documented as ADRs in `docs/context/adrs/`.

### When to Create an ADR

Create an ADR when:
- Making a significant architectural choice
- Choosing between multiple approaches
- Establishing a new pattern or convention
- Deviating from existing patterns

### ADR Format

```markdown
# ADR-NNNN: Title

**Status**: Proposed | Accepted | Deprecated | Superseded  
**Date**: YYYY-MM-DD  
**Decision Date**: YYYY-MM-DD (when accepted)  
**Deciders**: List of people involved  
**Related**: Links to related ADRs, phases, docs

## Context
What is the issue we're seeing that is motivating this decision or change?

## Decision
What is the change that we're proposing and/or doing?

## Consequences
What becomes easier or more difficult to do because of this change?

### Positive
- List benefits

### Negative
- List drawbacks and trade-offs

## Alternatives Considered
What other options were evaluated?

## References
- Links to relevant docs, discussions, or external resources
```

---

## Additional Resources

For deeper coding standards and repository-specific rules, see:

- **[AGENTS.md](./AGENTS.md)** - Detailed repository guidelines
- **[docs/context/](./docs/context/)** - Architecture documentation
- **[docs/context/glossary.md](./docs/context/glossary.md)** - Terminology reference

---

## Questions or Need Help?

- Open a [GitHub Discussion](https://github.com/yourusername/warp-idevs-core/discussions)
- Check existing [GitHub Issues](https://github.com/yourusername/warp-idevs-core/issues)
- Review [Phase Documentation](./docs/context/)

---

**Thank you for contributing to Idevs! 🚀**
