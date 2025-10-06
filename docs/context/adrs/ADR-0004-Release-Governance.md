# ADR-0004: Release Governance

**Status**: Accepted  
**Date**: 2025-10-04  
**Decision Date**: 2025-10-05  
**Deciders**: Architecture Team, Platform Lead, DevOps Team  
**Related**: Phase 1 (Platform Scaffold), AGENTS.md Repository Guidelines, Phase 0 (Discovery)

---

## Context

The Idevs framework is a library published to NuGet.org and GitHub Packages with semantic versioning. A robust release governance strategy is essential for:

### Business Requirements

1. **Predictability**: Consumers expect semantic versioning (SemVer 2.0)
   - **Major** (breaking changes): `1.0.0` → `2.0.0`
   - **Minor** (new features): `1.0.0` → `1.1.0`
   - **Patch** (bug fixes): `1.0.0` → `1.0.1`

2. **Automation**: Manual versioning is error-prone
   - Developers forget to bump versions
   - Inconsistent version numbers
   - Missing changelog entries

3. **Traceability**: Every release must be traceable
   - Link commits to releases
   - Generate changelogs automatically
   - Track breaking changes

4. **Quality Gates**: Prevent broken releases
   - All tests must pass
   - Code coverage ≥80%
   - No high/critical vulnerabilities

### From AGENTS.md Repository Guidelines

From [AGENTS.md](../../AGENTS.md):

> **Branching Strategy**
>
> - Open every PR against `develop` and keep it releasable
> - Merge `develop` into `main` only for reviewed production releases
> - Forward-merge fixes from `develop` to preserve GitVersion history
>
> **Versioning & Release Flow**
>
> - Restore tools with `dotnet tool restore`, then preview versions via `dotnet tool run gitversion`
> - Merges into `develop` publish patch packages to GitHub Packages
> - Promoting `develop` → `main` publishes to NuGet.org and tags the release
> - Conventional Commits keep GitVersion accurate

### Challenges

1. **Version Conflicts**: Multiple developers incrementing versions manually
2. **Changelog Maintenance**: Manual changelog editing is tedious
3. **Hotfix Complexity**: Emergency fixes must not disrupt ongoing development
4. **Prerelease Management**: Alpha/beta/rc tags must be consistent
5. **Branch Divergence**: `main` and `develop` can drift out of sync

---

## Decision

**Implement Git Flow branching model with GitVersion for automated semantic versioning and Conventional Commits for changelog generation.**

### Branch Strategy: Git Flow

```text
main (production releases only)
  ↑
  merge for release
  ↑
develop (integration branch)
  ↑
  merge PRs
  ↑
feature/*, hotfix/*, release/*
```text

#### Branch Descriptions

| Branch | Purpose | Protection | Lifespan |
|--------|---------|------------|----------|
| `main` | Production releases | Protected, require PR | Permanent |
| `develop` | Integration, pre-release | Protected, require PR | Permanent |
| `feature/*` | New features | None | Temporary |
| `hotfix/*` | Emergency fixes | None | Temporary |
| `release/*` | Release preparation | None | Temporary |

#### Branch Protection Rules

**Note**: As a solo developer initially, approval requirements are optional. Focus on automated quality gates.

**`main` branch**:

- ✅ Require pull requests (even from solo developer - forces process discipline)
- ✅ Require status checks to pass (CI build, tests, coverage)
- ✅ Require linear history (squash or rebase)
- ⚠️ Require approvals: **0 required** (solo developer)
- 💡 **Future**: When team grows, require 1-2 approvals

**`develop` branch**:

- ✅ Require pull requests for all changes
- ✅ Require status checks to pass (CI build, tests, coverage)
- ✅ Allow squash and rebase merges
- ⚠️ Require approvals: **0 required** (solo developer)

**All branches (feature/*, hotfix/*, release/*)**:

- ✅ All pull requests trigger CI build, tests, and coverage checks
- ✅ CI must pass before merge (even for solo developer)
- ✅ Coverage threshold: ≥80% enforced

### Semantic Versioning with GitVersion

**GitVersion.yml Configuration**:

```yaml
# GitVersion.yml
mode: Mainline
tag-prefix: 'v'
major-version-bump-message: '\+semver:\s?(breaking|major)'
minor-version-bump-message: '\+semver:\s?(feature|minor)'
patch-version-bump-message: '\+semver:\s?(fix|patch)'
no-bump-message: '\+semver:\s?none'
commit-message-incrementing: Enabled

branches:
  main:
    regex: ^master$|^main$
    mode: ContinuousDelivery
    tag: ''
    increment: Patch
    prevent-increment-of-merged-branch-version: true
    track-merge-target: false
    source-branches: ['develop', 'release']
    tracks-release-branches: false
    is-release-branch: true
    is-mainline: true
    pre-release-weight: 55000
  
  develop:
    regex: ^dev(elop)?(ment)?$
    mode: ContinuousDeployment
    tag: 'alpha'
    increment: Minor
    prevent-increment-of-merged-branch-version: false
    track-merge-target: true
    source-branches: []
    tracks-release-branches: true
    is-release-branch: false
    is-mainline: false
    pre-release-weight: 0
  
  feature:
    regex: ^features?[/-](?<BranchName>.+)
    mode: ContinuousDeployment
    tag: 'feat-{BranchName}'
    increment: Minor
    source-branches: ['develop']
    pre-release-weight: 30000
  
  hotfix:
    regex: ^hotfix(es)?[/-](?<BranchName>.+)
    mode: ContinuousDeployment
    tag: 'beta'
    increment: Patch
    source-branches: ['main']
    is-release-branch: false
    pre-release-weight: 40000
  
  release:
    regex: ^releases?[/-](?<BranchName>.+)
    mode: ContinuousDeployment
    tag: 'rc'
    increment: None
    source-branches: ['develop']
    is-release-branch: true
    pre-release-weight: 50000
  
  pull-request:
    regex: ^(pull|pull\-requests|pr)[/-]
    mode: ContinuousDeployment
    tag: 'pr'
    increment: Inherit
    tag-number-pattern: '[/-](?<number>\d+)'
    source-branches: ['develop', 'main', 'feature', 'release', 'hotfix']

ignore:
  sha: []

merge-message-formats:
  Default: Merge '{SourceBranch}' into '{TargetBranch}'
  PullRequest: 'Merge pull request #{PullRequestNumber} from {SourceBranch}'

workflow: GitFlow/v1
```text

**Version Examples** (workflow-based):

| Workflow Step | Branch | Version | Published To |
|---------------|--------|---------|--------------|
| Initial state | `main` | `1.0.0` | NuGet.org |
| Dev work | `develop` → feat: add login | `1.1.0-alpha.1` | GitHub Packages |
| Dev work | `develop` → fix: null ref | `1.1.0-alpha.2` | GitHub Packages |
| Feature branch | `feature/dashboard` | `1.1.0-feat-dashboard.1` | Not published |
| Prepare release | `release/1.1.0` | `1.1.0-rc.1` | GitHub Packages |
| **Release** | `release/1.1.0` → `main` | **`1.1.0`** | **NuGet.org** |
| Hotfix created | `hotfix/cve-fix` (from `main`) | `1.1.1-beta.1` | Not published |
| Hotfix PR created | `hotfix/cve-fix` → `release/1.1.1` | `1.1.1-rc.1` | GitHub Packages |
| **Hotfix Release** | `release/1.1.1` → `main` | **`1.1.1`** | **NuGet.org** |
| Back-merge | `main` → `develop` | `1.2.0-alpha.1` | GitHub Packages |

### Conventional Commits

**Format**: `<type>(<scope>): <description>`

```text
feat(auth): add JWT token refresh
fix(orders): prevent duplicate order creation
docs(readme): update installation instructions
chore(deps): bump EF Core to 8.0.1
test(users): add user creation tests
refactor(validation): extract validation logic
perf(queries): optimize customer lookup query
ci(build): add coverage reporting
```text

#### Commit Types

| Type | Description | Version Bump | Changelog Section |
|------|-------------|--------------|-------------------|
| `feat` | New feature | Minor | ✨ Features |
| `fix` | Bug fix | Patch | 🐛 Bug Fixes |
| `docs` | Documentation only | None | 📝 Documentation |
| `chore` | Maintenance | None | 🔧 Chore |
| `test` | Tests only | None | ✅ Tests |
| `refactor` | Code refactoring | None | ♻️ Refactor |
| `perf` | Performance improvement | Patch | ⚡ Performance |
| `ci` | CI/CD changes | None | 👷 CI/CD |
| `build` | Build system | None | 📦 Build |
| `revert` | Revert previous commit | Patch | ⏪ Reverts |

#### Breaking Changes

```text
feat(api)!: remove deprecated endpoints

BREAKING CHANGE: The `/api/v1/users/legacy` endpoint has been removed.
Migrate to `/api/v2/users` instead.
```text

- Breaking changes **must** include `!` after type/scope
- Breaking changes **must** include `BREAKING CHANGE:` footer
- Breaking changes bump **major** version

#### Commit Message Validation

**`.github/workflows/commitlint.yml`** (if using commitlint):

```yaml
name: Lint Commit Messages

on:
  pull_request:
    types: [opened, synchronize, reopened]

jobs:
  commitlint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      
      - uses: wagoid/commitlint-github-action@v5
```text

### CI/CD Pipeline

#### Stage 1: Build & Test (All Branches)

**`.github/workflows/build.yml`**:

```yaml
name: Build & Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]
    types: [opened, synchronize, reopened]
  # Also trigger on all PRs regardless of target branch
  pull_request_target:
    types: [opened, synchronize, reopened]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Required for GitVersion
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Install GitVersion
        run: dotnet tool restore
      
      - name: Calculate Version
        id: gitversion
        run: |
          dotnet tool run gitversion /output json /showvariable SemVer > version.txt
          echo "semver=$(cat version.txt)" >> $GITHUB_OUTPUT
      
      - name: Restore Dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore /p:Version=${{ steps.gitversion.outputs.semver }}
      
      - name: Run Tests
        run: dotnet test --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
      - name: Generate Coverage Report
        uses: danielpalme/ReportGenerator-GitHub-Action@5
        with:
          reports: '**/coverage.cobertura.xml'
          targetdir: 'coverage'
          reporttypes: 'HtmlInline;Cobertura'
      
      - name: Check Coverage Threshold
        run: |
          COVERAGE=$(grep -oP 'line-rate="\K[^"]+' coverage/Cobertura.xml | head -1)
          COVERAGE_PCT=$(echo "$COVERAGE * 100" | bc)
          echo "Coverage: $COVERAGE_PCT%"
          if (( $(echo "$COVERAGE_PCT < 80" | bc -l) )); then
            echo "❌ Coverage $COVERAGE_PCT% is below 80% threshold"
            exit 1
          fi
          echo "✅ Coverage $COVERAGE_PCT% meets threshold"
      
      - name: Upload Coverage Report
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage/
      
      - name: Comment Coverage on PR
        if: github.event_name == 'pull_request'
        uses: marocchino/sticky-pull-request-comment@v2
        with:
          header: coverage
          path: coverage/Summary.md
```text

#### Stage 2: Package (develop branch)

**`.github/workflows/package-prerelease.yml`**:

```yaml
name: Package Pre-Release

on:
  push:
    branches: [ develop ]

jobs:
  package:
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/develop'
    
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Install GitVersion
        run: dotnet tool restore
      
      - name: Calculate Version
        id: gitversion
        run: |
          dotnet tool run gitversion /output json /showvariable NuGetVersionV2 > version.txt
          echo "nugetversion=$(cat version.txt)" >> $GITHUB_OUTPUT
      
      - name: Build & Pack
        run: |
          dotnet build --configuration Release
          dotnet pack src/Idevs/Idevs.csproj --configuration Release --no-build --output ./artifacts /p:PackageVersion=${{ steps.gitversion.outputs.nugetversion }}
      
      - name: Publish to GitHub Packages
        run: |
          dotnet nuget push ./artifacts/*.nupkg \
            --source https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json \
            --api-key ${{ secrets.GITHUB_TOKEN }} \
            --skip-duplicate
      
      - name: Generate Pre-Release Notes
        run: |
          echo "## Pre-Release ${{ steps.gitversion.outputs.nugetversion }}" > release-notes.md
          echo "" >> release-notes.md
          echo "### Changes since last release:" >> release-notes.md
          git log --oneline --no-merges $(git describe --tags --abbrev=0)..HEAD >> release-notes.md
      
      - name: Create Pre-Release
        uses: softprops/action-gh-release@v1
        with:
          tag_name: v${{ steps.gitversion.outputs.nugetversion }}
          name: Pre-Release v${{ steps.gitversion.outputs.nugetversion }}
          body_path: release-notes.md
          prerelease: true
          files: ./artifacts/*.nupkg
```text

#### Stage 3: Release (main branch)

**`.github/workflows/release.yml`**:

```yaml
name: Release to NuGet

on:
  push:
    branches: [ main ]

jobs:
  release:
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Install GitVersion
        run: dotnet tool restore
      
      - name: Calculate Version
        id: gitversion
        run: |
          dotnet tool run gitversion /output json /showvariable MajorMinorPatch > version.txt
          echo "version=$(cat version.txt)" >> $GITHUB_OUTPUT
      
      - name: Build & Pack
        run: |
          dotnet build --configuration Release
          dotnet pack src/Idevs/Idevs.csproj --configuration Release --no-build --output ./artifacts /p:PackageVersion=${{ steps.gitversion.outputs.version }}
      
      - name: Publish to NuGet.org
        run: |
          dotnet nuget push ./artifacts/*.nupkg \
            --source https://api.nuget.org/v3/index.json \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --skip-duplicate
      
      - name: Generate Changelog
        id: changelog
        run: |
          # Extract commits since last tag
          LAST_TAG=$(git describe --tags --abbrev=0 HEAD^ 2>/dev/null || echo "")
          
          if [ -z "$LAST_TAG" ]; then
            COMMITS=$(git log --oneline --no-merges)
          else
            COMMITS=$(git log --oneline --no-merges $LAST_TAG..HEAD)
          fi
          
          # Parse conventional commits and categorize
          echo "## What's Changed" > changelog.md
          echo "" >> changelog.md
          
          # Features
          FEATURES=$(echo "$COMMITS" | grep -E "^[a-f0-9]+ feat(\(.*\))?:" || true)
          if [ ! -z "$FEATURES" ]; then
            echo "### ✨ Features" >> changelog.md
            echo "$FEATURES" | sed 's/^[a-f0-9]\+ /- /' >> changelog.md
            echo "" >> changelog.md
          fi
          
          # Bug Fixes
          FIXES=$(echo "$COMMITS" | grep -E "^[a-f0-9]+ fix(\(.*\))?:" || true)
          if [ ! -z "$FIXES" ]; then
            echo "### 🐛 Bug Fixes" >> changelog.md
            echo "$FIXES" | sed 's/^[a-f0-9]\+ /- /' >> changelog.md
            echo "" >> changelog.md
          fi
          
          # Breaking Changes
          BREAKING=$(git log --format=%B $LAST_TAG..HEAD | grep -A 10 "BREAKING CHANGE:" || true)
          if [ ! -z "$BREAKING" ]; then
            echo "### ⚠️ BREAKING CHANGES" >> changelog.md
            echo "$BREAKING" >> changelog.md
            echo "" >> changelog.md
          fi
      
      - name: Create Git Tag
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git tag -a v${{ steps.gitversion.outputs.version }} -m "Release v${{ steps.gitversion.outputs.version }}"
          git push origin v${{ steps.gitversion.outputs.version }}
      
      - name: Create GitHub Release
        uses: softprops/action-gh-release@v1
        with:
          tag_name: v${{ steps.gitversion.outputs.version }}
          name: Release v${{ steps.gitversion.outputs.version }}
          body_path: changelog.md
          files: ./artifacts/*.nupkg
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```text

### Hotfix Workflow

**Scenario**: Critical security vulnerability discovered in production (`1.1.0`)

**Important**: Hotfixes should NOT merge directly to `main`. They follow the same quality gates as features.

```bash
# 1. Create hotfix branch from main
git checkout main
git pull origin main
git checkout -b hotfix/security-cve-2024-001

# 2. Fix the issue
# ... make changes ...
git add .
git commit -m "fix(security): patch CVE-2024-001

Resolves SQL injection vulnerability in user query endpoint.

Closes #1234"

# 3. Push hotfix branch
git push origin hotfix/security-cve-2024-001

# 4. Create release branch from hotfix (same as feature releases)
git checkout -b release/1.1.1
git push origin release/1.1.1

# 5. Create PR: release/1.1.1 → main
# CI runs: build, test, coverage
# GitVersion on release/1.1.1 calculates: 1.1.1-rc.1

# 6. After PR approval and merge to main:
# GitVersion calculates: 1.1.1
# CI publishes to NuGet.org

# 7. Back-merge to develop to keep in sync
git checkout develop
git pull origin develop
git merge main
git push origin develop

# 8. Clean up branches
git branch -d hotfix/security-cve-2024-001
git branch -d release/1.1.1
git push origin --delete hotfix/security-cve-2024-001
git push origin --delete release/1.1.1
```text

**GitVersion Behavior**:

- Hotfix branch: `1.1.1-beta.1`
- Release branch from hotfix: `1.1.1-rc.1` (ready for testing)
- After merge to `main`: `1.1.1` (production)
- After back-merge to `develop`: `1.2.0-alpha.1` (continues development)

**Why not merge hotfix directly to main?**

- ✅ Maintains consistent release process (all releases via `release/*` branches)
- ✅ Allows testing and validation with rc version
- ✅ Triggers same CI/CD quality gates
- ✅ Provides rollback point if hotfix has issues
- ✅ Generates proper release notes via release branch

### Dependabot Configuration

**Purpose**: Automatically update dependencies and create PRs for security patches

**`.github/dependabot.yml`**:

```yaml
version: 2
updates:
  # .NET dependencies
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
      timezone: "UTC"
    open-pull-requests-limit: 5
    reviewers:
      - "yourusername"  # Replace with your GitHub username
    labels:
      - "dependencies"
      - "automated"
    commit-message:
      prefix: "chore"
      prefix-development: "chore"
      include: "scope"
    # Group minor and patch updates together
    groups:
      production-dependencies:
        dependency-type: "production"
        update-types:
          - "minor"
          - "patch"
      development-dependencies:
        dependency-type: "development"
        update-types:
          - "minor"
          - "patch"
    # Allow only security and patch updates for production
    allow:
      - dependency-type: "production"
        update-types: ["security", "patch"]
      - dependency-type: "development"
    # Ignore specific packages if needed
    ignore:
      # Example: pin specific major versions
      # - dependency-name: "Microsoft.EntityFrameworkCore"
      #   versions: ["9.x"]  # Stay on EF Core 8.x
  
  # GitHub Actions
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
    open-pull-requests-limit: 3
    labels:
      - "dependencies"
      - "github-actions"
    commit-message:
      prefix: "ci"
      include: "scope"
```text

**Dependabot PR Workflow**:

1. **Dependabot creates PR** → Targets `develop` branch
2. **CI automatically runs** → Build, test, coverage
3. **Solo developer reviews** → Check changelog and compatibility
4. **Merge if CI passes** → Automated or manual merge
5. **New alpha version** → Published to GitHub Packages

**Security Patches** (urgent):

- Dependabot creates PR with `security` label
- CI runs automatically
- Merge immediately if tests pass
- Consider hotfix process if affecting production

**Dependabot Auto-Merge** (optional for solo developer):

```yaml
# .github/workflows/dependabot-auto-merge.yml
name: Dependabot Auto-Merge

on:
  pull_request:
    branches: [ develop ]

jobs:
  auto-merge:
    runs-on: ubuntu-latest
    if: github.actor == 'dependabot[bot]'
    
    steps:
      - name: Dependabot metadata
        id: metadata
        uses: dependabot/fetch-metadata@v1
        with:
          github-token: "${{ secrets.GITHUB_TOKEN }}"
      
      # Auto-merge only patch and minor updates
      - name: Enable auto-merge for Dependabot PRs
        if: |
          steps.metadata.outputs.update-type == 'version-update:semver-patch' ||
          steps.metadata.outputs.update-type == 'version-update:semver-minor'
        run: gh pr merge --auto --squash "$PR_URL"
        env:
          PR_URL: ${{ github.event.pull_request.html_url }}
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```text

---

### Release Workflow (develop → main)

**Scenario**: Ready to release `1.2.0` with accumulated features

```bash
# 1. Create release branch from develop
git checkout develop
git pull origin develop
git checkout -b release/1.2.0

# 2. Final preparations (update CHANGELOG, version docs, etc.)
git commit -m "chore(release): prepare 1.2.0 release"

# 3. Push and create PR to main
git push origin release/1.2.0
# Create PR: release/1.2.0 → main

# 4. After review and merge, GitVersion calculates: 1.2.0
# CI publishes to NuGet.org

# 5. Back-merge to develop
git checkout develop
git merge release/1.2.0
git push origin develop

# 6. Delete release branch
git branch -d release/1.2.0
git push origin --delete release/1.2.0
```text

---

## Consequences

### ✅ Positive Consequences

1. **Predictable Versioning**
   - Automated semantic versioning
   - No manual version bumps
   - Conventional commits drive versioning

2. **Automated Changelogs**
   - Generated from commit messages
   - Consistent formatting
   - Categorized by type (features, fixes, breaking)

3. **Traceability**
   - Every commit linked to version
   - Git tags for every release
   - GitHub releases with artifacts

4. **Quality Gates**
   - CI prevents broken releases
   - Coverage thresholds enforced
   - All tests must pass

5. **Prerelease Management**
   - Alpha builds from `develop`
   - RC builds from `release/*`
   - Beta builds from `hotfix/*`

6. **Branch Isolation**
   - Features developed in isolation
   - Hotfixes don't disrupt features
   - Clean Git history

### ⚠️ Negative Consequences (with Mitigations)

1. **Process Overhead**
   - **Risk**: Developers find branching strategy complex
   - **Mitigation 1**: Document workflows with diagrams
   - **Mitigation 2**: Provide CLI aliases for common operations
   - **Mitigation 3**: Onboarding guide for new contributors
   - **Mitigation 4**: GitHub PR templates with checklist

2. **Commit Message Discipline**
   - **Risk**: Developers forget conventional commit format
   - **Mitigation 1**: Pre-commit hooks validate format
   - **Mitigation 2**: PR template includes commit examples
   - **Mitigation 3**: CI fails on invalid commit messages
   - **Mitigation 4**: Team training on conventional commits

3. **GitVersion Complexity**
   - **Risk**: GitVersion configuration is complex
   - **Mitigation 1**: Use provided `GitVersion.yml` template
   - **Mitigation 2**: Document version calculation logic
   - **Mitigation 3**: Provide `dotnet gitversion` preview command
   - **Mitigation 4**: Test version calculation in PR checks

4. **Merge Conflicts**
   - **Risk**: Frequent conflicts between `main` and `develop`
   - **Mitigation 1**: Regular back-merges after releases
   - **Mitigation 2**: Short-lived feature branches
   - **Mitigation 3**: Automated conflict detection in CI

5. **Release Delays**
   - **Risk**: Waiting for all features to complete before release
   - **Mitigation 1**: Feature flags for incomplete features
   - **Mitigation 2**: Frequent releases (monthly cadence)
   - **Mitigation 3**: Release train model (fixed schedule)

---

## Alternatives Considered

### Alternative 1: GitHub Flow

**Approach**: Single `main` branch with feature branches

```text
main (production)
  ↑
  merge PRs directly
  ↑
feature/*, hotfix/*
```text

**Pros**:

- Simpler than Git Flow
- Fewer branches to manage
- Continuous deployment friendly

**Cons**:

- ❌ No staging environment (develop)
- ❌ Difficult to prepare releases
- ❌ Hotfixes disrupt feature development
- ❌ No prerelease testing

**Verdict**: ❌ Rejected - Need staging/prerelease capabilities

---

### Alternative 2: Trunk-Based Development

**Approach**: All commits to `main`, feature flags for incomplete features

**Pros**:

- Simplest branching model
- Continuous integration
- No long-lived branches

**Cons**:

- ❌ Requires mature feature flag infrastructure
- ❌ High discipline required (no broken commits)
- ❌ Difficult for library releases (vs SaaS apps)
- ❌ No prerelease packages

**Verdict**: ❌ Rejected - Not suitable for library development

---

### Alternative 3: Manual Versioning

**Approach**: Developers manually update version in `.csproj`

**Pros**:

- Simple (no tools needed)
- Full control over versions

**Cons**:

- ❌ Error-prone (forget to bump version)
- ❌ Version conflicts in PRs
- ❌ No automation
- ❌ Inconsistent changelog

**Verdict**: ❌ Rejected - Too error-prone

---

### Alternative 4: CalVer (Calendar Versioning)

**Approach**: Version based on date: `YYYY.MM.MICRO`

**Example**: `2025.01.0`, `2025.01.1`, `2025.02.0`

**Pros**:

- Predictable release schedule
- Easy to see age of release

**Cons**:

- ❌ No semantic meaning (breaking vs minor)
- ❌ Consumers can't determine compatibility
- ❌ Not standard for libraries

**Verdict**: ❌ Rejected - Violates SemVer convention

---

## Implementation Guidance

### 1. Initialize Git Repository

```bash
# Initialize repo
git init
git branch -M main

# Create develop branch
git checkout -b develop

# Initial commit
git add .
git commit -m "chore: initial commit"

# Push both branches
git push -u origin main
git push -u origin develop
```text

### 2. Install GitVersion

```bash
# .config/dotnet-tools.json
dotnet new tool-manifest
dotnet tool install GitVersion.Tool
```text

**`.config/dotnet-tools.json`**:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "gitversion.tool": {
      "version": "5.12.0",
      "commands": ["dotnet-gitversion"]
    }
  }
}
```text

### 3. Add GitVersion Configuration

Copy the `GitVersion.yml` from the Decision section above to repository root.

### 4. Configure Branch Protection

**GitHub Settings → Branches → Branch protection rules**:

For `main`:

- Require pull request reviews (2 approvers)
- Require status checks: `build`, `test`, `coverage`
- Require conversation resolution
- Require linear history

For `develop`:

- Require pull request reviews (1 approver)
- Require status checks: `build`, `test`

### 5. Create GitHub Actions Workflows

Create the three workflow files from the Decision section:

- `.github/workflows/build.yml`
- `.github/workflows/package-prerelease.yml`
- `.github/workflows/release.yml`

### 6. Add NuGet API Key

**GitHub Settings → Secrets and variables → Actions → New repository secret**:

- Name: `NUGET_API_KEY`
- Value: (from nuget.org API keys)

### 7. First Release

```bash
# On develop
git checkout develop
git commit -m "feat: add initial framework contracts"
git push origin develop
# → Creates v1.0.0-alpha.1 prerelease

# Merge to main for v1.0.0
git checkout main
git merge develop
git push origin main
# → Creates v1.0.0 production release
```text

### 8. Developer Workflow

**Creating a Feature**:

```bash
git checkout develop
git pull origin develop
git checkout -b feature/add-user-authentication
# ... make changes ...
git add .
git commit -m "feat(auth): add JWT authentication"
git push origin feature/add-user-authentication
# Create PR: feature/add-user-authentication → develop
```text

**Creating a Hotfix**:

```bash
git checkout main
git pull origin main
git checkout -b hotfix/fix-validation-bug
# ... make fix ...
git add .
git commit -m "fix(validation): null reference in email validator

Fixes #123"
git push origin hotfix/fix-validation-bug
# Create PR: hotfix/fix-validation-bug → main
# After merge, back-merge to develop
```text

---

## Decision Drivers

1. **Predictability**: Consumers rely on semantic versioning
2. **Automation**: Reduce human error in versioning
3. **Traceability**: Link every release to commits
4. **Quality**: Enforce quality gates before release
5. **Flexibility**: Support hotfixes without disrupting development
6. **Transparency**: Automated changelogs for consumers

---

## References

- [AGENTS.md Repository Guidelines](../../AGENTS.md)
- [Phase 1: Platform Scaffold](../phase-1-platform/phase-1-platform.md)
- [GitVersion Documentation](https://gitversion.net/docs/)
- [Conventional Commits Specification](https://www.conventionalcommits.org/)
- [Semantic Versioning 2.0.0](https://semver.org/)
- [Git Flow Workflow](https://nvie.com/posts/a-successful-git-branching-model/)

---

## Approval

- [ ] Architecture Team
- [ ] Platform Lead
- [ ] DevOps Team
- [ ] Product Owner

**Target Approval Date**: 2025-10-10

---

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-04 | 1.0 | Initial draft | Architecture Team |
