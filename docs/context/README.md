# Idevs Framework Documentation Index

Welcome to the **Idevs** framework documentation hub. This directory contains comprehensive planning documentation for a .NET building-block framework designed for modern SaaS and ERP applications.

## 📋 Overview

**Idevs** is a CQRS-centric, multi-tenant framework that powers first-party solutions such as:

- Multi-store retail management systems
- Subscription billing platforms  
- Accounting and finance suites

**From idevs.work** - "I am a developer" - building blocks for modern .NET applications.

The framework is designed with Domain-Driven Design (DDD) principles, Test-Driven Development (TDD) practices, and a PostgreSQL-first approach while remaining open source for the wider community to extend.

**Package Namespace**: `Idevs.*` (e.g., `Idevs`, `Idevs.Application`, `Idevs.Data.PostgreSQL`)  
**Repository**: `idevs-core` (GitHub repo and solution file name)

## 📁 Documentation Structure

This documentation follows the **"one implementation plan per folder"** principle. Each phase has its own dedicated folder containing detailed planning documents.

### Core Documentation

Located in `docs/context/`:

- **[discovery-summary.md](discovery-summary.md)** - Stakeholders, personas, scenarios, and business drivers
- **[cqrs-framework-plan.md](cqrs-framework-plan.md)** - Overall roadmap with cross-phase checklist
- **[glossary.md](glossary.md)** - Standardized terminology (CQRS, DDD, multi-tenancy, observability)
- **[threat-model.md](threat-model.md)** - Security considerations and ASVS checklist
- **[observability-blueprint.md](observability-blueprint.md)** - Metrics, logging, and tracing strategy

### Phase Plans

Each phase folder contains a detailed implementation plan:

- **[phase-0-discovery/](phase-0-discovery/)** - Discovery, guardrails, stakeholder alignment, ADR initialization
- **[phase-1-platform/](phase-1-platform/)** - Platform scaffolding, build strategy, GitVersion, CI/CD design
- **[phase-2-domain/](phase-2-domain/)** - Domain contracts, CQRS abstractions, result patterns
- **[phase-3-application/](phase-3-application/)** - Application layer, mediator pipeline, behaviors
- **[phase-4-web/](phase-4-web/)** - ASP.NET Core integration, middleware, GraphQL support
- **[phase-5-infrastructure/](phase-5-infrastructure/)** - EF Core, repositories, persistence, caching
- **[phase-6-release/](phase-6-release/)** - Documentation finalization, samples, release governance

### Architecture Decision Records (ADRs)

Located in **[adrs/](adrs/)**:

- **ADR-0001**: Tenancy Strategy (row-level, PostgreSQL RLS, resolution precedence)
- **ADR-0002**: Audit Logging (EF Core interceptors, Serilog sinks, correlation)
- **ADR-0003**: Soft Delete (global query filters, purge policy, generic interfaces)
- **ADR-0004**: Release Governance (Git Flow, GitVersion, Conventional Commits)
- **ADR-0005**: DI Container Strategy (explicit registration, no reflection)

## 🎯 Phase Status Checklist

- [x] **Phase 0**: Discovery & Guardrails ✅ (Complete - 2025-10-05)
- [ ] **Phase 1**: Core Domain Abstractions ⏳ (Up Next)
- [ ] **Phase 2**: Application Layer & CQRS Pipeline ⏳ (Planned)
- [ ] **Phase 3**: Infrastructure & Persistence ⏳ (Planned)
- [ ] **Phase 4**: Web Integration & APIs ⏳ (Planned)
- [ ] **Phase 5**: Advanced Features & Optimization ⏳ (Planned)
- [ ] **Phase 6**: Documentation, Samples & Release Readiness ⏳ (Planned)

## 🤝 Contributing to Documentation

### Documentation Standards

All documentation in this repository follows these conventions:

1. **Structured Format**: Each phase plan includes:
   - **Objectives** - Clear goals for the phase
   - **Key Activities** - Specific tasks and deliverables
   - **Deliverables** - Concrete outputs
   - **Success Metrics** - Measurable outcomes
   - **Risks & Mitigations** - Identified risks with mitigation strategies
   - **Exit Criteria** - Conditions for phase completion
   - **Tracking Checklist** - Actionable items with checkboxes

2. **Checkboxes for Tracking**: Use `- [ ]` for pending items and `- [x]` for completed items

3. **Cross-References**: Link to relevant ADRs, phase docs, and core documents

4. **Metadata**: Include document owner and last updated date where applicable

### Conventional Commits for Documentation

When updating documentation, follow Conventional Commits:

```bash
docs: add phase 2 domain contracts specification
docs: update ADR-0001 with RLS implementation details
docs: clarify tenant resolution precedence in discovery summary
```

### Style Guide

- **Concise Language**: Clear, direct statements
- **Active Voice**: "Document the strategy" not "The strategy should be documented"
- **Bullet Points**: Break down complex information into digestible lists
- **Code Examples**: Use fenced code blocks with language identifiers
- **Clear Owners**: Assign responsibility where applicable (e.g., "Platform Lead", "Security Team")

## 🔗 Quick Links

- **Repository Guidelines**: [../AGENTS.md](../../AGENTS.md)
- **Root README**: [../../README.md](../../README.md)
- **Roadmap**: [cqrs-framework-plan.md](cqrs-framework-plan.md)

## 🏗️ Design Principles

The framework adheres to these core principles:

### Architecture & Design

- **CQRS-First**: Command Query Responsibility Segregation as the primary pattern
- **DDD Boundaries**: Clear domain boundaries with aggregates, entities, and value objects
- **Multi-Tenancy**: Row-level tenant isolation with PostgreSQL-first approach
- **Offline-First**: Support for offline sync, conflict resolution, and delta feeds

### Development Practices

- **TDD (Test-Driven Development)**: Red → Green → Refactor cycle
- **≥80% Coverage**: Branch coverage target across all components
- **No System.Reflection**: Prefer explicit registration or source generators
- **Modern C#**: Primary constructors, collection expressions, pattern matching, required members
- **Standard .NET Patterns**: Use middleware, decorators, and interceptors instead of custom mediator abstractions

### Operational Excellence

- **GitVersion + Conventional Commits**: Semantic versioning with Git Flow
- **Observability**: Structured logging (Serilog), metrics (RED), tracing (OpenTelemetry)
- **Security-First**: ASVS compliance, secrets management, PII redaction
- **Audit Trails**: Comprehensive audit logging with tenant and correlation awareness

### Technology Stack

- **.NET 8.0 LTS** (with .NET 10.0 LTS forward-compatibility plan)
- **PostgreSQL** (primary database, optional SQL Server/MySQL)
- **EF Core** with code-first migrations
- **xUnit, Shouldly, NSubstitute** for testing
- **Serilog** for structured logging
- **HotChocolate** for GraphQL (planned)

## 📊 Documentation Metrics

- **Core Documents**: 5 foundational documents
- **Phase Plans**: 7 phase-specific implementation plans (1 complete, 6 planned)
- **ADRs**: 5 architecture decision records
- **Total Lines**: 10,000+ lines of comprehensive documentation
- **Status**: ✅ **Phase 0 COMPLETE** - Build system operational, ready for Phase 1

## ❓ Questions or Feedback?

For questions about the documentation structure or content:

1. Review the relevant phase plan or ADR first
2. Check the [glossary](glossary.md) for term definitions
3. Refer to [AGENTS.md](../../AGENTS.md) for repository guidelines
4. Consult the [threat-model](threat-model.md) or [observability-blueprint](observability-blueprint.md) for cross-cutting concerns

---

**Last Updated**: 2025-10-05  
**Document Owner**: Platform Team  
**Status**: Active Development - Phase 0 Complete
