# Phase 2: Domain & Contracts - Completion Guide

> **Purpose:** Track implementation progress for Phase 2  
> **Last Updated:** 2025-10-08

## Overview

This guide provides a comprehensive checklist for implementing Phase 2: Domain & Contracts. Follow this guide to ensure all components are built, tested, and integrated correctly.

## Quick Reference

| Guide | Status | Est. Time | Priority |
|-------|--------|-----------|----------|
| [01-entity-interfaces](implementation/01-entity-interfaces.md) | ⬜ Not Started | 2-3 hours | 🔴 Critical |
| [02-value-objects](implementation/02-value-objects-base.md) | ⬜ Not Started | 2-3 hours | 🟡 High |
| [03-result-patterns](implementation/03-result-pattern-core.md) | ⬜ Not Started | 3-4 hours | 🔴 Critical |
| [04-cqrs-contracts](implementation/04-cqrs-contracts-basic.md) | ⬜ Not Started | 2-3 hours | 🔴 Critical |
| [05-aggregates-entities](implementation/05-aggregates-base.md) | ⬜ Not Started | 3-4 hours | 🔴 Critical |
| [06-tenant-user-context](implementation/06-tenant-context.md) | ⬜ Not Started | 2-3 hours | 🔴 Critical |
| [07-repository-uow](implementation/07-repository-pattern.md) | ⬜ Not Started | 3-4 hours | 🔴 Critical |
| [08-domain-events](implementation/08-domain-events-contracts.md) | ⬜ Not Started | 2-3 hours | 🟡 High |
| [09-specifications](implementation/09-specifications-pattern.md) | ⬜ Not Started | 2-3 hours | 🟢 Medium |

**Total Estimated Time:** 22-28 hours

## Implementation Checklist

### Phase 2.1: Core Interfaces & Patterns

#### ✅ Guide 01: Entity Interfaces

**Files to Create:**

- [ ] `src/Idevs/Contracts/Entities/IEntity.cs`
- [ ] `src/Idevs/Contracts/Entities/IAuditable.cs`
- [ ] `src/Idevs/Contracts/Entities/ISoftDeletable.cs`
- [ ] `src/Idevs/Contracts/Entities/ITenant.cs`
- [ ] `src/Idevs/Contracts/Entities/ITenantEntity.cs`
- [ ] `src/Idevs/Contracts/Entities/README.md`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/Contracts/EntityInterfacesTests.cs`
- [ ] All 4+ unit tests passing

**Verification:**

- [ ] All interfaces use nullable reference types correctly
- [ ] DateTimeOffset used (not DateTime)
- [ ] Generic constraint `IEquatable<TKey>` applied
- [ ] XML documentation complete

---

#### ✅ Guide 03: Result Patterns

**Files to Create:**

- [ ] `src/Idevs/Results/Error.cs`
- [ ] `src/Idevs/Results/Result.cs`
- [ ] `src/Idevs/Results/ResultExtensions.cs`
- [ ] `src/Idevs/Results/PagedResult.cs`
- [ ] `src/Idevs/Results/DomainErrors.cs`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/Results/ResultTests.cs`
- [ ] `tests/Idevs.Tests/Results/PagedResultTests.cs`
- [ ] All 13+ unit tests passing

**Verification:**

- [ ] Result<T> with Success/Failure factory methods
- [ ] Extension methods: Map, Bind, Match, Tap
- [ ] Implicit operators working
- [ ] Error propagation in chains verified

---

#### ✅ Guide 04: CQRS Contracts

**Files to Create:**

- [ ] `src/Idevs/CQRS/Unit.cs`
- [ ] `src/Idevs/CQRS/Commands/ICommand.cs`
- [ ] `src/Idevs/CQRS/Commands/ICommandHandler.cs`
- [ ] `src/Idevs/CQRS/Queries/IQuery.cs`
- [ ] `src/Idevs/CQRS/Queries/IQueryHandler.cs`
- [ ] `src/Idevs/CQRS/Commands/Examples/CreateCustomerCommand.cs`
- [ ] `src/Idevs/CQRS/Commands/Examples/CreateCustomerCommandHandler.cs`
- [ ] `src/Idevs/CQRS/Queries/Examples/GetCustomerByIdQuery.cs`
- [ ] `src/Idevs/CQRS/Queries/Examples/GetCustomerByIdQueryHandler.cs`
- [ ] `src/Idevs/CQRS/README.md`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/CQRS/CqrsContractsTests.cs`
- [ ] All 7+ unit tests passing

**Verification:**

- [ ] Unit type defined
- [ ] All handlers return `Task<Result<T>>`
- [ ] CancellationToken support
- [ ] Example implementations working

---

### Phase 2.2: Value Objects & Aggregates

#### ✅ Guide 02: Value Objects

**Files to Create:**

- [ ] `src/Idevs/Domain/ValueObjects/ValueObject.cs`
- [ ] `src/Idevs/Domain/ValueObjects/Email.cs`
- [ ] `src/Idevs/Domain/ValueObjects/Money.cs`
- [ ] `src/Idevs/Domain/ValueObjects/Address.cs`
- [ ] `src/Idevs/Domain/ValueObjects/DateRange.cs`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/Domain/ValueObjectTests.cs`
- [ ] All 16+ unit tests passing

**Verification:**

- [ ] All value objects immutable
- [ ] Factory methods return Result<T>
- [ ] Structural equality working
- [ ] Validation in constructors

---

#### ✅ Guide 05: Aggregates & Entities

**Files to Create:**

- [ ] `src/Idevs/Domain/Entities/Entity.cs`
- [ ] `src/Idevs/Domain/Entities/AggregateRoot.cs`
- [ ] `src/Idevs/Domain/Guards/Guard.cs`
- [ ] `src/Idevs/Domain/Entities/Examples/Customer.cs`
- [ ] `src/Idevs/Domain/Events/Examples/CustomerEvents.cs`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/Domain/EntityTests.cs`
- [ ] `tests/Idevs.Tests/Domain/AggregateRootTests.cs`
- [ ] `tests/Idevs.Tests/Domain/CustomerTests.cs`
- [ ] All 15+ unit tests passing

**Verification:**

- [ ] Identity-based equality working
- [ ] Domain events raised on state changes
- [ ] Private setters on aggregates
- [ ] Factory methods enforce invariants

---

### Phase 2.3: Multi-Tenancy & Context

#### ✅ Guide 06: Tenant & User Context

**Files to Create:**

- [ ] `src/Idevs/Context/TenantInfo.cs`
- [ ] `src/Idevs/Context/ITenantContext.cs`
- [ ] `src/Idevs/Context/UserInfo.cs`
- [ ] `src/Idevs/Context/ICurrentUser.cs`
- [ ] `src/Idevs/Context/Testing/TestTenantContext.cs`
- [ ] `src/Idevs/Context/Testing/TestCurrentUser.cs`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/Context/TenantContextTests.cs`
- [ ] `tests/Idevs.Tests/Context/CurrentUserTests.cs`
- [ ] All 13+ unit tests passing

**Verification:**

- [ ] TenantInfo validation working
- [ ] UserInfo role helpers functional
- [ ] Test implementations complete

---

### Phase 2.4: Data Access & Patterns

#### ✅ Guide 07: Repository & Unit of Work

**Files to Create:**

- [ ] `src/Idevs/Data/Repositories/IRepository.cs`
- [ ] `src/Idevs/Data/UnitOfWork/IUnitOfWork.cs`
- [ ] `src/Idevs/Data/Repositories/IReadOnlyRepository.cs`
- [ ] `src/Idevs/Data/Repositories/ICustomerRepository.cs`
- [ ] `src/Idevs/Data/Repositories/InMemoryRepository.cs`
- [ ] `src/Idevs/Data/README.md`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/Data/RepositoryTests.cs`
- [ ] All 6+ unit tests passing

**Verification:**

- [ ] Generic repository interface complete
- [ ] InMemoryRepository for testing
- [ ] Expression<Func<>> support
- [ ] Specialized repository examples

---

#### ✅ Guide 08: Domain Events

**Files to Create:**

- [ ] `src/Idevs/Domain/Events/IDomainEvent.cs`
- [ ] `src/Idevs/Domain/Events/IDomainEventHandler.cs`
- [ ] `src/Idevs/Domain/Events/DomainEventBase.cs`
- [ ] `src/Idevs/Domain/Events/IDomainEventDispatcher.cs`
- [ ] `src/Idevs/Domain/Events/InMemoryDomainEventDispatcher.cs`
- [ ] `src/Idevs/Domain/Events/Handlers/DomainEventLoggingHandler.cs`
- [ ] `src/Idevs/Domain/Events/Handlers/CustomerCreatedEventHandler.cs`
- [ ] `src/Idevs/Domain/Events/README.md`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/Domain/DomainEventTests.cs`
- [ ] `tests/Idevs.Tests/Domain/DomainEventDispatcherTests.cs`
- [ ] All 6+ unit tests passing

**Verification:**

- [ ] Event interface with metadata
- [ ] Handler interface with async support
- [ ] Dispatcher implementation working
- [ ] Multiple handlers per event supported

---

#### ✅ Guide 09: Specifications

**Files to Create:**

- [ ] `src/Idevs/Domain/Specifications/ISpecification.cs`
- [ ] `src/Idevs/Domain/Specifications/Specification.cs`
- [ ] `src/Idevs/Domain/Specifications/AndSpecification.cs`
- [ ] `src/Idevs/Domain/Specifications/OrSpecification.cs`
- [ ] `src/Idevs/Domain/Specifications/NotSpecification.cs`
- [ ] `src/Idevs/Domain/Specifications/ReplaceExpressionVisitor.cs`
- [ ] `src/Idevs/Domain/Specifications/Common/TenantSpecification.cs`
- [ ] `src/Idevs/Domain/Specifications/Common/ActiveOnlySpecification.cs`
- [ ] `src/Idevs/Domain/Specifications/CustomerSpecifications.cs`
- [ ] `src/Idevs/Domain/Specifications/README.md`

**Tests to Create:**

- [ ] `tests/Idevs.Tests/Domain/SpecificationTests.cs`
- [ ] All 7+ unit tests passing

**Verification:**

- [ ] And/Or/Not composition working
- [ ] Expression tree compilation correct
- [ ] IsSatisfiedBy for in-memory evaluation
- [ ] Repository integration via extension

---

## Integration Checklist

### Build & Compilation

- [ ] `dotnet restore` completes successfully
- [ ] `dotnet build -c Release` produces no warnings
- [ ] All projects target `net8.0`
- [ ] Nullable reference types enabled

### Testing

- [ ] All unit tests passing: `dotnet test`
- [ ] Test coverage ≥80% on domain logic
- [ ] Test naming convention: `Method_State_Expectation`
- [ ] Shouldly assertions used consistently

### Code Quality

- [ ] No `System.Reflection` usage (per rule)
- [ ] Modern C# features used (records, init, pattern matching)
- [ ] XML documentation on public APIs
- [ ] Private setters on aggregate properties
- [ ] Immutable value objects

### Documentation

- [ ] README.md in each major folder
- [ ] Usage examples documented
- [ ] Common pitfalls sections complete
- [ ] Cross-references between guides working

---

## Exit Criteria

### Must Have (Blocking)

- [ ] ✅ All 9 implementation guides followed
- [ ] ✅ All 90+ unit tests passing
- [ ] ✅ Zero build warnings in Release mode
- [ ] ✅ Entity interfaces support multi-tenancy
- [ ] ✅ Result pattern prevents exception-driven flow
- [ ] ✅ CQRS interfaces defined
- [ ] ✅ Aggregate roots manage domain events
- [ ] ✅ Repository pattern abstracts persistence
- [ ] ✅ Specifications enable composable queries

### Should Have (Non-Blocking)

- [ ] Value object examples (Email, Money, Address)
- [ ] Customer aggregate as reference implementation
- [ ] InMemoryRepository for testing
- [ ] Domain event logging handler
- [ ] Specification composition (And/Or/Not)

### Nice to Have (Future)

- [ ] Additional value objects (PhoneNumber, URL)
- [ ] More aggregate examples (Order, Product)
- [ ] Event sourcing infrastructure
- [ ] Transactional outbox pattern

---

## Verification Commands

### Run All Tests

```bash
dotnet test tests/Idevs.Tests/Idevs.Tests.csproj
```

### Run Specific Test Categories

```bash
# Entity tests
dotnet test --filter "FullyQualifiedName~EntityTests"

# Result tests
dotnet test --filter "FullyQualifiedName~ResultTests"

# CQRS tests
dotnet test --filter "FullyQualifiedName~CqrsContractsTests"

# Domain tests
dotnet test --filter "FullyQualifiedName~Domain"

# Repository tests
dotnet test --filter "FullyQualifiedName~RepositoryTests"
```

### Build Commands

```bash
# Restore packages
dotnet restore

# Build Release
dotnet build -c Release

# Check for outdated packages
dotnet list package --outdated

# Format code
dotnet format
```

### Coverage Report (if configured)

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Common Issues & Solutions

### Issue: Nullable Reference Warnings

**Solution:** Ensure all reference types are properly annotated with `?` for nullable or initialized with `= default!`

```csharp
// Good
public string Name { get; set; } = string.Empty;
public string? OptionalName { get; set; }

// Bad
public string Name { get; set; } // Warning!
```

### Issue: Circular Dependencies

**Solution:** Keep dependencies unidirectional:

- Domain → never depends on infrastructure
- Application → depends on domain contracts
- Infrastructure → depends on domain & application

### Issue: EF Core Proxy Type Comparison

**Solution:** Use `GetRealType()` method in Entity<TKey> base class

### Issue: Expression Tree Composition

**Solution:** Use ReplaceExpressionVisitor for And/Or specifications

---

## Next Steps After Completion

Once Phase 2 is complete:

1. **Validate Integration**
   - Run full test suite
   - Check code coverage
   - Review architectural decisions

2. **Document Deviations**
   - Note any changes from guides
   - Update ADRs if needed
   - Record lessons learned

3. **Proceed to Phase 3**
   - Application layer (handlers, validation)
   - Build on Phase 2 contracts
   - Implement CQRS pipeline

---

## Support & Resources

- **Documentation:** [phase-2-domain-NEW.md](phase-2-domain-NEW.md)
- **References:** [REFERENCES.md](REFERENCES.md)
- **Guides:** [implementation/](implementation/)
- **Original:** [phase-2-domain-ORIGINAL.md](phase-2-domain-ORIGINAL.md)

---

**Status Legend:**

- ⬜ Not Started
- 🔄 In Progress
- ✅ Complete
- ⚠️ Blocked
- ❌ Failed

**Priority Legend:**

- 🔴 Critical - Must complete first
- 🟡 High - Complete soon after critical
- 🟢 Medium - Complete when able
- ⚪ Low - Optional/future

---

*Last Updated: 2025-10-08 | Version: 1.0*
