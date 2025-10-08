# File Split Strategy

## 02-value-objects.md (742 lines)
**Split Point:** After Money implementation (~line 400)
- **02-value-objects-base.md** (~370 lines)
  - Overview, Prerequisites, Objectives
  - ValueObject base class
  - Email value object
  - Money value object
- **02-value-objects-examples.md** (~370 lines)
  - Address value object
  - DateRange value object
  - Tests
  - Verification, Usage Examples, Pitfalls

## 03-result-patterns.md (630 lines)
**Split Point:** After Result<T> implementation (~line 315)
- **03-result-pattern-core.md** (~315 lines)
  - Overview through Result<T> and Error classes
- **03-result-pattern-extensions.md** (~315 lines)
  - PagedResult<T>, ValidationResult
  - Railway operations (Map, Bind, Match)
  - Tests

## 04-cqrs-contracts.md (626 lines)
**Split Point:** After handler interfaces (~line 310)
- **04-cqrs-contracts-basic.md** (~310 lines)
  - ICommand, IQuery, handler interfaces
- **04-cqrs-contracts-advanced.md** (~310 lines)
  - Decorators, pipeline behaviors
  - Examples, tests

## 05-aggregates-entities.md (863 lines) ✅ IN PROGRESS
**Split Point:** After Guard clauses (~line 266)
- **05-aggregates-base.md** (290 lines) ✅ CREATED
  - Entity<TKey>, AggregateRoot<TKey>, Guard
- **05-aggregates-examples.md** (~570 lines) - NEEDS CREATION
  - Customer aggregate, events, tests

## 06-tenant-user-context.md (738 lines)
**Split Point:** After ITenantContext (~line 370)
- **06-tenant-context.md** (~370 lines)
  - TenantInfo, ITenantContext, implementation
- **06-user-context.md** (~370 lines)
  - UserInfo, ICurrentUser, implementation, tests

## 07-repository-uow.md (672 lines)
**Split Point:** After IRepository (~line 335)
- **07-repository-pattern.md** (~335 lines)
  - IRepository<T, TKey>, spec integration
- **07-unit-of-work.md** (~335 lines)
  - IUnitOfWork, transactions, event dispatch, tests

## 08-domain-events.md (690 lines)
**Split Point:** After interfaces (~line 345)
- **08-domain-events-contracts.md** (~345 lines)
  - IDomainEvent, IDomainEventHandler, DomainEventBase
- **08-domain-events-impl.md** (~345 lines)
  - InMemoryDomainEventDispatcher, handlers, tests

## 09-specifications.md (800 lines)
**Split Point:** After base class (~line 400)
- **09-specifications-pattern.md** (~400 lines)
  - ISpecification<T>, base, And/Or/Not
- **09-specifications-examples.md** (~400 lines)
  - Domain specs, paging/sorting, complex queries, tests

