# Phase 1: NuGet Package Configuration

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Define NuGet packaging conventions for the Idevs framework.

## Package Metadata

Each project that ships as NuGet should include:

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

## Package Dependencies

### Best Practices

**✅ Good: Reference abstractions**

```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
```

**❌ Bad: Reference concrete implementations**

```xml
<PackageReference Include="Serilog.AspNetCore" />
```

**✅ Good: Build-time dependencies**

```xml
<PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
```

### Minimize Core Dependencies

Keep `Idevs` package dependency-free (contracts only).

## Package Hierarchy

```text
Idevs (no dependencies)
  ↑
  ├─ Idevs.Application
  ├─ Idevs.Data
  └─ Idevs.Web
       ↑
       └─ Idevs.Data.PostgreSQL
```

## Creating Packages

```bash
# Pack all projects
dotnet pack --configuration Release --output ./packages

# Pack specific project
dotnet pack src/Idevs/Idevs.csproj -c Release -o ./packages
```

## Publishing (Phase 6)

Publishing is automated via CI/CD (see Phase 6: Release).

## Next Steps

- **[Dependency Injection](10-dependency-injection.md)** - DI strategy and decorator pattern

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: Dependency Injection →](10-dependency-injection.md)
