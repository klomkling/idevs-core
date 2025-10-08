# Phase 6: Contribution Guide

[← Back to Phase 6 Overview](../phase-6-release.md)

## Overview

Establish community contribution guidelines covering code standards, PR process, testing requirements, and communication. This ensures consistent, high-quality contributions.

## CONTRIBUTING.md Template

Create `CONTRIBUTING.md` in repository root:

```markdown
# Contributing to Idevs

Thank you for your interest in contributing to Idevs! This guide will help you get started.

## Code of Conduct

We expect all contributors to follow our [Code of Conduct](CODE_OF_CONDUCT.md). Be respectful, inclusive, and professional.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git
- Your favorite IDE (VS, Rider, or VS Code)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork:
   \`\`\`bash
   git clone https://github.com/YOUR-USERNAME/idevs-core.git
   cd idevs-core
   \`\`\`
3. Add upstream remote:
   \`\`\`bash
   git remote add upstream https://github.com/idevs/idevs-core.git
   \`\`\`

### Build and Test

\`\`\`bash
# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Run tests
dotnet test -c Release
\`\`\`

## Development Workflow

### Branching

- Create feature branches from `develop`:
  \`\`\`bash
  git checkout develop
  git pull upstream develop
  git checkout -b feature/your-feature
  \`\`\`

### Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation only
- `refactor:` Code refactoring
- `test:` Adding tests
- `chore:` Build/tool changes

Examples:
\`\`\`
feat: add IEventStore abstraction
fix: correct tenant resolution in queries
docs: update getting started guide
\`\`\`

### Code Style

- Follow .NET coding conventions
- Use 4-space indentation
- Enable nullable reference types
- Run `dotnet format` before committing

### Testing

- Write tests for all new features
- Maintain ≥80% code coverage
- Use xUnit, Shouldly, and NSubstitute
- Follow naming: `MethodName_StateUnderTest_ExpectedBehavior`

### Pull Requests

1. **Push your branch:**
   \`\`\`bash
   git push origin feature/your-feature
   \`\`\`

2. **Create PR:**
   - Title: Brief summary (follows Conventional Commits)
   - Description: What, why, and how
   - Link related issues

3. **PR Checklist:**
   - [ ] Tests added/updated
   - [ ] Documentation updated
   - [ ] Code formatted (`dotnet format`)
   - [ ] All tests passing
   - [ ] No merge conflicts

4. **Review Process:**
   - At least one maintainer approval required
   - CI must pass
   - Address review feedback promptly

## What to Contribute

### Good First Issues

Look for issues tagged `good-first-issue` or `help-wanted`.

### Feature Requests

Open an issue first to discuss before implementing large features.

### Bug Reports

Include:
- Description
- Steps to reproduce
- Expected vs actual behavior
- Environment (OS, .NET version)

## Project Structure

\`\`\`
idevs-core/
├── src/              # Source projects
├── tests/            # Test projects
├── samples/          # Sample applications
├── docs/             # Documentation
└── benchmarks/       # Performance benchmarks
\`\`\`

## Design Principles

- **CQRS First:** Commands and queries are distinct
- **Multi-Tenant:** Tenant isolation is built-in
- **Clean Architecture:** Clear layer boundaries
- **TDD:** Write tests first
- **Domain-Driven Design:** Rich domain models

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

- Open a [GitHub Discussion](https://github.com/idevs/idevs-core/discussions)
- Check existing [Issues](https://github.com/idevs/idevs-core/issues)
```

## Pull Request Template

Create `.github/PULL_REQUEST_TEMPLATE.md`:

```markdown
## Description

<!-- Provide a brief summary of the changes -->

## Motivation and Context

<!-- Why is this change required? What problem does it solve? -->
<!-- If it fixes an open issue, link to the issue here -->

Fixes # (issue)

## Type of Change

<!-- Mark relevant items with [x] -->

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Refactoring (no functional changes)
- [ ] Performance improvement

## How Has This Been Tested?

<!-- Describe the tests you ran -->

- [ ] Unit tests
- [ ] Integration tests
- [ ] Manual testing

## Checklist

- [ ] My code follows the project's code style
- [ ] I have run `dotnet format` to format my code
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code where necessary
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix/feature works
- [ ] New and existing unit tests pass locally
- [ ] Any dependent changes have been merged and published

## Screenshots (if applicable)

<!-- Add screenshots to help explain your changes -->

## Additional Notes

<!-- Any additional information -->
```

## Issue Templates

### Bug Report Template

Create `.github/ISSUE_TEMPLATE/bug_report.md`:

```markdown
---
name: Bug Report
about: Create a report to help us improve
title: '[BUG] '
labels: bug
assignees: ''
---

## Describe the Bug

<!-- A clear and concise description -->

## To Reproduce

Steps to reproduce:
1. Go to '...'
2. Call method '...'
3. See error

## Expected Behavior

<!-- What you expected to happen -->

## Actual Behavior

<!-- What actually happened -->

## Environment

- OS: [e.g., Windows 11, macOS 14, Ubuntu 22.04]
- .NET Version: [e.g., .NET 8.0]
- Package Version: [e.g., Idevs 1.0.0]

## Stack Trace / Logs

\`\`\`
Paste stack trace or logs here
\`\`\`

## Additional Context

<!-- Screenshots, related issues, etc. -->
```

### Feature Request Template

Create `.github/ISSUE_TEMPLATE/feature_request.md`:

```markdown
---
name: Feature Request
about: Suggest an idea for this project
title: '[FEATURE] '
labels: enhancement
assignees: ''
---

## Problem Statement

<!-- Describe the problem you're trying to solve -->

## Proposed Solution

<!-- Describe your ideal solution -->

## Alternatives Considered

<!-- What other approaches did you consider? -->

## API Design (if applicable)

\`\`\`csharp
// Proposed API usage
\`\`\`

## Additional Context

<!-- Mockups, examples, etc. -->
```

## Code of Conduct

Create `CODE_OF_CONDUCT.md`:

```markdown
# Contributor Covenant Code of Conduct

## Our Pledge

We pledge to make participation in our community a harassment-free experience for everyone.

## Our Standards

Examples of behavior that contributes to a positive environment:

- Using welcoming and inclusive language
- Being respectful of differing viewpoints
- Gracefully accepting constructive criticism
- Focusing on what is best for the community

Examples of unacceptable behavior:

- Trolling, insulting comments, or personal attacks
- Public or private harassment
- Publishing others' private information
- Other conduct which could reasonably be considered inappropriate

## Enforcement

Instances of abusive behavior may be reported to [conduct@idevs.work](mailto:conduct@idevs.work).

## Attribution

This Code of Conduct is adapted from the [Contributor Covenant](https://www.contributor-covenant.org), version 2.1.
```

## Commit Convention Enforcement

Install and configure `commitlint`:

```bash
# Install commitlint
npm install --save-dev @commitlint/cli @commitlint/config-conventional

# Create config
echo "module.exports = {extends: ['@commitlint/config-conventional']}" > commitlint.config.js

# Install husky
npm install --save-dev husky
npx husky install
npx husky add .husky/commit-msg 'npx --no -- commitlint --edit "$1"'
```

Or use `.NET` alternative with `GitHooks.NET`:

```bash
dotnet tool install -g GitHooks.NET
dotnet githooks install
```

## Maintainer Guidelines

### Review Checklist

- [ ] Code follows project conventions
- [ ] Tests cover new code
- [ ] Documentation updated
- [ ] No breaking changes (or properly documented)
- [ ] CI passes
- [ ] Performance acceptable

### Merge Strategy

- Use **squash and merge** for feature branches
- Use **merge commit** for release branches
- Delete branch after merge

### Release Process

See [Release Pipeline](05-release-pipeline.md) for full details.

## Community Communication

- **GitHub Discussions:** General questions and ideas
- **GitHub Issues:** Bug reports and feature requests
- **Pull Requests:** Code contributions

## Recognition

Contributors are recognized in:

- `CONTRIBUTORS.md` file
- Release notes
- Package metadata (for significant contributions)

## Next Steps

- **[Migration Guides](07-migration-guides.md)** - Upgrade documentation
- **[Performance Benchmarks](08-performance-benchmarks.md)** - Performance tracking

---

[← Back to Phase 6 Overview](../phase-6-release.md) | [Next: Migration Guides →](07-migration-guides.md)
