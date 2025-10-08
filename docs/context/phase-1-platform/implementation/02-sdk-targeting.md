# Phase 1: SDK & Target Framework

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Pin the .NET SDK version for reproducible builds and define target framework strategy for the Idevs framework.

## SDK Pinning

### Create global.json

**File:** `global.json` (repository root)

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
```

### Rationale

- **Pin to specific .NET 8 SDK** - Ensures reproducible builds across environments
- **`rollForward: latestMinor`** - Allows patch updates (8.0.1, 8.0.2, etc.) automatically
- **`allowPrerelease: false`** - Prevents accidental use of preview SDKs
- **Prevents "works on my machine" issues** - All developers use same SDK version

### Verification

```bash
# Check current SDK version
dotnet --version
# Should output: 8.0.100 (or latest minor like 8.0.x)

# List installed SDKs
dotnet --list-sdks

# Build with pinned SDK
dotnet build
```

## Target Framework

### Current Strategy

**Framework:** `net8.0` (LTS until November 2026)

Configure in all project files:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### Future: .NET 10 LTS

**Target Date:** November 2025

When .NET 10.0 LTS is released, evaluate multi-targeting:

```xml
<PropertyGroup>
  <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
</PropertyGroup>
```

### Multi-Targeting Decision Criteria

**When to multi-target:**

- ✅ .NET 10.0 reaches GA (stable release)
- ✅ Specific .NET 10 features needed (e.g., performance improvements)
- ✅ Customer demand for latest .NET version
- ✅ Sufficient testing capacity for both frameworks

**When to single-target:**

- ✅ Early in framework lifecycle (prefer stability)
- ✅ .NET 8 meets all requirements
- ✅ Limited testing resources
- ✅ Default choice for simplicity

**Current Decision:** Single target `net8.0` for Phase 1-6

## C# Language Version

### Strategy

Use latest stable C# version for .NET 8:

```xml
<PropertyGroup>
  <LangVersion>12.0</LangVersion>
</PropertyGroup>
```

**Benefits:**

- Primary constructors
- Collection expressions
- Required members
- Pattern matching improvements
- Raw string literals

### Example: Modern C# Features

```csharp
// Primary constructors
public class ProductService(IRepository<Product> repository, ILogger<ProductService> logger)
{
    public async Task<Product> GetByIdAsync(ProductId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await repository.GetByIdAsync(id);
    }
}

// Collection expressions
var tags = ["electronics", "computers", "laptops"];

// Required members
public record CreateProductCommand
{
    public required string Name { get; init; }
    public required decimal Price { get; init; }
}
```

## Runtime Configuration

### RuntimeIdentifiers (Optional)

For self-contained deployments:

```xml
<PropertyGroup>
  <RuntimeIdentifiers>linux-x64;win-x64;osx-x64;osx-arm64</RuntimeIdentifiers>
</PropertyGroup>
```

**Note:** Not needed for typical library packages. Use only for sample applications.

## SDK Installation

### Developer Setup

```bash
# Download .NET 8 SDK
# URL: https://dotnet.microsoft.com/download/dotnet/8.0

# Verify installation
dotnet --version

# Should output: 8.0.x
```

### CI/CD Setup

```yaml
# .github/workflows/ci.yml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'
    dotnet-quality: 'ga'  # Only use GA releases
```

## SDK Features Used

### .NET 8 Key Features

**Performance:**

- Native AOT support
- Improved JIT compilation
- Reduced memory footprint

**Language:**

- C# 12 features
- Primary constructors
- Collection expressions

**Libraries:**

- System.Text.Json improvements
- Improved TimeProvider for testing
- Keyed DI services

**ASP.NET Core 8:**

- Improved minimal APIs
- Native AOT support
- Built-in rate limiting

## Compatibility Matrix

| .NET Version | Support End | Status |
|--------------|-------------|--------|
| .NET 8.0 LTS | Nov 2026 | ✅ Current |
| .NET 9.0 STS | May 2025 | ⚠️ Skip (STS) |
| .NET 10.0 LTS | Nov 2027 | 📋 Future |

**Legend:**

- **LTS** = Long-term Support (3 years)
- **STS** = Standard Term Support (18 months)

## Migration Strategy

### When .NET 10 LTS Releases

**Phase 1: Evaluation (Month 1-2)**

1. Install .NET 10 SDK preview
2. Test framework compilation
3. Run full test suite
4. Identify breaking changes

**Phase 2: Multi-Targeting (Month 3-4)**

1. Add `net10.0` to `TargetFrameworks`
2. Fix compilation errors
3. Update CI/CD pipeline
4. Test on both frameworks

**Phase 3: Migration (Month 5-6)**

1. Drop `net8.0` target after .NET 8 EOL
2. Update documentation
3. Communicate to users via migration guide

## Best Practices

### Do

- ✅ Pin SDK version in `global.json`
- ✅ Use LTS releases for production
- ✅ Test on target framework before release
- ✅ Document framework requirements

### Don't

- ❌ Use preview SDKs in production
- ❌ Mix STS and LTS versions
- ❌ Skip SDK pinning
- ❌ Target unsupported frameworks

## Troubleshooting

### Issue: SDK Not Found

**Symptom:** `error NETSDK1045: The current .NET SDK does not support targeting .NET 8.0`

**Solution:**

1. Install .NET 8 SDK from official site
2. Verify with `dotnet --version`
3. Restart terminal/IDE

### Issue: Wrong SDK Version Used

**Symptom:** Build uses different SDK than specified

**Solution:**

1. Check `global.json` exists in repo root
2. Verify JSON syntax is correct
3. Run `dotnet --info` to see SDK resolution

### Issue: Multi-Target Build Fails

**Symptom:** Compilation errors on one framework

**Solution:**

1. Use conditional compilation: `#if NET8_0`
2. Check for framework-specific APIs
3. Test both frameworks locally

## Next Steps

- **[Package Management](03-package-management.md)** - Centralize dependencies
- **[Build Properties](04-build-properties.md)** - Shared MSBuild config
- **[Code Style](05-code-style.md)** - EditorConfig setup

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: Package Management →](03-package-management.md)
