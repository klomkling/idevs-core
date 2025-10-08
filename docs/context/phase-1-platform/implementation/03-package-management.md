# Phase 1: Central Package Management

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Configure Directory.Packages.props for centralized dependency management across all projects in the Idevs framework.

## Benefits

- **Single source of truth** for package versions
- **Easier upgrades** - change version once, applies everywhere
- **Prevents version conflicts** across projects
- **Deterministic builds** with transitive pinning
- **Simplified project files** - no version attributes

## Create Directory.Packages.props

**File:** `Directory.Packages.props` (repository root)

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

## Usage in Projects

### Before CPM (Old Way)

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
```

### After CPM (New Way)

```xml
<PackageReference Include="Serilog.AspNetCore" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
```

**No version attribute needed!** Versions come from `Directory.Packages.props`.

## Package Categories

### Core Framework

Microsoft.Extensions.* packages for DI, logging, configuration

### Data Access

Entity Framework Core + PostgreSQL provider

### Web Framework

ASP.NET Core MVC components

### Observability

Serilog for logging, OpenTelemetry for tracing/metrics

### Testing

xUnit, NSubstitute, Shouldly, NetArchTest

## Transitive Pinning

**Enabled with:** `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>`

### What It Does

Pins transitive dependencies to specific versions for deterministic builds.

### Example

If package A depends on package B v2.0, but you need B v2.1:

```xml
<ItemGroup>
  <PackageVersion Include="PackageA" Version="1.0.0" />
  <PackageVersion Include="PackageB" Version="2.1.0" />
</ItemGroup>
```

Package B v2.1.0 will be used even though A wants v2.0.

## Version Upgrade Strategy

### Minor/Patch Updates

Safe to update regularly:

```xml
<!-- Update patch version -->
<PackageVersion Include="Serilog.AspNetCore" Version="8.0.1" />
```

### Major Updates

Requires testing:

1. Create branch: `update/ef-core-9`
2. Update version in `Directory.Packages.props`
3. Run full test suite
4. Fix breaking changes
5. Merge after validation

## Validation

### Verify CPM is Active

```bash
# Build should succeed without version warnings
dotnet build

# List all packages with versions
dotnet list package
```

### Check for Version Conflicts

```bash
# Show transitive dependencies
dotnet list package --include-transitive

# Look for version conflicts (multiple versions of same package)
dotnet list package --include-transitive | grep -i "conflict"
```

## Dependency Security

### Check for Vulnerabilities

```bash
# List vulnerable packages
dotnet list package --vulnerable

# Update vulnerable packages
dotnet list package --vulnerable --include-transitive
```

### GitHub Dependabot

Configure in `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10
```

## Best Practices

### Do

- ✅ Keep all package versions in `Directory.Packages.props`
- ✅ Use consistent versions across all projects
- ✅ Enable transitive pinning for determinism
- ✅ Regular security audits with `dotnet list package --vulnerable`
- ✅ Test major version updates in isolation

### Don't

- ❌ Mix CPM with traditional versioning
- ❌ Use different versions for same package
- ❌ Ignore security advisories
- ❌ Update all packages at once without testing

## Troubleshooting

### Issue: Package Version Not Found

**Symptom:** `error NU1101: Unable to find package`

**Solution:**

1. Check package name spelling
2. Verify version exists on NuGet.org
3. Clear NuGet cache: `dotnet nuget locals all --clear`

### Issue: Version Conflict

**Symptom:** Multiple versions of same package in dependency tree

**Solution:**

1. Add explicit version to `Directory.Packages.props`
2. Use transitive pinning
3. Check with `dotnet list package --include-transitive`

### Issue: CPM Not Applied

**Symptom:** Build still requires version attributes

**Solution:**

1. Verify `Directory.Packages.props` is in repository root
2. Check `ManagePackageVersionsCentrally` is `true`
3. Remove version attributes from project files

## Next Steps

- **[Build Properties](04-build-properties.md)** - Shared MSBuild configuration
- **[Code Style](05-code-style.md)** - EditorConfig setup
- **[Versioning](06-versioning.md)** - GitVersion strategy

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: Build Properties →](04-build-properties.md)
