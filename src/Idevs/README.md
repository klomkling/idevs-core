# Idevs - Core Library

[![NuGet](https://img.shields.io/nuget/v/Idevs.svg)](https://www.nuget.org/packages/Idevs/)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)

Core abstractions and building blocks for the Idevs framework.

## Overview

**Idevs** is the foundation package providing essential contracts, utilities, and patterns for building CQRS-based, multi-tenant applications with Domain-Driven Design principles.

## Installation

```bash
dotnet add package Idevs
```

## What's Included

### Marker Interfaces

- **IIdevsMarker** - Assembly marker interface for package identification

## Input Validation

For input validation and guard clauses, we recommend using built-in .NET methods or established libraries:

### Built-in .NET Validation

```csharp
public class OrderService
{
    public void CreateOrder(string customerEmail, decimal amount)
    {
        // Use built-in .NET guard methods
        ArgumentNullException.ThrowIfNull(customerEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);
        
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
            
        // Create order logic...
    }
}
```

### Recommended: Ardalis.GuardClauses

For more comprehensive validation, consider using [Ardalis.GuardClauses](https://github.com/ardalis/GuardClauses):

```bash
dotnet add package Ardalis.GuardClauses
```

```csharp
using Ardalis.GuardClauses;

public class OrderService
{
    public void CreateOrder(string customerEmail, decimal amount)
    {
        Guard.Against.NullOrWhiteSpace(customerEmail);
        Guard.Against.NegativeOrZero(amount);
        
        // Create order logic...
    }
}
```

## Documentation

For comprehensive documentation, see the [main repository README](../../README.md).

## Design Principles

- **No Reflection**: Explicit over magic - no runtime discovery or assembly scanning
- **Modern C#**: Leverages C# 12 features (primary constructors, collection expressions, pattern matching)
- **Minimal Dependencies**: Core package has minimal external dependencies
- **Test-Driven**: ≥80% branch coverage maintained

## Related Packages

- `Idevs.Application` - Application layer patterns (commands, queries, handlers)
- `Idevs.Domain` - Domain primitives and contracts
- `Idevs.Infrastructure` - Base infrastructure implementations
- `Idevs.Data.PostgreSQL` - PostgreSQL-specific data access
- `Idevs.Web` - ASP.NET Core integration

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines on how to contribute.

## Support

- 📖 [Documentation](../../docs/context/)
- 💬 [GitHub Discussions](https://github.com/yourusername/warp-idevs-core/discussions)
- 🐛 [Issues](https://github.com/yourusername/warp-idevs-core/issues)
