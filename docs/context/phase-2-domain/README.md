# Phase 2: Domain & Contracts

> **Building Block:** Domain layer and foundational contracts for the Idevs framework  
> **Status:** Documentation Complete ✅  
> **Last Updated:** 2025-10-08

## 📖 Overview

Phase 2 establishes the domain foundation with DDD patterns, CQRS contracts, multi-tenancy support, and composable query specifications. This phase is critical as all application logic builds on these abstractions.

**Documentation Stats:**

- 📄 **12 documents** (10,033 lines total)
- 📚 **9 implementation guides** (6,151 lines)
- ✅ **90+ unit tests** defined
- 🎯 **22-28 hours** estimated implementation time

## 🚀 Quick Start

### New to Phase 2?

1. **Read:** [phase-2-domain-NEW.md](phase-2-domain-NEW.md) - High-level overview
2. **Review:** [COMPLETION-GUIDE.md](COMPLETION-GUIDE.md) - Implementation checklist
3. **Follow:** [implementation/](implementation/) - Step-by-step guides

### Ready to Implement?

Start with the critical path:

1. [Entity Interfaces](implementation/01-entity-interfaces.md) → Base contracts
2. [Result Patterns](implementation/03-result-pattern-core.md) → Error handling
3. [CQRS Contracts](implementation/04-cqrs-contracts-basic.md) → Command/Query separation

## 📚 Documentation Structure

### Core Documents

| Document | Purpose | Lines | Audience |
|----------|---------|-------|----------|
| **[phase-2-domain-NEW.md](phase-2-domain-NEW.md)** | Main overview & navigation | 133 | Everyone |
| **[COMPLETION-GUIDE.md](COMPLETION-GUIDE.md)** | Implementation checklist | 433 | Developers |
| **[REFERENCES.md](REFERENCES.md)** | Learning resources | 393 | Everyone |

### Implementation Guides

All guides follow consistent structure: Prerequisites → Objectives → Steps → Tests → Verification → Pitfalls

| Guide | Topic | Lines | Priority | Time |
|-------|-------|-------|----------|------|
| [01-entity-interfaces.md](implementation/01-entity-interfaces.md) | Entity contracts | 390 | 🔴 Critical | 2-3h |
| [02-value-objects.md](implementation/02-value-objects-base.md) | Value objects | 742 | 🟡 High | 2-3h |
| [03-result-patterns.md](implementation/03-result-pattern-core.md) | Error handling | 630 | 🔴 Critical | 3-4h |
| [04-cqrs-contracts.md](implementation/04-cqrs-contracts-basic.md) | CQRS interfaces | 626 | 🔴 Critical | 2-3h |
| [05-aggregates-entities.md](implementation/05-aggregates-base.md) | Domain models | 863 | 🔴 Critical | 3-4h |
| [06-tenant-user-context.md](implementation/06-tenant-context.md) | Multi-tenancy | 738 | 🔴 Critical | 2-3h |
| [07-repository-uow.md](implementation/07-repository-pattern.md) | Data access | 672 | 🔴 Critical | 3-4h |
| [08-domain-events.md](implementation/08-domain-events-contracts.md) | Event-driven | 690 | 🟡 High | 2-3h |
| [09-specifications.md](implementation/09-specifications-pattern.md) | Query patterns | 800 | 🟢 Medium | 2-3h |

## 🎯 What You'll Build

### Core Interfaces

```csharp
IEntity<TKey>         // Base entity with identity
IAuditable            // Created/Updated tracking
ISoftDeletable        // Soft delete support
ITenant               // Multi-tenant isolation
```

### Value Objects

```csharp
Email                 // Validated email addresses
Money                 // Currency-aware amounts
Address               // Multi-field addresses
DateRange             // Temporal ranges
```

### Result Pattern

```csharp
Result<T>             // Success/failure monads
Error                 // Structured errors
PagedResult<T>        // Pagination support
DomainErrors          // Common error codes
```

### CQRS Contracts

```csharp
ICommand<TResponse>   // Write operations
IQuery<TResponse>     // Read operations
ICommandHandler       // Command processors
IQueryHandler         // Query processors
```

### Aggregates & Entities

```csharp
Entity<TKey>          // Identity-based equality
AggregateRoot<TKey>   // Domain event management
Guard                 // Invariant validation
```

### Multi-Tenancy

```csharp
ITenantContext        // Current tenant access
ICurrentUser          // Authenticated user
TenantInfo            // Tenant metadata
UserInfo              // User metadata
```

### Data Access

```csharp
IRepository<T, TKey>  // Generic repository
IUnitOfWork           // Transaction coordination
ISpecification<T>     // Composable queries
```

### Domain Events

```csharp
IDomainEvent          // Event marker
IDomainEventHandler   // Event processors
IDomainEventDispatcher // Event publishing
```

## 🏗️ Architecture Principles

### Domain-Driven Design (DDD)

- **Ubiquitous Language:** Align code with business terminology
- **Bounded Contexts:** Clear domain boundaries
- **Aggregates:** Consistency boundaries with root entities
- **Value Objects:** Immutable, validated domain primitives
- **Domain Events:** Decouple business logic

### CQRS (Command Query Responsibility Segregation)

- **Separation:** Commands (write) vs Queries (read)
- **Composability:** Decorators for cross-cutting concerns
- **Testability:** Mock handlers, not services

### Railway-Oriented Programming

- **Explicit Errors:** Result<T> instead of exceptions
- **Composition:** Map, Bind, Match for functional flow
- **Type Safety:** Compiler-enforced error handling

### Multi-Tenancy

- **Isolation:** Tenant ID on all entities
- **Security:** Row-level filtering at query level
- **Context:** Ambient tenant/user access

## 📝 Usage Examples

### Creating Aggregates

```csharp
var result = Customer.Create(
    tenantId,
    firstName: "John",
    lastName: "Doe",
    email: "john@example.com"
);

if (result.IsFailure)
    return result.Error;

await _repository.AddAsync(result.Value);
await _unitOfWork.CommitAsync(); // Dispatches domain events
```

### CQRS Handlers

```csharp
public class CreateCustomerCommandHandler 
    : ICommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var customer = Customer.Create(/* ... */);
        if (customer.IsFailure) return customer.Error;
        
        await _repository.AddAsync(customer.Value);
        return customer.Value.Id;
    }
}
```

### Specifications

```csharp
var spec = new CustomerSpecifications.ByTenant(tenantId)
    .And(new CustomerSpecifications.IsActive())
    .And(new CustomerSpecifications.ByNamePattern("john"));

var customers = await _repository.FindAsync(spec);
```

## ✅ Success Criteria

### Must Have (Blocking)

- [ ] All 9 implementation guides followed
- [ ] All 90+ unit tests passing
- [ ] Zero build warnings in Release mode
- [ ] Entity interfaces support multi-tenancy
- [ ] Result pattern prevents exception-driven flow
- [ ] CQRS interfaces defined
- [ ] Aggregate roots manage domain events
- [ ] Repository pattern abstracts persistence
- [ ] Specifications enable composable queries

### Quality Metrics

- **Test Coverage:** ≥80% on domain logic
- **Build Time:** <30s for clean build
- **Code Quality:** No `System.Reflection` usage
- **Documentation:** XML docs on all public APIs

## 🧪 Testing Strategy

### Unit Tests

```bash
# Run all tests
dotnet test

# Run specific categories
dotnet test --filter "FullyQualifiedName~Domain"
dotnet test --filter "FullyQualifiedName~ResultTests"
dotnet test --filter "FullyQualifiedName~CqrsContractsTests"
```

### Test Doubles

- **InMemoryRepository** - Database-free testing
- **TestTenantContext** - Tenant context for tests
- **TestCurrentUser** - User context for tests

## 📦 Deliverables

### Source Files

```
src/Idevs/
├── Contracts/Entities/          # Entity interfaces
├── Results/                     # Result pattern
├── CQRS/                        # Command/Query contracts
├── Domain/
│   ├── Entities/                # Entity base classes
│   ├── ValueObjects/            # Value object types
│   ├── Events/                  # Domain events
│   ├── Guards/                  # Validation helpers
│   └── Specifications/          # Query specifications
├── Context/                     # Tenant/User context
└── Data/
    ├── Repositories/            # Repository interfaces
    └── UnitOfWork/              # UnitOfWork interface
```

### Test Files

```
tests/Idevs.Tests/
├── Contracts/                   # Interface tests
├── Results/                     # Result pattern tests
├── CQRS/                        # CQRS tests
├── Domain/                      # Domain tests
├── Context/                     # Context tests
└── Data/                        # Repository tests
```

## 🔗 Dependencies

### Prerequisites

- **Phase 0:** Design principles & ADRs
- **Phase 1:** Solution structure & build config

### Enables

- **Phase 3:** Application layer (handlers, validation)
- **Phase 4:** Web layer (API controllers, mapping)
- **Phase 5:** Infrastructure (EF Core, database)

## 🎓 Learning Path

### Beginner → Intermediate

1. Read Eric Evans' DDD (Blue Book) - Chapters 5-7
2. Watch: Greg Young on CQRS
3. Follow implementation guides in order
4. Study Customer aggregate example

### Intermediate → Advanced

1. Read Vaughn Vernon's IDDD (Red Book) - Chapters 5-12
2. Study railway-oriented programming
3. Implement custom aggregates
4. Explore event sourcing patterns

### Resources

See [REFERENCES.md](REFERENCES.md) for curated links to:

- Books & articles
- Video courses
- Conference talks
- Code examples
- Tools & libraries

## ⚠️ Common Pitfalls

### Don't

- ❌ Use `System.Reflection` (violates framework rules)
- ❌ Make aggregates with public setters
- ❌ Return `IQueryable` from repositories
- ❌ Create repositories for child entities
- ❌ Forget to call `UnitOfWork.CommitAsync()`
- ❌ Use exceptions for expected errors
- ❌ Modify aggregates in domain event handlers

### Do

- ✅ Use Result<T> for expected failures
- ✅ Encapsulate invariants in aggregates
- ✅ Raise domain events for state changes
- ✅ Test specifications independently
- ✅ Use value objects to prevent primitive obsession
- ✅ Keep aggregates small and focused

## 🔄 Next Steps

### After Phase 2 Completion

1. **Validate Integration**
   - Run full test suite
   - Check code coverage
   - Review architectural decisions

2. **Document Learnings**
   - Note deviations from guides
   - Update ADRs if needed
   - Share with team

3. **Proceed to Phase 3**
   - Application layer implementation
   - Build on Phase 2 contracts
   - Implement CQRS pipeline with MediatR

## 📞 Support

### Questions?

- Review [REFERENCES.md](REFERENCES.md) for external resources
- Check implementation guides for specific topics
- Consult original [phase-2-domain.md](phase-2-domain.md) for additional context

### Contributing

Found an issue or improvement?

1. Document the problem
2. Propose a solution
3. Update relevant guides
4. Submit for review

## 📊 Progress Tracking

Use [COMPLETION-GUIDE.md](COMPLETION-GUIDE.md) to track:

- [ ] Phase 2.1: Core Interfaces & Patterns
- [ ] Phase 2.2: Value Objects & Aggregates
- [ ] Phase 2.3: Multi-Tenancy & Context
- [ ] Phase 2.4: Data Access & Patterns

**Status Indicators:**

- ⬜ Not Started
- 🔄 In Progress
- ✅ Complete
- ⚠️ Blocked

---

## 📈 Documentation Metrics

- **Total Lines:** 10,033
- **Implementation Guides:** 9 (6,151 lines)
- **Code Examples:** 100+
- **Unit Tests Defined:** 90+
- **Estimated Implementation:** 22-28 hours
- **Target Coverage:** ≥80%

---

**Quick Links:**

- [Overview](phase-2-domain-NEW.md)
- [Completion Guide](COMPLETION-GUIDE.md)
- [References](REFERENCES.md)
- [Implementation Guides](implementation/)

---

*Phase 2 Documentation Complete - Ready for Implementation*  
*Version 1.0 | Last Updated: 2025-10-08*
