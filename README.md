# Idevs Framework

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-in%20development-yellow.svg)](https://github.com/klomkling/idevs-core)

A .NET building-block framework for modern SaaS and ERP applications. Built with CQRS, DDD, and multi-tenancy at its core.

## 🎯 Overview

**Idevs** (from [idevs.work](https://idevs.work) - "I am a developer") is a production-ready, CQRS-centric framework that powers first-party solutions such as:

- 🏪 Multi-store retail management systems
- 💳 Subscription billing platforms
- 💰 Accounting and finance suites

**Package Namespace**: `Idevs.*` (e.g., `Idevs`, `Idevs.Application`, `Idevs.Data.PostgreSQL`)

## ✨ Key Features

- **CQRS Pattern**: Command/Query separation with decorator pipeline
- **Domain-Driven Design**: Aggregate roots, entities, value objects
- **Multi-Tenancy**: PostgreSQL RLS with row-level isolation
- **Result Pattern**: Railway-oriented programming (no exceptions)
- **Modern C#**: C# 12 features throughout
- **Test-Driven**: ≥80% branch coverage target
- **No Reflection**: Explicit DI registration for performance
- **PostgreSQL-First**: Optimized for PostgreSQL with optional SQL Server/MySQL

## 📦 Packages

```text
Idevs                              # Core abstractions
Idevs.Application                  # Application layer patterns
Idevs.Domain                       # Domain primitives
Idevs.Infrastructure               # Base infrastructure
Idevs.Infrastructure.PostgreSQL    # PostgreSQL implementation
Idevs.Infrastructure.Redis         # Redis caching
Idevs.Web                          # ASP.NET Core integration
Idevs.Testing                      # Testing utilities
```text

## 🚀 Quick Start

### Installation

```bash
# Install the core package
dotnet add package Idevs --version 0.1.0
```

### Basic Usage

```csharp
using Idevs.Abstractions;
using Idevs.Common;
using Idevs.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

// Define a command
public sealed record CreateUserCommand(string Email, string Name) : ICommand;

// Implement the handler
public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        // Business logic here
        return Result.Success();
    }
}

// Register in DI
var services = new ServiceCollection();
services.AddIdevs(options =>
{
    options.Decorators.EnableLogging = true;
    options.Decorators.EnableValidation = true;
});
services.AddCommandHandler<CreateUserCommand, CreateUserCommandHandler>();

var provider = services.BuildServiceProvider();

// Execute the command
var handler = provider.GetRequiredService<ICommandHandler<CreateUserCommand>>();
var result = await handler.HandleAsync(
    new CreateUserCommand("user@example.com", "John Doe"),
    cancellationToken);

if (result.IsSuccess)
{
    // Success!
}
```

See [Phase 1 Documentation](./docs/context/phase-1-platform/implementation/phase-1-platform-foundation/README.md) for comprehensive examples

## 📚 Documentation

Comprehensive documentation is available in the [`docs/context/`](./docs/context/) directory:

- **[README](./docs/context/README.md)** - Documentation index
- **[Discovery Summary](./docs/context/discovery-summary.md)** - Stakeholders and scenarios
- **[CQRS Framework Plan](./docs/context/cqrs-framework-plan.md)** - Overall roadmap
- **[Glossary](./docs/context/glossary.md)** - Terminology reference
- **[Architecture Decision Records](./docs/context/adrs/)** - Key design decisions

### Phase Documentation

Each implementation phase has detailed documentation:

- [Phase 0: Discovery & Guardrails](./docs/context/phase-0-discovery/)
- [Phase 1: Platform Scaffold](./docs/context/phase-1-platform/)
- [Phase 2: Domain & Contracts](./docs/context/phase-2-domain/)
- [Phase 3: Application Layer](./docs/context/phase-3-application/)
- [Phase 4: Web Adapters](./docs/context/phase-4-web/)
- [Phase 5: Infrastructure](./docs/context/phase-5-infrastructure/)
- [Phase 6: Release Readiness](./docs/context/phase-6-release/)

## 🏗️ Architecture Principles

- **CQRS-First**: Command Query Responsibility Segregation as the primary pattern
- **DDD Boundaries**: Clear domain boundaries with aggregates and bounded contexts
- **Multi-Tenancy**: Row-level tenant isolation with defense-in-depth
- **Offline-First**: Support for offline sync and conflict resolution
- **TDD**: Test-Driven Development with red → green → refactor cycle
- **No Reflection**: Explicit registration or source generators (ADR-0005)
- **Modern .NET**: Primary constructors, collection expressions, pattern matching

## 🛠️ Technology Stack

- **.NET 8.0 LTS** (with .NET 10.0 forward-compatibility)
- **PostgreSQL** (primary), SQL Server, MySQL (optional)
- **Entity Framework Core 8.0**
- **xUnit, Shouldly, NSubstitute** (testing)
- **Serilog** (structured logging)
- **HotChocolate** (GraphQL - planned)

## 🔒 Security & Compliance

- **Multi-Tenant Isolation**: PostgreSQL RLS + query filters
- **Audit Trails**: Comprehensive who/what/when tracking
- **OWASP ASVS**: Security considerations documented
- **No Secrets in Repo**: User secrets and environment variables
- **PII Redaction**: Privacy compliance built-in

## 📊 Project Status
|| Phase | Status | Completion | Description |
|-------|--------|------------|-------------|
| Phase 0 | ✅ Complete | 2025-10-05 | Discovery & Guardrails |
| Phase 1 | ✅ Complete | 2025-10-08 | Platform Foundation - Result, CQRS, Decorators |
| Phase 2 | ⏳ Up Next | - | Application Layer Extensions |
| Phase 3 | ⏳ Planned | - | Infrastructure & Persistence |
| Phase 4 | ⏳ Planned | - | Web Integration & APIs |
| Phase 5 | ⏳ Planned | - | Advanced Features |
| Phase 6 | ⏳ Planned | - | Release Readiness |

**Phase 0 Deliverables** (Complete):

- ✅ 5 foundational ADRs accepted
- ✅ Solution structure with `src/Idevs` and `tests/Idevs.Tests`
- ✅ Build system: Directory.Build.props, Directory.Packages.props
- ✅ Multi-platform CI/CD: GitHub Actions workflows (Linux, Windows, macOS)
- ✅ GitVersion configured for semantic versioning
- ✅ Baseline code: `IIdevsMarker` interface
- ✅ Documentation validation: Markdownlint with 2,000+ files processed
- ✅ Development guidelines: CONTRIBUTING.md, AGENTS.md

**Phase 1 Deliverables** (Complete):

- ✅ Result pattern for type-safe error handling
- ✅ CQRS abstractions (ICommand, IQuery, handlers)
- ✅ Decorator infrastructure (logging, validation, metrics)
- ✅ Core services (tenant context, current user, unit of work, metrics)
- ✅ Dependency injection with zero reflection
- ✅ 48 unit tests, 90.1% line coverage, 70% branch coverage
- ✅ Comprehensive documentation and examples
- ✅ NuGet package v0.1.0

**Current Implementation Status**:

- **Build System**: ✅ Operational (multi-platform, .NET 9)
- **CI/CD**: ✅ Configured (Linux, Windows, macOS)
- **GitVersion**: ✅ Semantic versioning with environment variables
- **Documentation**: ✅ Complete with automated validation
- **Code Quality**: ✅ Linting, formatting, and style enforcement
- **Package**: ✅ Published v0.1.0 (pre-release)
- **Next**: Phase 2 - Application Layer Extensions

## 🤝 Contributing

Contributions are welcome! Please read our [contributing guidelines](./CONTRIBUTING.md) and [repository guidelines](./AGENTS.md) first.

### Development Requirements

- **.NET SDK 9.0** or later
- **PostgreSQL 15+** (for future integration tests)
- **Docker** (optional, for Testcontainers)

### Getting Started

```bash
# Clone the repository
git clone https://github.com/klomkling/warp-idevs-core.git
cd warp-idevs-core

# Checkout develop branch
git checkout develop

# Restore dependencies
dotnet restore

# Build (Release mode with CI settings)
dotnet build -c Release

# Run tests with coverage
dotnet test -c Release -p:CollectCoverage=true -p:Threshold=80 -p:ThresholdType=branch

# Pack NuGet packages
dotnet pack -c Release -o artifacts/packages

# Check semantic version
dotnet tool restore
dotnet gitversion
```text

See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed development workflow, Git Flow branching, conventional commits, and PR guidelines.

## 📝 License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Domain-Driven Design patterns from Eric Evans and Vaughn Vernon
- CQRS guidance from Greg Young and Udi Dahan
- Modern .NET patterns from the .NET team

## 📞 Support

- **Documentation**: [docs/context/](./docs/context/)
- **Issues**: [GitHub Issues](https://github.com/klomkling/idevs-core/issues)
- **Discussions**: [GitHub Discussions](https://github.com/klomkling/idevs-core/discussions)

---

**Built with ❤️ by the idevs.work community**
