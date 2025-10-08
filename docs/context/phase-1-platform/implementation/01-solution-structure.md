# Phase 1: Solution Structure & Project Organization

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Define the logical organization of projects within the Idevs framework, establishing clear naming conventions and dependency flow rules.

## Recommended Structure

```text
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
│   └── workflows/                           # CI/CD workflows
├── .editorconfig                            # Code style rules
├── Directory.Build.props                    # Shared build properties
├── Directory.Packages.props                 # Central package management (CPM)
├── GitVersion.yml                           # GitVersion configuration
├── global.json                              # .NET SDK version pinning
└── idevs-core.sln                          # Solution file
```

## Project Naming Conventions

### Pattern

`Idevs.<Layer>` or `Idevs.<Layer>.<Provider>`

### Core Packages

- ✅ `Idevs` - Core abstractions + domain (required by all)
- ✅ `Idevs.Application` - CQRS handlers and application logic
- ✅ `Idevs.Data` - Data access abstractions
- ✅ `Idevs.Web` - ASP.NET Core integration

### Provider/Implementation Packages

- ✅ `Idevs.Data.PostgreSQL` - PostgreSQL-specific implementation
- ✅ `Idevs.Caching.Redis` - Redis caching implementation
- ✅ `Idevs.Web.GraphQL` - GraphQL extension

### Anti-patterns (Avoid)

- ❌ `Core.Domain` - Missing vendor prefix
- ❌ `Idevs.Core.Infrastructure.Persistence` - Too verbose (39 chars!)
- ❌ `Idevs.EntityFramework` - Technology-specific naming

**Rationale:** Package names use `Idevs.*` prefix (from idevs.work domain) for clear branding and namespace collision avoidance.

## Dependency Flow Rules

### Principle

Dependencies flow inward toward the domain:

```text
Presentation Layer (Web, GraphQL)
    ↓ depends on
Application Layer (Handlers, Decorators)
    ↓ depends on
Domain Layer (Entities, Interfaces)
    ↑ implements
Infrastructure Layer (EF Core, Caching, etc.)
```

### Enforcement

- Use `NetArchTest.Rules` to validate dependency rules
- Domain layer has **zero** external dependencies (except primitives)
- Infrastructure depends on domain abstractions, not concrete types

### Example: NetArchTest Validation

```csharp
// tests/Idevs.ArchitectureTests/DependencyTests.cs
using NetArchTest.Rules;
using Xunit;

public class DependencyTests
{
    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        var result = Types
            .InAssembly(typeof(Idevs.Domain.Entity).Assembly)
            .Should()
            .NotHaveDependencyOn("Idevs.Data")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_ShouldNotDependOnWeb()
    {
        var result = Types
            .InAssembly(typeof(Idevs.Application.Commands.ICommand).Assembly)
            .Should()
            .NotHaveDependencyOn("Idevs.Web")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
```

## Creating the Solution

### Step 1: Create Solution File

```bash
# Create solution
dotnet new sln -n idevs-core

# Create folder structure
mkdir -p src tests samples docs
```

### Step 2: Create Core Projects

```bash
# Core abstraction package
dotnet new classlib -n Idevs -o src/Idevs --framework net8.0

# Application layer
dotnet new classlib -n Idevs.Application -o src/Idevs.Application --framework net8.0

# Domain layer  
dotnet new classlib -n Idevs.Data -o src/Idevs.Data --framework net8.0

# Web integration
dotnet new classlib -n Idevs.Web -o src/Idevs.Web --framework net8.0
```

### Step 3: Create Infrastructure Projects

```bash
# PostgreSQL provider
dotnet new classlib -n Idevs.Data.PostgreSQL -o src/Idevs.Data.PostgreSQL --framework net8.0

# Caching abstractions
dotnet new classlib -n Idevs.Caching -o src/Idevs.Caching --framework net8.0

# Redis implementation
dotnet new classlib -n Idevs.Caching.Redis -o src/Idevs.Caching.Redis --framework net8.0
```

### Step 4: Create Test Projects

```bash
# Unit tests
dotnet new xunit -n Idevs.UnitTests -o tests/Idevs.UnitTests --framework net8.0

# Integration tests
dotnet new xunit -n Idevs.IntegrationTests -o tests/Idevs.IntegrationTests --framework net8.0

# Architecture tests
dotnet new xunit -n Idevs.ArchitectureTests -o tests/Idevs.ArchitectureTests --framework net8.0
```

### Step 5: Add Projects to Solution

```bash
# Add all src projects
dotnet sln add src/**/*.csproj

# Add all test projects
dotnet sln add tests/**/*.csproj
```

## Project Dependencies

### Core Package (`Idevs`)

**No dependencies** - Pure abstractions and interfaces

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <!-- No PackageReferences - pure abstractions -->
</Project>
```

### Application Package (`Idevs.Application`)

Depends on: `Idevs`

```xml
<ItemGroup>
  <ProjectReference Include="..\Idevs\Idevs.csproj" />
</ItemGroup>
```

### Infrastructure Packages

Depend on: `Idevs`, `Idevs.Data`

```xml
<!-- Idevs.Data.PostgreSQL -->
<ItemGroup>
  <ProjectReference Include="..\Idevs\Idevs.csproj" />
  <ProjectReference Include="..\Idevs.Data\Idevs.Data.csproj" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
</ItemGroup>
```

## Validation

Run these commands to validate structure:

```bash
# List all projects
dotnet sln list

# Build solution
dotnet build

# Verify no circular dependencies
dotnet list package --include-transitive
```

## Best Practices

### Do

- ✅ Keep core packages dependency-free
- ✅ Use clear, descriptive project names
- ✅ Group related projects in folders
- ✅ Document project purpose in README files

### Don't

- ❌ Create circular dependencies
- ❌ Reference infrastructure from domain
- ❌ Mix responsibilities in single project
- ❌ Use overly generic names

## Next Steps

- **[SDK & Targeting](02-sdk-targeting.md)** - Pin .NET SDK version
- **[Package Management](03-package-management.md)** - Centralize dependencies
- **[Build Properties](04-build-properties.md)** - Shared MSBuild config

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: SDK & Targeting →](02-sdk-targeting.md)
