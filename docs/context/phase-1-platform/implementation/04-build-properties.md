# Phase 1: Shared Build Properties

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Define common build settings in Directory.Build.props to share MSBuild configuration across all projects.

## Create Directory.Build.props

**File:** `Directory.Build.props` (repository root)

```xml
<Project>
  <!-- Common Properties -->
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
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

## Key Settings

### Nullable Reference Types

**Required:** `<Nullable>enable</Nullable>`

Enforces null safety throughout the codebase.

### Warnings as Errors

**Enforce clean builds:** `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`

Ensures warnings are fixed before merge.

### Deterministic Builds

**Reproducible builds:** `<Deterministic>true</Deterministic>`

Same source code = same binary output (byte-for-byte).

### Source Link

Enables debugging into NuGet packages:

- `<PublishRepositoryUrl>true</PublishRepositoryUrl>`
- `<IncludeSymbols>true</IncludeSymbols>`
- `<SymbolPackageFormat>snupkg</SymbolPackageFormat>`

## How It Works

`Directory.Build.props` is automatically imported by MSBuild into every project file in the directory tree.

### Inheritance Order

```text
1. Directory.Build.props (root)
2. Directory.Build.props (subfolder, if exists)
3. Project.csproj
4. Directory.Build.targets (optional)
```

### Override in Specific Projects

Projects can override settings:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Override: Don't treat warnings as errors in this project -->
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

## Validation

```bash
# Build with verbose output to see properties
dotnet build -v detailed | grep "TreatWarningsAsErrors"

# Verify deterministic builds
dotnet build -p:Deterministic=true
```

## Best Practices

### Do

- ✅ Keep common settings in root `Directory.Build.props`
- ✅ Enable nullable reference types framework-wide
- ✅ Use deterministic builds for reproducibility
- ✅ Include SourceLink for NuGet packages

### Don't

- ❌ Put project-specific settings in shared props
- ❌ Disable warnings globally (use NoWarn sparingly)
- ❌ Override nullable settings per project

## Next Steps

- **[Code Style](05-code-style.md)** - EditorConfig setup
- **[Versioning](06-versioning.md)** - GitVersion strategy
- **[CI/CD Workflow](07-cicd-workflow.md)** - GitHub Actions

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: Code Style →](05-code-style.md)
