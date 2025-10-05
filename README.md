# Idevs Framework

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)
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

```
Idevs                              # Core abstractions
Idevs.Application                  # Application layer patterns
Idevs.Domain                       # Domain primitives
Idevs.Infrastructure               # Base infrastructure
Idevs.Infrastructure.PostgreSQL    # PostgreSQL implementation
Idevs.Infrastructure.Redis         # Redis caching
Idevs.Web                          # ASP.NET Core integration
Idevs.Testing                      # Testing utilities
```

## 🚀 Quick Start

> **Note**: This framework is currently in active development. Full implementation coming soon.

```bash
# Install the core package (when available)
dotnet add package Idevs

# Install application layer
dotnet add package Idevs.Application

# Install PostgreSQL infrastructure
dotnet add package Idevs.Infrastructure.PostgreSQL
```

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

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 0 | ✅ Complete | Discovery & Guardrails |
| Phase 1 | ✅ Complete | Platform Scaffolding (Documentation) |
| Phase 2 | 🚧 In Progress | Domain & Contracts (Implementation) |
| Phase 3 | ⏳ Planned | Application Layer |
| Phase 4 | ⏳ Planned | Web Adapters |
| Phase 5 | ⏳ Planned | Infrastructure |
| Phase 6 | ⏳ Planned | Release Readiness |

**Documentation**: ✅ 100% Complete (10,000+ lines)  
**Implementation**: 🚧 In Progress

## 🤝 Contributing

Contributions are welcome! Please read our [contribution guidelines](./AGENTS.md) first.

### Development Requirements

- .NET 8.0 SDK or later
- PostgreSQL 15+ (for integration tests)
- Docker (optional, for Testcontainers)

### Getting Started

```bash
# Clone the repository
git clone https://github.com/klomkling/idevs-core.git
cd idevs-core

# Checkout develop branch
git checkout develop

# Restore dependencies (when projects are created)
dotnet restore

# Build
dotnet build

# Run tests
dotnet test
```

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
