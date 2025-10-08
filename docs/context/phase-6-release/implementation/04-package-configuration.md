# Phase 6: Package Configuration

[← Back to Phase 6 Overview](../phase-6-release.md)

## Overview

Configure NuGet packages for the Idevs framework with proper metadata, versioning, and SourceLink support. This ensures packages are discoverable, well-documented, and debuggable.

## Package Structure

```text
packages/
├── Idevs/                              # Core abstractions
├── Idevs.Application/                  # Application layer
├── Idevs.Domain/                       # Domain primitives
├── Idevs.Infrastructure/               # Base infrastructure
├── Idevs.Infrastructure.PostgreSQL/    # PostgreSQL implementation
├── Idevs.Infrastructure.SqlServer/     # SQL Server implementation (future)
├── Idevs.Infrastructure.Redis/         # Redis caching
├── Idevs.Web/                          # ASP.NET Core integration
└── Idevs.Testing/                      # Testing utilities
```

## Package Dependencies

| Package | Dependencies |
|---------|--------------|
| `Idevs` | None (pure abstractions) |
| `Idevs.Application` | `Idevs` |
| `Idevs.Domain` | `Idevs` |
| `Idevs.Infrastructure` | `Idevs`, `Idevs.Domain` |
| `Idevs.Infrastructure.PostgreSQL` | `Idevs.Infrastructure`, `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `Idevs.Infrastructure.Redis` | `Idevs.Infrastructure`, `StackExchange.Redis` |
| `Idevs.Web` | `Idevs.Application`, `Microsoft.AspNetCore.Mvc` |
| `Idevs.Testing` | `Idevs`, `xUnit`, `NSubstitute`, `Shouldly` |

## .csproj Configuration

Standard package configuration for all Idevs packages:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Target Framework -->
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Package Metadata -->
    <PackageId>Idevs</PackageId>
    <Version>1.0.0</Version>
    <Authors>Idevs Team</Authors>
    <Company>idevs.work</Company>
    <Description>
      Core abstractions for the Idevs framework - a CQRS-centric,
      multi-tenant building block library for modern .NET applications.
    </Description>
    <PackageTags>cqrs;ddd;multi-tenant;framework;clean-architecture</PackageTags>
    <PackageProjectUrl>https://github.com/idevs/idevs-core</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <RepositoryUrl>https://github.com/idevs/idevs-core</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>

    <!-- SourceLink -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <None Include="..\\..\\docs\\images\\icon.png" Pack="true" PackagePath="\\" />
    <None Include="..\\..\\README.md" Pack="true" PackagePath="\\" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>

</Project>
```

## GitVersion Configuration

Semantic versioning based on branch and commit history:

```yaml
# GitVersion.yml
mode: ContinuousDelivery

branches:
  main:
    regex: ^main$
    tag: ''
    increment: Patch
  
  develop:
    regex: ^dev(elop)?(ment)?$
    tag: beta
    increment: Minor
  
  feature:
    regex: ^features?[/-]
    tag: alpha.{BranchName}
    increment: Minor
  
  release:
    regex: ^releases?[/-]
    tag: rc
    increment: None
  
  hotfix:
    regex: ^hotfix(es)?[/-]
    tag: ''
    increment: Patch

ignore:
  sha: []

merge-message-formats: {}
```

## Version Strategy

| Branch | Example Version | Use Case |
|--------|----------------|----------|
| `main` | `1.0.0` | Production releases |
| `develop` | `1.1.0-beta.1` | Pre-release testing |
| `feature/new-api` | `1.1.0-alpha.new-api.1` | Feature development |
| `release/1.0` | `1.0.0-rc.1` | Release candidates |
| `hotfix/bug-fix` | `1.0.1` | Critical fixes |

## Package Icon

Create a 128x128 PNG icon at `docs/images/icon.png`:

- Use brand colors
- Simple, recognizable design
- High contrast for visibility
- Works at small sizes

## Package README

Each package should include a `README.md`:

```markdown
# Idevs

Core abstractions for the Idevs framework.

## Installation

\`\`\`bash
dotnet add package Idevs
\`\`\`

## Quick Start

\`\`\`csharp
// Basic usage example
\`\`\`

## Documentation

- [Getting Started](https://github.com/idevs/idevs-core/docs/getting-started.md)
- [API Reference](https://idevs.work/api/)
- [Samples](https://github.com/idevs/idevs-core/tree/main/samples)

## License

MIT License - see [LICENSE](LICENSE) for details.
```

## NuGet.org Publishing

### Prerequisites

1. Create account on [NuGet.org](https://www.nuget.org)
2. Generate API key
3. Add to GitHub Secrets as `NUGET_API_KEY`

### Manual Publishing

```bash
# Build and pack
dotnet pack src/Idevs/Idevs.csproj -c Release -o ./artifacts

# Push to NuGet.org
dotnet nuget push ./artifacts/Idevs.1.0.0.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### Automated Publishing

Handled by GitHub Actions (see [Release Pipeline](05-release-pipeline.md)).

## SourceLink Verification

Verify SourceLink is working:

```bash
# Install tool
dotnet tool install --global sourcelink

# Test package
sourcelink test ./artifacts/Idevs.1.0.0.nupkg
```

## Package Validation

Use NuGet Package Explorer to inspect packages:

- Metadata is correct
- Dependencies are accurate
- Icon displays properly
- README is included
- Symbols are embedded
- SourceLink data present

## Central Package Management

Use `Directory.Packages.props` for version management:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Framework packages -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageVersion Include="FluentValidation" Version="11.9.2" />
    
    <!-- Testing packages -->
    <PackageVersion Include="xunit" Version="2.6.0" />
    <PackageVersion Include="Shouldly" Version="4.2.1" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
  </ItemGroup>
</Project>
```

## Pre-release Packages

Test pre-release versions before stable release:

```bash
# Create beta package
dotnet pack -c Release /p:VersionSuffix=beta1

# Install pre-release
dotnet add package Idevs --version 1.0.0-beta1 --prerelease
```

## Next Steps

- **[Release Pipeline](05-release-pipeline.md)** - Automate publishing
- **[Contribution Guide](06-contribution-guide.md)** - Community contributions
- **[Migration Guides](07-migration-guides.md)** - Version upgrades

---

[← Back to Phase 6 Overview](../phase-6-release.md) | [Next: Release Pipeline →](05-release-pipeline.md)
