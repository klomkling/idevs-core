# Phase 1: Versioning Strategy

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Define semantic versioning strategy using GitVersion and Conventional Commits.

## GitVersion Configuration

**File:** `GitVersion.yml` (documented, implementation in Phase 6)

See [phase-1-platform-ORIGINAL.md](../phase-1-platform-ORIGINAL.md#6-versioning-strategy-gitversion) lines 467-547 for complete YAML configuration.

### Branch Strategy

| Branch | Tag | Version Example |
|--------|-----|-----------------|
| `main` | (none) | `1.2.0` |
| `develop` | `alpha` | `1.2.0-alpha.1` |
| `feature/new-api` | `new-api` | `1.2.0-new-api.1` |
| `release/1.2.0` | `beta` | `1.2.0-beta.1` |
| `hotfix/security` | `beta` | `1.2.1-beta.1` |

## Conventional Commits

**Format:** `<type>(<scope>): <subject>`

### Types

- `feat:` - New feature (minor bump)
- `fix:` - Bug fix (patch bump)
- `docs:` - Documentation only (no bump)
- `chore:` - Maintenance (no bump)
- `refactor:` - Code restructuring (no bump)
- `test:` - Tests (no bump)
- `BREAKING CHANGE:` - Breaking change (major bump)

### Examples

```bash
feat: add tenant isolation middleware
fix: resolve null reference in tenant context
docs: update ADR-0003 with purge policy

# Breaking change
feat!: change ICommandHandler signature

BREAKING CHANGE: ICommandHandler now requires CancellationToken
```

## Versioning Scenarios

### Feature Development

```bash
git checkout develop
git commit -m "feat: add soft delete support"
# GitVersion: 1.2.0-alpha.1
```

### Release

```bash
git checkout -b release/1.2.0
# GitVersion: 1.2.0-beta.1

git commit -m "fix: update validation message"
# GitVersion: 1.2.0-beta.2

git checkout main
git merge release/1.2.0
git tag v1.2.0
# GitVersion: 1.2.0
```

### Hotfix

```bash
git checkout develop
git checkout -b hotfix/security-patch

git commit -m "fix: resolve SQL injection +semver: patch"
# GitVersion: 1.2.1-beta.1

git checkout main
git merge release/1.2.1
git tag v1.2.1
# GitVersion: 1.2.1
```

## Next Steps

- **[CI/CD Workflow](07-cicd-workflow.md)** - Automated pipeline
- **[Testing Strategy](08-testing-strategy.md)** - Test approach

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: CI/CD Workflow →](07-cicd-workflow.md)
