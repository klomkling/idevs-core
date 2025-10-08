# Phase 2: Domain & Contracts - References

> **Purpose:** Curated resources for implementing Phase 2  
> **Last Updated:** 2025-10-08

## Domain-Driven Design (DDD)

### Essential Reading

**Books**
- **[Domain-Driven Design](https://www.domainlanguage.com/ddd/)** by Eric Evans (Blue Book)
  - The definitive guide to DDD
  - Focus: Chapters 5-7 (Model-Driven Design), 10-11 (Supple Design)
  
- **[Implementing Domain-Driven Design](https://vaughnvernon.com/implementing-domain-driven-design/)** by Vaughn Vernon (Red Book)
  - Practical implementation guide
  - Focus: Chapters 5-8 (Entities, Value Objects, Domain Services, Domain Events)

- **[Domain-Driven Design Distilled](https://www.pearson.com/en-us/subject-catalog/p/domain-driven-design-distilled/P200000009456)** by Vaughn Vernon
  - Quick overview (100 pages)
  - Perfect for team alignment

**Online Resources**
- **[DDD Community](https://www.domainlanguage.com/)** - Eric Evans' official site
- **[DDD Crew](https://github.com/ddd-crew)** - GitHub organization with templates & tools
- **[DDD Reference](https://www.domainlanguage.com/ddd/reference/)** - Free summary by Eric Evans

### Patterns & Practices

**Entities & Value Objects**
- [Martin Fowler: Value Object](https://martinfowler.com/bliki/ValueObject.html)
- [Jimmy Bogard: Immutable Value Objects](https://lostechies.com/jimmybogard/2007/12/03/immutable-value-objects/)
- [Enterprise Craftsmanship: Value Objects](https://enterprisecraftsmanship.com/posts/value-objects-explained/)

**Aggregates**
- [Martin Fowler: DDD Aggregate](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [Vaughn Vernon: Effective Aggregate Design](https://www.dddcommunity.org/library/vernon_2011/)
  - Part I: Aggregate modeling
  - Part II: Making aggregates work together
  - Part III: Event sourcing

**Repositories**
- [Martin Fowler: Repository](https://martinfowler.com/eaaCatalog/repository.html)
- [Martin Fowler: Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Edward Hieatt & Rob Mee: Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)

**Specifications**
- [Martin Fowler: Specification](https://martinfowler.com/apsupp/spec.pdf)
- [Vladimir Khorikov: Specification Pattern C# Implementation](https://enterprisecraftsmanship.com/posts/specification-pattern-c-implementation/)

---

## CQRS & Event-Driven Architecture

### Command Query Responsibility Segregation

**Core Concepts**
- [Martin Fowler: CQRS](https://martinfowler.com/bliki/CQRS.html)
- [Greg Young: CQRS Documents](https://cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf)
- [Microsoft: CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)

**Implementation Guides**
- [Jimmy Bogard: CQRS with MediatR](https://lostechies.com/jimmybogard/2016/10/27/cqrsmediatr-implementation-patterns/)
- [Vladimir Khorikov: CQRS in Practice](https://www.pluralsight.com/courses/cqrs-in-practice)

### Domain Events

**Foundational Articles**
- [Martin Fowler: Domain Event](https://martinfowler.com/eaaDev/DomainEvent.html)
- [Udi Dahan: Domain Events – Salvation](https://udidahan.com/2009/06/14/domain-events-salvation/)
- [Microsoft: Domain Events Implementation](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

**Event Sourcing**
- [Martin Fowler: Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html)
- [Greg Young: Event Sourcing Basics](https://www.youtube.com/watch?v=8JKjvY4etTY)
- [Eventstore Documentation](https://www.eventstore.com/event-sourcing)

---

## Result Pattern & Functional Error Handling

### Railway Oriented Programming

**Core Concepts**
- [Scott Wlaschin: Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
  - Excellent visualization of Result pattern
  - Composability explained

- [Vladimir Khorikov: Functional C#](https://enterprisecraftsmanship.com/posts/functional-c-primitive-obsession/)
  - Result<T> implementation
  - CSharpFunctionalExtensions library

**C# Implementations**
- [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions)
- [LanguageExt](https://github.com/louthy/language-ext)
- [ErrorOr](https://github.com/amantinband/error-or)

---

## Multi-Tenancy

### Architectural Patterns

**Microsoft Documentation**
- [Multi-tenant SaaS Patterns](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/tenancy-models)
- [Multi-tenant Identity](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/identity)
- [Data Partitioning](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/data-partitioning)

**Implementation Guides**
- [Database-per-tenant vs Shared](https://docs.microsoft.com/en-us/azure/sql-database/saas-tenancy-app-design-patterns)
- [Row-Level Security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)
- [EF Core Global Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)

---

## C# & .NET Specific

### Language Features

**Modern C# (C# 10+)**
- [Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [Init-only Setters](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init)
- [Nullable Reference Types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)
- [Pattern Matching](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching)
- [Primary Constructors](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#primary-constructors)

**Expression Trees**
- [Expression Trees Overview](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/)
- [Building Expression Trees](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/expression-trees-building)
- [ExpressionVisitor](https://learn.microsoft.com/en-us/dotnet/api/system.linq.expressions.expressionvisitor)

### .NET 8 Features

**Official Documentation**
- [What's New in .NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Performance Improvements](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8/)
- [ASP.NET Core 8](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-8.0)

---

## Entity Framework Core

### Core Concepts

**Official Docs**
- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [Shadow Properties](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)
- [Owned Types](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities)

**Value Conversions**
- [Value Converters](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions)
- [Value Comparers](https://learn.microsoft.com/en-us/ef/core/modeling/value-comparers)

### Interceptors & Extensions

**Saving Data**
- [SaveChanges Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [Audit Trail Implementation](https://www.thereformedprogrammer.net/ef-core-in-depth-saving-data-in-entity-framework-core/)

**Query Optimization**
- [Split Queries](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries)
- [Compiled Queries](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#compiled-queries)
- [Query Tags](https://learn.microsoft.com/en-us/ef/core/querying/tags)

---

## Testing

### Unit Testing

**Frameworks & Tools**
- [xUnit Documentation](https://xunit.net/)
- [Shouldly Assertions](https://docs.shouldly.org/)
- [NSubstitute (Mocking)](https://nsubstitute.github.io/)
- [Bogus (Test Data)](https://github.com/bchavez/Bogus)

**Best Practices**
- [Microsoft: Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Vladimir Khorikov: Unit Testing Principles](https://enterprisecraftsmanship.com/posts/unit-testing-principles/)

### Integration Testing

**EF Core Testing**
- [Testing with InMemory](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy)
- [SQLite for Testing](https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database)
- [Respawn (Database Reset)](https://github.com/jbogard/Respawn)

---

## Architecture & Design Patterns

### Clean Architecture

**Core Principles**
- [Robert C. Martin: Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Microsoft: Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [Ardalis: Clean Architecture](https://github.com/ardalis/CleanArchitecture)

### Hexagonal Architecture (Ports & Adapters)

**Foundational**
- [Alistair Cockburn: Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture/)
- [Netflix: Ready for changes with Hexagonal Architecture](https://netflixtechblog.com/ready-for-changes-with-hexagonal-architecture-b315ec967749)

### Onion Architecture

**Resources**
- [Jeffrey Palermo: Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)
- [Implementing Onion Architecture in ASP.NET Core](https://www.codeguru.com/csharp/implementing-onion-architecture-in-asp-net-core/)

---

## Videos & Courses

### Conference Talks

**Domain-Driven Design**
- [Eric Evans: What I've learned about DDD since the book](https://www.youtube.com/watch?v=lE6Hxz4yomA)
- [Vaughn Vernon: How to Use Aggregates for Tactical Design](https://www.youtube.com/watch?v=Xf_aLAK1RfE)
- [Jimmy Bogard: Domain-Driven Refactoring](https://www.youtube.com/watch?v=f64tZ90Dntg)

**CQRS & Event Sourcing**
- [Greg Young: CQRS and Event Sourcing](https://www.youtube.com/watch?v=JHGkaShoyNs)
- [Udi Dahan: Advanced Distributed Systems Design](https://particular.net/adsd)

**Clean Code & Architecture**
- [Robert C. Martin: Clean Code](https://www.youtube.com/watch?v=7EmboKQH8lM)
- [Ian Cooper: TDD, Where Did It All Go Wrong](https://www.youtube.com/watch?v=EZ05e7EMOLM)

### Online Courses

**Pluralsight**
- [Vladimir Khorikov: Domain-Driven Design in Practice](https://www.pluralsight.com/courses/domain-driven-design-in-practice)
- [Vladimir Khorikov: CQRS in Practice](https://www.pluralsight.com/courses/cqrs-in-practice)
- [Steve Smith: SOLID Principles for C# Developers](https://www.pluralsight.com/courses/csharp-solid-principles)

**Dometrain**
- [Nick Chapsas: From Zero to Hero: Domain-Driven Design](https://dometrain.com/)
- [Milan Jovanović: Pragmatic Clean Architecture](https://www.milanjovanovic.tech/pragmatic-clean-architecture)

---

## Community Resources

### Blogs & Websites

**Technical Blogs**
- [Enterprise Craftsmanship](https://enterprisecraftsmanship.com/) - Vladimir Khorikov
- [The Reformed Programmer](https://www.thereformedprogrammer.net/) - Jon P Smith (EF Core)
- [Code Opinion](https://codeopinion.com/) - Derek Comartin
- [Mark Seemann](https://blog.ploeh.dk/) - Functional programming in C#

**Community Sites**
- [Domain-Driven Design Community](https://www.dddcommunity.org/)
- [Stack Overflow: DDD Tag](https://stackoverflow.com/questions/tagged/domain-driven-design)
- [Reddit: r/dotnet](https://www.reddit.com/r/dotnet/)

### GitHub Examples

**Reference Implementations**
- [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers) - Microsoft's microservices example
- [Modular Monolith](https://github.com/kgrzybek/modular-monolith-with-ddd) - Kamil Grzybek
- [Sample .NET Core CQRS API](https://github.com/kgrzybek/sample-dotnet-core-cqrs-api)

---

## Tools & Libraries

### NuGet Packages

**Core Libraries**
- [MediatR](https://github.com/jbogard/MediatR) - CQRS/Mediator pattern
- [FluentValidation](https://github.com/FluentValidation/FluentValidation) - Validation
- [AutoMapper](https://github.com/AutoMapper/AutoMapper) - Object mapping
- [Serilog](https://github.com/serilog/serilog) - Structured logging

**Testing**
- [xUnit](https://www.nuget.org/packages/xunit/)
- [Shouldly](https://www.nuget.org/packages/Shouldly/)
- [NSubstitute](https://www.nuget.org/packages/NSubstitute/)
- [FluentAssertions](https://www.nuget.org/packages/FluentAssertions/)

### Development Tools

**Code Quality**
- [SonarLint](https://www.sonarlint.org/) - Real-time code analysis
- [Roslynator](https://github.com/JosefPihrt/Roslynator) - Roslyn analyzers
- [StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)

**Productivity**
- [ReSharper](https://www.jetbrains.com/resharper/) - Code analysis & refactoring
- [Rider](https://www.jetbrains.com/rider/) - Cross-platform .NET IDE
- [LINQPad](https://www.linqpad.net/) - .NET scratchpad

---

## Idevs Framework Specific

### Internal Documentation

**Architecture Decision Records (ADRs)**
- [ADR-0002: Audit Trails](../adrs/adr-0002-audit-trails.md)
- [ADR-0003: Soft Delete Strategy](../adrs/adr-0003-soft-delete.md)
- [ADR-0004: Multi-Tenancy](../adrs/adr-0004-multi-tenancy.md)
- [ADR-0005: Result Pattern](../adrs/adr-0005-result-pattern.md)

**Phase Documentation**
- [Phase 0: Discovery & Planning](../phase-0-discovery/)
- [Phase 1: Platform Scaffolding](../phase-1-platform/)
- [Phase 3: Application Layer](../phase-3-application/)
- [Phase 5: Infrastructure](../phase-5-infrastructure/)

### Code Examples

**Sample Aggregates**
- Customer Aggregate - See Guide 05
- Order Aggregate (TBD - Phase 3)
- Product Catalog (TBD - Phase 3)

---

## Academic Papers

### Domain-Driven Design

- **[Tackling Complexity in the Heart of Software](https://www.domainlanguage.com/ddd/)** - Eric Evans (2003)
- **[Domain-Driven Design Quickly](https://www.infoq.com/minibooks/domain-driven-design-quickly/)** - InfoQ Mini-book (Free)

### Software Architecture

- **[Architectural Patterns and Tactics for Scalable Complex Event Processing](https://www.researchgate.net/publication/220424560)**
- **[Microservices: Yesterday, Today, and Tomorrow](https://arxiv.org/abs/1606.04036)**

---

## Further Reading by Topic

### Value Objects
1. Evans (Blue Book): Chapter 5
2. Vernon (Red Book): Chapter 6
3. Fowler: Value Object pattern

### Aggregates
1. Vernon: Effective Aggregate Design (series)
2. Evans (Blue Book): Chapter 6
3. Vernon (Red Book): Chapter 10

### Domain Events
1. Vernon (Red Book): Chapter 8
2. Fowler: Domain Event pattern
3. Dahan: Domain Events - Salvation

### Repositories
1. Evans (Blue Book): Chapter 6
2. Fowler: Repository pattern
3. Vernon (Red Book): Chapter 12

### CQRS
1. Young: CQRS Documents
2. Fowler: CQRS overview
3. Vernon (Red Book): Chapter 4

---

## Quick Links

### Official Docs
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [C# Language Reference](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [EF Core Docs](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Docs](https://learn.microsoft.com/en-us/aspnet/core/)

### Community
- [.NET Blog](https://devblogs.microsoft.com/dotnet/)
- [C# Discord](https://discord.gg/csharp)
- [/r/csharp Subreddit](https://www.reddit.com/r/csharp/)

---

## Contributing

Found a useful resource? Add it to this document:

1. Ensure it's high-quality and relevant
2. Include author/source
3. Add brief description
4. Place in appropriate section
5. Keep alphabetical within sections

---

*Last Updated: 2025-10-08 | Version: 1.0*
