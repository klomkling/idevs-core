# Phase 1: Platform Scaffolding

**Phase Owner**: Platform Engineering Team  
**Last Updated**: 2025-10-04  
**Status**: Planning  
**Dependencies**: Phase 0 (Discovery & Guardrails)

---

## Purpose

Phase 1 establishes the **platform foundation** for the **Idevs** framework (repo: `idevs-core`) by defining build strategy, versioning, CI/CD workflows, packaging conventions, and development tooling. This phase ensures reproducible builds and automated quality gates before any implementation begins.

**Key Principle**: *"Automate everything that can be automated"* — establish CI/CD early to prevent technical debt.

---

## Objectives

### Primary Objectives

1. **Define Build Strategy**
   - Document .NET 8.0 LTS build configuration
   - Plan forward compatibility path to .NET 10.0 LTS
   - Define multi-targeting strategy (if needed)
   - Document solution structure and project organization

2. **Establish Versioning & Branching**
   - Implement GitVersion + Conventional Commits strategy
   - Define Git Flow branching model
   - Document version bump rules and release tagging
   - Plan hotfix and patch management workflows

3. **Design CI/CD Pipeline**
   - Define workflow stages: build, test, coverage, package, release
   - Document quality gates and thresholds (≥80% coverage)
   - Plan NuGet packaging and publishing strategy
   - Design automated release notes generation

4. **Configure Development Tools**
   - Define code analyzers and style guidelines (EditorConfig, .editorconfig)
   - Plan testing infrastructure (xUnit, Testcontainers)
   - Document local development setup
   - Define dependency management policies

---

## Key Activities

### 1. Solution Structure & Project Organization

**Activity**: Define the logical organization of projects within the framework

#### Recommended Structure

```
idevs-core/
├── src/
│   ├── Idevs/                               # Core package (abstractions + domain)
│   ├── Idevs.Application/                   # CQRS handlers, decorators
│   ├── Idevs.Data/                          # Data access abstractions (EF Core base)
│   ├── Idevs.Data.PostgreSQL/               # PostgreSQL provider + features
│   ├── Idevs.Caching/                       # Caching abstractions
│   ├── Idevs.Caching.Redis/                 # Redis implementation
│   ├── Idevs.Messaging/                     # Outbox/inbox patterns
│   ├── Idevs.Web/                           # ASP.NET Core integration
│   └── Idevs.Web.GraphQL/                   # GraphQL support (optional)
├── tests/
│   ├── Idevs.UnitTests/                     # Unit tests
│   ├── Idevs.IntegrationTests/              # Integration tests (InMemory + PostgreSQL)
│   └── Idevs.ArchitectureTests/             # Architecture validation (NetArchTest)
├── samples/
│   ├── Retail.Sample/                       # Retail management sample
│   ├── Billing.Sample/                      # Subscription billing sample
│   └── Finance.Sample/                      # Accounting/finance sample
├── docs/
│   └── context/                             # Current documentation
├── .github/
│   └── workflows/                           # CI/CD workflows (future)
├── .editorconfig                            # Code style rules
├── Directory.Build.props                    # Shared build properties
├── Directory.Packages.props                 # Central package management (CPM)
├── GitVersion.yml                           # GitVersion configuration (future)
├── global.json                              # .NET SDK version pinning
└── idevs-core.sln                          # Solution file
```

#### Project Naming Conventions

**Pattern**: `Idevs.<Layer>` or `Idevs.<Layer>.<Provider>`

**Core Packages**:

- ✅ `Idevs` - Core abstractions + domain (required by all)
- ✅ `Idevs.Application` - CQRS handlers and application logic
- ✅ `Idevs.Data` - Data access abstractions
- ✅ `Idevs.Web` - ASP.NET Core integration

**Provider/Implementation Packages**:

- ✅ `Idevs.Data.PostgreSQL` - PostgreSQL-specific implementation
- ✅ `Idevs.Caching.Redis` - Redis caching implementation
- ✅ `Idevs.Web.GraphQL` - GraphQL extension

**Anti-patterns** (avoid):

- ❌ `Core.Domain` - Missing vendor prefix
- ❌ `Idevs.Core.Infrastructure.Persistence` - Too verbose (39 chars!)
- ❌ `Idevs.EntityFramework` - Technology-specific naming

#### Dependency Flow Rules

**Principle**: Dependencies flow inward toward the domain

```
Presentation Layer (Web, GraphQL)
    ↓ depends on
Application Layer (Handlers, Decorators)
    ↓ depends on
Domain Layer (Entities, Interfaces)
    ↑ implements
Infrastructure Layer (EF Core, Caching, etc.)
```

**Enforcement**:

- Use `NetArchTest.Rules` to validate dependency rules
- Domain layer has **zero** external dependencies (except primitives)
- Infrastructure depends on domain abstractions, not concrete types

---

### 2. .NET SDK & Target Framework

**Activity**: Define .NET SDK version and target framework

#### SDK Pinning

**File**: `global.json`

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
```

**Rationale**:

- Pin to specific .NET 8 SDK to ensure reproducible builds
- `rollForward: latestMinor` allows patch updates (8.0.1, 8.0.2, etc.)
- Prevents "works on my machine" issues

**Branding Note**: Package names use `Idevs.*` (from your domain: idevs.work)

#### Target Framework

**Current**: `net8.0` (LTS until November 2026)

**Future**: `net10.0` (LTS planned for November 2025)

**Multi-Targeting Strategy** (when needed):

```xml
<PropertyGroup>
  <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
</PropertyGroup>
```

**Decision Criteria for Multi-Targeting**:

- Wait until .NET 10.0 reaches GA (stable release)
- Only multi-target if specific .NET 10 features needed
- Default: single target (`net8.0`) for simplicity

---

### 3. Central Package Management (CPM)

**Activity**: Configure Directory.Packages.props for centralized dependency management

**File**: `Directory.Packages.props`

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core Framework (Microsoft.Extensions.DependencyInjection) -->
    <!-- NOTE: Using MEDI as primary DI container. See ADR-0005 for rationale. -->
    <!-- NO assembly scanning packages (Autofac/Scrutor) - explicit registration only -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    
    <!-- Entity Framework Core (PostgreSQL-first) -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
    
    <!-- ASP.NET Core -->
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Core" Version="8.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Abstractions" Version="8.0.0" />
    
    <!-- Observability -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageVersion Include="OpenTelemetry" Version="1.7.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.9" />
    <PackageVersion Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.7.0-rc.1" />
    
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.6.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
    <PackageVersion Include="Shouldly" Version="4.2.1" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    
    <!-- Code Coverage -->
    <PackageVersion Include="coverlet.collector" Version="6.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>
</Project>
```

**Benefits**:

- Single source of truth for package versions
- Easier dependency upgrades (change version once)
- Prevents version conflicts across projects
- Transitive pinning ensures deterministic builds

**Usage in Projects**:

```xml
<!-- Before (old way): -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />

<!-- After (CPM): -->
<PackageReference Include="Serilog.AspNetCore" />
```

---

### 4. Shared Build Properties

**Activity**: Define common build settings in Directory.Build.props

**File**: `Directory.Build.props`

```xml
<Project>
  <!-- Common Properties -->
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <NoWarn>CS8618</NoWarn> <!-- Nullable warnings for constructors -->
  </PropertyGroup>

  <!-- Package Metadata -->
  <PropertyGroup>
    <Authors>idevs.work</Authors>
    <Company>idevs.work</Company>
    <Product>Idevs Framework</Product>
    <Copyright>Copyright © idevs.work 2025</Copyright>
    <PackageProjectUrl>https://github.com/yourorg/idevs-core</PackageProjectUrl>
    <RepositoryUrl>https://github.com/yourorg/idevs-core</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>idevs;cqrs;ddd;multi-tenant;dotnet;saas;erp</PackageTags>
  </PropertyGroup>

  <!-- Deterministic Builds -->
  <PropertyGroup>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>

  <!-- Code Analysis -->
  <PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <!-- Source Link (for debugging NuGet packages) -->
  <PropertyGroup>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

**Key Settings**:

- **Nullable**: Required (enforce null safety)
- **TreatWarningsAsErrors**: Enforce clean builds in CI
- **Deterministic**: Reproducible builds (same input = same output)
- **Source Link**: Enable debugging into NuGet packages

---

### 5. Code Style & Analyzers

**Activity**: Define consistent code style and static analysis rules

**File**: `.editorconfig`

```ini
# EditorConfig is awesome: https://EditorConfig.org

root = true

# All files
[*]
charset = utf-8
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

# XML files
[*.{xml,csproj,props,targets}]
indent_size = 2

# JSON/YAML files
[*.{json,yml,yaml}]
indent_size = 2

# Markdown files
[*.md]
trim_trailing_whitespace = false

# C# files
[*.cs]

# Organize usings
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# this. preferences
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning

# Language keywords vs BCL types
dotnet_style_predefined_type_for_locals_parameters_members = true:warning
dotnet_style_predefined_type_for_member_access = true:warning

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:silent

# Expression-level preferences
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_prefer_auto_properties = true:suggestion
dotnet_style_prefer_conditional_expression_over_return = true:silent

# Null-checking preferences
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion

# var preferences
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

# Expression-bodied members
csharp_style_expression_bodied_methods = when_on_single_line:silent
csharp_style_expression_bodied_constructors = false:silent
csharp_style_expression_bodied_operators = when_on_single_line:silent
csharp_style_expression_bodied_properties = when_on_single_line:suggestion
csharp_style_expression_bodied_indexers = when_on_single_line:suggestion
csharp_style_expression_bodied_accessors = when_on_single_line:suggestion

# Pattern matching preferences
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion

# Null-checking preferences
csharp_style_throw_expression = true:suggestion
csharp_style_conditional_delegate_call = true:suggestion

# Code block preferences
csharp_prefer_braces = true:warning

# Modifier preferences
csharp_prefer_static_local_function = true:suggestion
csharp_preferred_modifier_order = public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async:suggestion

# Naming conventions

# Constants should be PascalCase
dotnet_naming_rule.constants_should_be_pascal_case.severity = warning
dotnet_naming_rule.constants_should_be_pascal_case.symbols = constants
dotnet_naming_rule.constants_should_be_pascal_case.style = pascal_case_style

dotnet_naming_symbols.constants.applicable_kinds = field
dotnet_naming_symbols.constants.required_modifiers = const

dotnet_naming_style.pascal_case_style.capitalization = pascal_case

# Private fields should be _camelCase
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.severity = warning
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.style = camel_case_underscore_style

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.camel_case_underscore_style.capitalization = camel_case
dotnet_naming_style.camel_case_underscore_style.required_prefix = _

# Interfaces should be IPascalCase
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interfaces
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = i_prefix_pascal_case_style

dotnet_naming_symbols.interfaces.applicable_kinds = interface

dotnet_naming_style.i_prefix_pascal_case_style.capitalization = pascal_case
dotnet_naming_style.i_prefix_pascal_case_style.required_prefix = I

# Code Analysis Rules (selected subset)

# CA1031: Do not catch general exception types
dotnet_diagnostic.CA1031.severity = none

# CA1062: Validate arguments of public methods
dotnet_diagnostic.CA1062.severity = none

# CA2007: Consider calling ConfigureAwait on awaited task
dotnet_diagnostic.CA2007.severity = none

# IDE0058: Expression value is never used
dotnet_diagnostic.IDE0058.severity = none
```

**Custom Analyzers** (future consideration):

- StyleCop.Analyzers (if stricter style enforcement needed)
- Roslynator (additional refactorings and analyzers)
- SonarAnalyzer.CSharp (security and code quality rules)

---

### 6. Versioning Strategy (GitVersion)

**Activity**: Define semantic versioning strategy using GitVersion

**File**: `GitVersion.yml` (documented, not implemented yet)

```yaml
mode: ContinuousDeployment
tag-prefix: '[vV]'
continuous-delivery-fallback-tag: ci
major-version-bump-message: '\+semver:\s?(breaking|major)'
minor-version-bump-message: '\+semver:\s?(feature|minor)'
patch-version-bump-message: '\+semver:\s?(fix|patch)'
no-bump-message: '\+semver:\s?none'
commit-message-incrementing: Enabled

branches:
  main:
    regex: ^main$
    mode: ContinuousDelivery
    tag: ''
    increment: Patch
    prevent-increment-of-merged-branch-version: true
    track-merge-target: false
    tracks-release-branches: false
    is-release-branch: true

  develop:
    regex: ^dev(elop)?(ment)?$
    mode: ContinuousDeployment
    tag: alpha
    increment: Minor
    prevent-increment-of-merged-branch-version: false
    track-merge-target: true
    tracks-release-branches: true
    is-release-branch: false

  feature:
    regex: ^features?[/-]
    mode: ContinuousDeployment
    tag: useBranchName
    increment: Inherit
    prevent-increment-of-merged-branch-version: false
    track-merge-target: false
    tracks-release-branches: false
    is-release-branch: false

  release:
    regex: ^releases?[/-]
    mode: ContinuousDeployment
    tag: beta
    increment: None
    prevent-increment-of-merged-branch-version: true
    track-merge-target: false
    tracks-release-branches: false
    is-release-branch: true

  hotfix:
    regex: ^hotfix(es)?[/-]
    mode: ContinuousDeployment
    tag: beta
    increment: Patch
    prevent-increment-of-merged-branch-version: false
    track-merge-target: false
    tracks-release-branches: false
    is-release-branch: false

  pull-request:
    regex: ^(pull|pull\-requests|pr)[/-]
    mode: ContinuousDeployment
    tag: PullRequest
    increment: Inherit
    prevent-increment-of-merged-branch-version: false
    track-merge-target: false
    tracks-release-branches: false
    is-release-branch: false

ignore:
  sha: []
merge-message-formats: {}
```

#### Versioning Examples

**Scenario 1: Feature Development**

```bash
# On develop branch
git commit -m "feat: add tenant isolation middleware"
# GitVersion: 1.2.0-alpha.1

git commit -m "fix: resolve null reference in tenant context"
# GitVersion: 1.2.0-alpha.2
```

**Scenario 2: Release**

```bash
# Create release branch
git checkout -b release/1.2.0
# GitVersion: 1.2.0-beta.1

# Bug fixes during release
git commit -m "fix: update validation message"
# GitVersion: 1.2.0-beta.2

# Merge to main and tag
git checkout main
git merge release/1.2.0
git tag v1.2.0
# GitVersion: 1.2.0
```

**Scenario 3: Hotfix**

```bash
# Branch from develop (not main!)
git checkout develop
git checkout -b hotfix/security-patch

git commit -m "fix: resolve SQL injection vulnerability +semver: patch"
# GitVersion: 1.2.1-beta.1

# Merge to develop first
git checkout develop
git merge hotfix/security-patch

# Then release to main
git checkout -b release/1.2.1
git checkout main
git merge release/1.2.1
git tag v1.2.1
# GitVersion: 1.2.1
```

#### Conventional Commits Integration

**Format**: `<type>(<scope>): <subject>`

**Types**:

- `feat`: New feature (minor version bump)
- `fix`: Bug fix (patch version bump)
- `docs`: Documentation only (no version bump)
- `chore`: Maintenance (no version bump)
- `refactor`: Code restructuring (no version bump)
- `test`: Test additions/changes (no version bump)
- `perf`: Performance improvements (patch version bump)
- `BREAKING CHANGE`: Breaking API change (major version bump)

**Examples**:

```bash
feat: add soft delete support
fix: resolve tenant filter bypass
docs: update ADR-0003 with purge policy
chore: bump dependency versions
refactor: simplify handler registration

feat!: change ICommandHandler signature
# or
feat: change ICommandHandler signature

BREAKING CHANGE: ICommandHandler now requires CancellationToken
```

---

### 7. CI/CD Workflow Design

**Activity**: Define GitHub Actions workflow stages

**File**: `.github/workflows/ci.yml` (documented, not implemented)

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]
  release:
    types: [published]

env:
  DOTNET_VERSION: '8.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  # Stage 1: Build & Validate
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for GitVersion

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Install GitVersion
        uses: gittools/actions/gitversion/setup@v0
        with:
          versionSpec: '5.x'

      - name: Determine Version
        uses: gittools/actions/gitversion/execute@v0
        with:
          useConfigFile: true

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release /p:Version=${{ env.GitVersion_SemVer }}

      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: build-output
          path: |
            **/bin/Release/
            **/obj/Release/

  # Stage 2: Unit Tests
  test:
    runs-on: ubuntu-latest
    needs: build
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Run unit tests
        run: |
          dotnet test \
            --configuration Release \
            --no-build \
            --logger "trx;LogFileName=test-results.trx" \
            --collect:"XPlat Code Coverage" \
            -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Unit Test Results
          path: '**/test-results.trx'
          reporter: dotnet-trx

  # Stage 3: Integration Tests (Hybrid Strategy)
  integration-test:
    runs-on: ubuntu-latest
    needs: build
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Run integration tests (InMemory)
        run: |
          dotnet test tests/Idevs.IntegrationTests \
            --configuration Release \
            --filter "Category!=RequiresPostgreSQL" \
            --logger "trx;LogFileName=integration-test-results.trx"

      - name: Publish integration test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Integration Test Results
          path: '**/integration-test-results.trx'
          reporter: dotnet-trx

  # Stage 3b: E2E Tests with PostgreSQL (runs on main/develop only)
  e2e-test:
    runs-on: ubuntu-latest
    needs: build
    if: github.ref == 'refs/heads/main' || github.ref == 'refs/heads/develop'
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_DB: idevs_core_test
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Run E2E tests with PostgreSQL
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=idevs_core_test;Username=postgres;Password=postgres"
          USE_REAL_DATABASE: "true"
        run: |
          dotnet test tests/Idevs.IntegrationTests \
            --configuration Release \
            --filter "Category=RequiresPostgreSQL" \
            --logger "trx;LogFileName=e2e-test-results.trx"

      - name: Publish E2E test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: E2E Test Results (PostgreSQL)
          path: '**/e2e-test-results.trx'
          reporter: dotnet-trx

  # Stage 4: Code Coverage
  coverage:
    runs-on: ubuntu-latest
    needs: [test, integration-test]
    # Note: E2E tests don't block coverage (they're optional)
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Generate coverage report
        run: |
          dotnet test \
            --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage

      - name: Install ReportGenerator
        run: dotnet tool install -g dotnet-reportgenerator-globaltool

      - name: Generate HTML report
        run: |
          reportgenerator \
            -reports:"./coverage/**/coverage.opencover.xml" \
            -targetdir:"./coverage-report" \
            -reporttypes:"Html;MarkdownSummaryGithub;JsonSummary"

      - name: Check coverage threshold
        run: |
          COVERAGE=$(jq -r '.summary.lineCoverage' ./coverage-report/Summary.json)
          echo "Coverage: $COVERAGE%"
          if (( $(echo "$COVERAGE < 80" | bc -l) )); then
            echo "❌ Coverage $COVERAGE% is below threshold of 80%"
            exit 1
          else
            echo "✅ Coverage $COVERAGE% meets threshold"
          fi

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/**/coverage.opencover.xml
          fail_ci_if_error: true

  # Stage 5: Architecture Tests
  architecture-test:
    runs-on: ubuntu-latest
    needs: build
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Run architecture tests
        run: dotnet test tests/Idevs.ArchitectureTests --configuration Release

  # Stage 6: Package (only on main/release)
  package:
    runs-on: ubuntu-latest
    needs: [test, integration-test, coverage, architecture-test]
    # Note: E2E tests run in parallel, don't block packaging
    if: github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/heads/release/')
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Determine Version
        uses: gittools/actions/gitversion/execute@v0

      - name: Create NuGet packages
        run: |
          dotnet pack \
            --configuration Release \
            --no-build \
            --output ./packages \
            /p:Version=${{ env.GitVersion_SemVer }} \
            /p:PackageVersion=${{ env.GitVersion_NuGetVersionV2 }}

      - name: Upload packages
        uses: actions/upload-artifact@v4
        with:
          name: nuget-packages
          path: ./packages/*.nupkg

  # Stage 7: Publish (only on tagged releases)
  publish:
    runs-on: ubuntu-latest
    needs: package
    if: github.event_name == 'release' && github.event.action == 'published'
    steps:
      - name: Download packages
        uses: actions/download-artifact@v4
        with:
          name: nuget-packages
          path: ./packages

      - name: Publish to NuGet.org
        run: |
          dotnet nuget push "./packages/*.nupkg" \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate
```

#### Quality Gates

| Gate | Threshold | Action on Failure |
|------|-----------|-------------------|
| Build | Must succeed | Block merge |
| Unit Tests | 100% pass | Block merge |
| Integration Tests | 100% pass | Block merge |
| Code Coverage | ≥80% | Block merge |
| Architecture Tests | 100% pass | Block merge |
| Code Style | Zero warnings | Block merge |

---

### 8. Hybrid Testing Strategy

**Activity**: Define testing approach that balances speed and thoroughness

#### Testing Pyramid

```
           ┌───────────────┐
           │   E2E Tests   │  Small (PostgreSQL, slow, thorough)
           │  (PostgreSQL) │  Run on main/develop only
           └───────────────┘
          ┌─────────────────┐
          │  Integration    │  Medium (InMemory, fast, good coverage)
          │   Tests         │  Run on every PR
          │  (InMemory)     │
          └─────────────────┘
        ┌───────────────────┐
        │    Unit Tests     │   Large (Mocked, very fast)
        │    (Mocked)       │   Run on every commit
        └───────────────────┘
```

#### Test Categories

**1. Unit Tests** (Majority of tests)

- **Purpose**: Test individual components in isolation
- **Database**: None (mocked with NSubstitute)
- **Speed**: Very fast (milliseconds)
- **When**: Every commit, every PR
- **Coverage Target**: ≥80% of business logic

```csharp
public class CreateOrderHandlerTests
{
    [Fact]
    public async Task Handle_ValidOrder_ReturnsSuccess()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();
        var handler = new CreateOrderHandler(repository);
        var command = new CreateOrderCommand { /* ... */ };
        
        // Act
        var result = await handler.HandleAsync(command);
        
        // Assert
        result.IsSuccess.ShouldBeTrue();
        await repository.Received(1).AddAsync(Arg.Any<Order>());
    }
}
```

**2. Integration Tests** (InMemory)

- **Purpose**: Test component interactions, EF Core queries, business workflows
- **Database**: EF Core InMemory provider
- **Speed**: Fast (seconds)
- **When**: Every PR (not marked with `[Trait("Category", "RequiresPostgreSQL")]`)
- **Limitations**:
  - No database constraints
  - No stored procedures
  - No PostgreSQL-specific features
  - Foreign keys not enforced

```csharp
public class OrderRepositoryIntegrationTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _options;
    
    public OrderRepositoryIntegrationTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
    }
    
    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        await using var context = new AppDbContext(_options);
        var repository = new OrderRepository(context);
        var order = new Order { Id = Guid.NewGuid(), /* ... */ };
        await repository.AddAsync(order);
        await context.SaveChangesAsync();
        
        // Act
        var result = await repository.GetByIdAsync(order.Id);
        
        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(order.Id);
    }
}
```

**3. E2E Tests with PostgreSQL** (Selective)

- **Purpose**: Validate PostgreSQL-specific features, constraints, complex queries
- **Database**: Real PostgreSQL (GitHub Actions Service)
- **Speed**: Slower (minutes)
- **When**: Only on `main` and `develop` branches
- **Mark with**: `[Trait("Category", "RequiresPostgreSQL")]`

```csharp
public class PostgreSQLSpecificTests
{
    [Fact]
    [Trait("Category", "RequiresPostgreSQL")]
    public async Task UniqueConstraint_DuplicateEmail_ThrowsException()
    {
        // This test validates actual PostgreSQL unique constraints
        // InMemory provider doesn't enforce these
        
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string required");
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        // Act & Assert
        var user1 = new User { Email = "test@example.com" };
        context.Users.Add(user1);
        await context.SaveChangesAsync();
        
        var user2 = new User { Email = "test@example.com" }; // Duplicate
        context.Users.Add(user2);
        
        await Should.ThrowAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());
    }
    
    [Fact]
    [Trait("Category", "RequiresPostgreSQL")]
    public async Task SoftDelete_GlobalQueryFilter_ExcludesDeletedRecords()
    {
        // Validates PostgreSQL query filters work correctly
        // This is critical for multi-tenant data isolation
    }
}
```

#### Test Selection Strategy

**Use InMemory when**:

- ✅ Testing business logic
- ✅ Testing LINQ queries
- ✅ Testing EF Core change tracking
- ✅ Testing repository patterns
- ✅ Fast feedback needed

**Use PostgreSQL when**:

- ✅ Testing unique constraints
- ✅ Testing foreign key constraints
- ✅ Testing PostgreSQL-specific functions (e.g., `ILIKE`, array operations)
- ✅ Testing migrations
- ✅ Testing RLS (Row-Level Security)
- ✅ Testing complex indexes
- ✅ Validating soft-delete filters in production-like environment

#### CI Pipeline Flow

```
Pull Request (Feature Branch)
├─ Unit Tests (always run) ✅
├─ Integration Tests - InMemory (always run) ✅
├─ Architecture Tests (always run) ✅
└─ Coverage Check ≥80% (always run) ✅

Main/Develop Branch
├─ Unit Tests ✅
├─ Integration Tests - InMemory ✅
├─ E2E Tests - PostgreSQL ✅ (thorough validation)
├─ Architecture Tests ✅
└─ Coverage Check ≥80% ✅
```

**Benefits**:

- ⚡ Fast PR feedback (no PostgreSQL startup time)
- 💰 Minimal CI minutes usage
- 🎯 Thorough validation on important branches
- 🔄 Can run integration tests locally without Docker

#### Local Development

**Option A: InMemory Only (Recommended for most development)**

```bash
dotnet test  # Runs unit + integration tests with InMemory
```

**Option B: With PostgreSQL (For E2E validation)**

```bash
# Start PostgreSQL with Docker Compose
docker-compose up -d postgres

# Run all tests including E2E
export USE_REAL_DATABASE=true
export ConnectionStrings__DefaultConnection="Host=localhost;Database=idevs_core_dev;Username=postgres;Password=postgres"
dotnet test
```

**Docker Compose** (for local PostgreSQL):

```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: idevs_core_dev
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data

volumes:
  postgres-data:
```

---

### 9. NuGet Package Configuration

**Activity**: Define NuGet packaging conventions

#### Package Structure

Each project that ships as NuGet should have:

```xml
<PropertyGroup>
  <PackageId>Idevs</PackageId>
  <Title>Idevs Framework - Core</Title>
  <Description>
    Core abstractions and domain primitives for the Idevs framework.
    Provides base types for CQRS, DDD, and multi-tenant applications.
    From idevs.work - building blocks for modern .NET applications.
  </Description>
  <PackageTags>idevs;cqrs;ddd;domain;multi-tenant;entities;building-blocks</PackageTags>
  <PackageReleaseNotes>See https://github.com/yourorg/idevs-core/releases</PackageReleaseNotes>
  <PackageProjectUrl>https://idevs.work</PackageProjectUrl>
</PropertyGroup>
```

#### Package Dependencies

**Best Practices**:

- Minimize dependencies in `Idevs.Core.Abstractions` (contracts only)
- Reference abstractions, not implementations
- Use `PrivateAssets="All"` for build-time dependencies

```xml
<!-- ✅ Good: Reference abstraction -->
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />

<!-- ❌ Bad: Reference concrete implementation -->
<PackageReference Include="Serilog.AspNetCore" />

<!-- ✅ Good: Build-time dependency -->
<PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
```

---

### 10. Dependency Injection & Decorator Pattern

**Activity**: Define service registration and decorator pattern without assembly scanning

**Principle**: **"Explicit Over Magic"** - No Autofac, no Scrutor, no assembly scanning

**Decision**: See [ADR-0005: DI Container Strategy](../adrs/ADR-0005-DI-Container-Strategy.md)

#### Why Manual Registration?

1. ✅ **Aligns with Design Principles**: Explicit, no reflection, better startup performance
2. ✅ **Zero External Dependencies**: Uses built-in `Microsoft.Extensions.DependencyInjection`
3. ✅ **Future-Proof**: Microsoft guarantees MEDI support
4. ✅ **Debuggable**: Clear call chains, no magic
5. ✅ **Lower Adoption Barrier**: Works with any .NET application out of the box

#### Decorator Pattern Implementation

**Core Abstraction**:

```csharp
// Idevs/Abstractions/ICommandHandler.cs
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

**Decorator Base Class**:

```csharp
// Idevs/Decorators/CommandHandlerDecoratorBase.cs
public abstract class CommandHandlerDecoratorBase<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected readonly ICommandHandler<TCommand> Inner;
    
    protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }
    
    public abstract Task<Result> HandleAsync(
        TCommand command, 
        CancellationToken cancellationToken = default);
}
```

**Example Decorators**:

```csharp
// Idevs/Decorators/LoggingCommandHandler.cs
public class LoggingCommandHandler<TCommand> : CommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger<LoggingCommandHandler<TCommand>> _logger;
    
    public LoggingCommandHandler(
        ICommandHandler<TCommand> inner,
        ILogger<LoggingCommandHandler<TCommand>> logger)
        : base(inner)
    {
        _logger = logger;
    }
    
    public override async Task<Result> HandleAsync(
        TCommand command, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing command {CommandType} with {CommandData}",
            typeof(TCommand).Name,
            command);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await Inner.HandleAsync(command, cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Command {CommandType} completed in {Duration}ms with result {Success}",
                typeof(TCommand).Name,
                stopwatch.ElapsedMilliseconds,
                result.IsSuccess);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(
                ex,
                "Command {CommandType} failed after {Duration}ms",
                typeof(TCommand).Name,
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}

// Idevs/Decorators/ValidationCommandHandler.cs
public class ValidationCommandHandler<TCommand> : CommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    private readonly IValidator<TCommand> _validator;
    
    public ValidationCommandHandler(
        ICommandHandler<TCommand> inner,
        IValidator<TCommand> validator)
        : base(inner)
    {
        _validator = validator;
    }
    
    public override async Task<Result> HandleAsync(
        TCommand command, 
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            return Result.Failure(
                validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }
        
        return await Inner.HandleAsync(command, cancellationToken);
    }
}
```

#### Service Registration Helpers

**Extension Method for Handler Registration**:

```csharp
// Idevs/DependencyInjection/IServiceCollectionExtensions.cs
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers a command handler with decorators (logging, validation, metrics).
    /// Explicit registration - no assembly scanning.
    /// </summary>
    public static IServiceCollection AddCommandHandler<TCommand, THandler>(
        this IServiceCollection services,
        bool addValidation = true,
        bool addLogging = true,
        bool addMetrics = true)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        services.AddScoped<ICommandHandler<TCommand>>(sp =>
        {
            // 1. Create the core handler
            ICommandHandler<TCommand> handler = 
                ActivatorUtilities.CreateInstance<THandler>(sp);
            
            // 2. Apply validation decorator (innermost)
            if (addValidation)
            {
                var validator = sp.GetService<IValidator<TCommand>>();
                if (validator != null)
                {
                    handler = new ValidationCommandHandler<TCommand>(handler, validator);
                }
            }
            
            // 3. Apply logging decorator
            if (addLogging)
            {
                var logger = sp.GetRequiredService<ILogger<LoggingCommandHandler<TCommand>>>();
                handler = new LoggingCommandHandler<TCommand>(handler, logger);
            }
            
            // 4. Apply metrics decorator (outermost)
            if (addMetrics)
            {
                var metrics = sp.GetRequiredService<IMetrics>();
                handler = new MetricsCommandHandler<TCommand>(handler, metrics);
            }
            
            return handler;
        });
        
        return services;
    }
    
    /// <summary>
    /// Registers all core Idevs services.
    /// </summary>
    public static IServiceCollection AddIdevs(
        this IServiceCollection services,
        Action<IdevsOptions>? configure = null)
    {
        var options = new IdevsOptions();
        configure?.Invoke(options);
        
        // Register core services
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        
        // Register metrics
        services.AddSingleton<IMetrics, MetricsCollector>();
        
        return services;
    }
}
```

#### Consumer Usage

**In Consumer Application**:

```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Add Idevs core services
builder.Services.AddIdevs();

// Add FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

// Register handlers explicitly (no assembly scanning!)
builder.Services.AddCommandHandler<CreateOrder, CreateOrderHandler>();
builder.Services.AddCommandHandler<UpdateOrder, UpdateOrderHandler>();
builder.Services.AddCommandHandler<DeleteOrder, DeleteOrderHandler>(addValidation: false);

builder.Services.AddQueryHandler<GetOrder, Order, GetOrderHandler>();
builder.Services.AddQueryHandler<ListOrders, PagedResult<Order>, ListOrdersHandler>();

var app = builder.Build();
```

**Benefits of Explicit Registration**:

1. ✅ **Clear Dependencies**: You see exactly what handlers are registered
2. ✅ **IDE Support**: Navigate to handler with F12/Ctrl+Click
3. ✅ **Compile-Time Safety**: Typos caught at compile time
4. ✅ **Easy to Debug**: Clear call stack, no reflection magic
5. ✅ **Opt-Out Capability**: Can disable decorators per handler
6. ✅ **Zero External Dependencies**: Uses only built-in .NET APIs

#### Alternative: .NET 8 Keyed Services (Optional)

For those who prefer keyed services:

```csharp
public static IServiceCollection AddCommandHandlerWithKey<TCommand, THandler>(
    this IServiceCollection services)
    where TCommand : ICommand
    where THandler : class, ICommandHandler<TCommand>
{
    // Register base handler with a key
    services.AddKeyedScoped<ICommandHandler<TCommand>, THandler>("base");
    
    // Register decorated version as the default
    services.AddScoped<ICommandHandler<TCommand>>(sp =>
    {
        var baseHandler = sp.GetRequiredKeyedService<ICommandHandler<TCommand>>("base");
        var validator = sp.GetService<IValidator<TCommand>>();
        
        ICommandHandler<TCommand> handler = baseHandler;
        
        if (validator != null)
        {
            handler = new ValidationCommandHandler<TCommand>(handler, validator);
        }
        
        handler = new LoggingCommandHandler<TCommand>(
            handler,
            sp.GetRequiredService<ILogger<LoggingCommandHandler<TCommand>>>());
        
        return handler;
    });
    
    return services;
}
```

#### Optional: Autofac Adapter (Future)

For consumers who want Autofac (like your personal apps), provide a separate package:

**Package**: `Idevs.DependencyInjection.Autofac` (optional, shipped separately)

```csharp
// Idevs.DependencyInjection.Autofac/IdevsAutofacModule.cs
public class IdevsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Assembly scanning for convenience (opt-in only!)
        builder.RegisterAssemblyTypes(typeof(ICommandHandler<>).Assembly)
            .Where(t => t.Name.EndsWith("Handler"))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        
        // Decorators
        builder.RegisterGenericDecorator(
            typeof(ValidationCommandHandler<>),
            typeof(ICommandHandler<>));
        
        builder.RegisterGenericDecorator(
            typeof(LoggingCommandHandler<>),
            typeof(ICommandHandler<>));
    }
}
```

**Usage in Consumer App**:

```csharp
// Your personal apps can still use Autofac!
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule<IdevsAutofacModule>();
});
```

#### Summary

| Aspect | Idevs Core Framework | Consumer Applications |
|--------|---------------------|----------------------|
| **DI Container** | MEDI (built-in) | Any (MEDI, Autofac, etc.) |
| **Registration** | Explicit helpers | Consumer's choice |
| **Assembly Scanning** | ❌ Not provided | Consumer's choice (via adapter) |
| **Decorators** | ✅ Manual factory pattern | ✅ Works with any DI |
| **Dependencies** | Zero external | Optional `Idevs.DependencyInjection.Autofac` |

**Design Philosophy**: *"Build for everyone, optimize for you"*

---

## Deliverables

| Deliverable | Status | Owner | Notes |
|-------------|--------|-------|-------|
| Solution structure defined | 🔲 Pending | Platform | Idevs.* naming convention |
| global.json created | 🔲 Pending | Platform | SDK version pinning |
| Directory.Build.props | 🔲 Pending | Platform | Shared build properties |
| Directory.Packages.props | 🔲 Pending | Platform | Central package management |
| .editorconfig | 🔲 Pending | Platform | Code style rules |
| GitVersion.yml | 🔲 Pending | Platform | Versioning strategy |
| CI/CD workflow (documented) | 🔲 Pending | DevOps | GitHub Actions design |
| NuGet package conventions | 🔲 Pending | Platform | Packaging guidelines (Idevs.*) |

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Build Reproducibility | 100% | Same commit = same binaries |
| CI Pipeline Duration | <10 minutes | GitHub Actions metrics |
| Test Execution Time | <2 minutes | CI logs |
| Code Coverage | ≥80% | Coverlet report |
| Dependency Conflicts | Zero | Build warnings |
| Version Correctness | 100% | GitVersion validation |

---

## Risks & Mitigations

### Risk 1: Version Drift

**Impact**: Medium  
**Probability**: Medium  
**Symptom**: Different versions on different branches  
**Mitigation**:

- Enforce GitVersion in CI (no manual version setting)
- Validate version in PR checks
- Document branching model clearly
- Use branch protection rules

### Risk 2: Build Environment Differences

**Impact**: High  
**Probability**: Low  
**Symptom**: "Works on my machine"  
**Mitigation**:

- Pin .NET SDK version in global.json
- Use deterministic builds
- Run CI on every PR
- Document local setup requirements

### Risk 3: Excessive Build Time

**Impact**: Medium  
**Probability**: Medium  
**Symptom**: Slow CI pipeline, developer frustration  
**Mitigation**:

- Cache NuGet packages in CI
- Parallelize test execution
- Run architecture tests separately
- Optimize Testcontainer usage

### Risk 4: Dependency Vulnerabilities

**Impact**: High  
**Probability**: Medium  
**Symptom**: Known CVEs in dependencies  
**Mitigation**:

- Enable Dependabot in GitHub
- Run `dotnet list package --vulnerable` in CI
- Subscribe to security advisories
- Regular dependency updates

---

## Exit Criteria

Phase 1 is **complete** when:

### Must Have (Blocking)

- [ ] Solution structure defined and documented
- [ ] global.json created with .NET 8 SDK pinned
- [ ] Directory.Build.props configured
- [ ] Directory.Packages.props with CPM enabled
- [ ] .editorconfig with code style rules
- [ ] GitVersion.yml documented (not implemented)
- [ ] CI/CD workflow documented (YAML ready for implementation)
- [ ] Branching model documented (Git Flow)
- [ ] Conventional Commits guidelines documented

### Should Have (Non-Blocking)

- [ ] Architecture tests project scaffolded
- [ ] Code coverage tooling documented
- [ ] NuGet package metadata templates
- [ ] Local development setup guide

### Nice to Have (Future)

- [ ] Docker Compose for local PostgreSQL development
- [ ] Pre-commit hooks for code style
- [ ] GitHub PR templates
- [ ] Issue templates
- [ ] Performance benchmarks with BenchmarkDotNet

---

## Tracking Checklist

### Build Configuration

- [ ] global.json created
- [ ] Directory.Build.props configured
- [ ] Directory.Packages.props configured
- [ ] .editorconfig created
- [ ] Solution file structure defined

### Versioning

- [ ] GitVersion.yml documented
- [ ] Conventional Commits guide linked
- [ ] Version bump rules documented
- [ ] Hotfix protocol defined

### CI/CD

- [ ] Workflow stages documented
- [ ] Quality gates defined (≥80% coverage)
- [ ] Test strategy documented
- [ ] Package publishing strategy defined

### Tooling

- [ ] xUnit + Shouldly + NSubstitute documented
- [ ] Hybrid testing strategy documented (InMemory + PostgreSQL E2E)
- [ ] NetArchTest for architecture validation
- [ ] Coverlet for code coverage

---

## Dependencies & Relationships

### Prerequisites

- **Phase 0**: Design principles, ADR-0004 (Release Governance)

### Outputs to Other Phases

- **Phase 2 (Domain)**: Solution structure, testing framework
- **Phase 3 (Application)**: DI configuration, decorator registration patterns
- **Phase 4 (Web)**: ASP.NET Core project setup, middleware configuration
- **Phase 5 (Infrastructure)**: EF Core project setup, migrations strategy
- **Phase 6 (Release)**: CI/CD workflow, NuGet publishing, release notes automation

---

## References

### Internal Documents

- [Phase 0: Discovery](../phase-0-discovery/phase-0-discovery.md)
- [ADR-0004: Release Governance](../adrs/ADR-0004-release-governance.md) (pending)
- [CQRS Framework Plan](../cqrs-framework-plan.md)

### External References

- [GitVersion Documentation](https://gitversion.net/docs/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [EditorConfig](https://editorconfig.org/)
- [GitHub Actions](https://docs.github.com/en/actions)
- [GitHub Actions Services](https://docs.github.com/en/actions/using-containerized-services/about-service-containers)
- [EF Core InMemory Provider](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)

---

## Appendix: Local Development Setup

### Prerequisites

```bash
# Install .NET 8 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# Verify installation
dotnet --version  # Should be 8.0.x

# Install GitVersion (optional, for local version testing)
dotnet tool install --global GitVersion.Tool

# Install ReportGenerator (optional, for local coverage reports)
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### First-Time Setup

```bash
# Clone repository
git clone https://github.com/yourorg/idevs-core.git
cd idevs-core

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Check code style
dotnet format --verify-no-changes
```

### Recommended IDE Setup

**Visual Studio 2022 (v17.8+)**:

- Install "ASP.NET and web development" workload
- Install "EditorConfig Language Service" extension
- Optional: Docker Desktop (for local PostgreSQL)

**JetBrains Rider 2023.3+**:

- Native EditorConfig support
- Built-in GitVersion integration
- Excellent code analysis

**Visual Studio Code**:

- Install C# Dev Kit extension
- Install EditorConfig extension
- Install GitLens extension

---

**Next Phase**: [Phase 2: Domain Model](../phase-2-domain/phase-2-domain.md)

**Document Maintainer**: Platform Engineering Team  
**Last Review Date**: 2025-10-04  
**Next Review Date**: TBD (after Phase 1 implementation)
