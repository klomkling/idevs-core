# Phase 1 Platform Foundation - Implementation Decisions

**Date**: 2025-10-07  
**Status**: Active

---

## Overview

This document records all significant design decisions, trade-offs, and deviations from the original specification during Phase 1 implementation.

---

## Decisions Log

### Decision 001: .NET 9.0 Target Framework

**Date**: 2025-10-07  
**Status**: ✅ Approved

**Context**:
- Original Phase 1 plan specified .NET 8.0 LTS
- Current development environment uses .NET SDK 9.0.305
- Need to decide on target framework

**Decision**:
- Target .NET 9.0 instead of .NET 8.0
- No global.json needed (using system-wide SDK)

**Rationale**:
- Aligns with installed SDK version
- .NET 9 provides performance improvements and new language features
- Simplifies development setup (no SDK version switching)
- Can add multi-targeting later if .NET 8 support is required

**Impact**:
- All projects updated to `<TargetFramework>net9.0</TargetFramework>`
- Package versions updated to .NET 9 compatible versions
- CI workflows updated to use .NET 9 SDK

**Risks**:
- Some consumers may still be on .NET 8
- Mitigation: Can add multi-targeting in future if needed

---

### Decision 002: Result Pattern Implementation

**Date**: 2025-10-07  
**Status**: 📋 Pending Implementation

**Context**:
- Need to decide between record vs sealed class for Error type
- Need to decide on deconstruction support

**Options**:
1. **Record** - Immutable by default, built-in equality, concise syntax
2. **Sealed Class** - More explicit, can hide implementation details

**Decision**: TBD during implementation

**Considerations**:
- Records provide cleaner syntax for immutable data
- Records have built-in equality semantics
- Sealed classes provide more control over initialization

---

### Decision 003: Decorator Pipeline Order

**Date**: 2025-10-07  
**Status**: ✅ Approved

**Context**:
- Need to establish standard order for decorator composition

**Decision**:
- Outermost to innermost: **Logging → Validation → Metrics → Core Handler**

**Rationale**:
- Logging first: Captures all events including validation failures
- Validation second: Short-circuits on validation failure before metrics
- Metrics third: Only measures valid commands
- Core handler last: Actual business logic execution

**Example Pipeline**:
```
LoggingDecorator(
  ValidationDecorator(
    MetricsDecorator(
      CoreHandler
    )
  )
)
```

---

### Decision 004: Validation Decorator Behavior

**Date**: 2025-10-07  
**Status**: ✅ Approved

**Context**:
- Need to decide how ValidationDecorator behaves when no validator is registered

**Decision**:
- Skip validation silently if `IValidator<TCommand>` is not registered
- Allow optional per-handler configuration to require validator

**Rationale**:
- Not all commands need validation (e.g., simple queries)
- Explicit opt-in reduces boilerplate
- Consumers can use FluentValidation or custom validators

**API**:
```csharp
// Validation optional (default)
services.AddCommandHandler<CreateOrder, CreateOrderHandler>();

// Validation required (throws if no validator)
services.AddCommandHandler<CreateOrder, CreateOrderHandler>(
    options => options.RequireValidation = true
);
```

---

### Decision 005: Metrics Interface Design

**Date**: 2025-10-07  
**Status**: 📋 Pending Implementation

**Context**:
- Need to design IMetrics interface that works with multiple providers

**Options**:
1. **OpenTelemetry-specific** - Tight coupling to OTel
2. **Generic abstraction** - Provider-agnostic
3. **System.Diagnostics.Metrics** - Built-in .NET metrics

**Decision**: TBD

**Considerations**:
- System.Diagnostics.Metrics is built-in and well-supported
- OpenTelemetry has good tooling ecosystem
- Generic abstraction provides flexibility but adds complexity

---

## Deviations from Original Specification

### Deviation 001: No global.json

**Original Spec**: Create global.json to pin .NET SDK version  
**Actual**: No global.json created  
**Reason**: Using system-wide .NET 9 SDK, no pinning needed

---

## Trade-offs

### Trade-off 001: Explicit Registration vs Assembly Scanning

**Chosen**: Explicit Registration  
**Alternative**: Assembly Scanning (Scrutor/Autofac)

**Pros of Explicit**:
- ✅ No reflection overhead
- ✅ Clear dependency graph
- ✅ IDE navigation works (F12)
- ✅ Compile-time safety

**Cons of Explicit**:
- ❌ More verbose
- ❌ Manual maintenance required

**Justification**: Aligns with "Explicit Over Magic" design principle from Phase 0

---

### Trade-off 002: Microsoft.Extensions.DependencyInjection vs Autofac

**Chosen**: Microsoft.Extensions.DependencyInjection (MEDI)  
**Alternative**: Autofac with decorator support

**Pros of MEDI**:
- ✅ Zero external dependencies
- ✅ Works with any .NET app out of the box
- ✅ Microsoft guarantee of support
- ✅ Familiar to all .NET developers

**Cons of MEDI**:
- ❌ No built-in decorator pattern
- ❌ More verbose factory registrations

**Justification**: Lower adoption barrier, zero dependencies, future-proof

---

## Open Questions

### Question 001: Multi-targeting Support

**Question**: Should we multi-target .NET 8 and .NET 9?

**Considerations**:
- Pro: Broader compatibility
- Con: Increased complexity
- Pro: Enterprise users may be on .NET 8 LTS

**Status**: Deferred to post-Phase 1

---

### Question 002: Query Handler Decorators

**Question**: Do we need separate decorators for query handlers?

**Considerations**:
- Queries may not need validation (read-only)
- Logging and metrics still useful
- Could reuse command decorators with minimal changes

**Status**: Deferred to testing phase

---

## References

- [Phase 1 Platform Plan](../../phase-1-platform.md)
- [DI Strategy Summary](../../DI-STRATEGY-SUMMARY.md)
- [AGENTS.md Repository Guidelines](../../../../../AGENTS.md)

---

**Last Updated**: 2025-10-07

---

## Phase 1 Status: ✅ COMPLETE

**Completion Date**: 2025-10-08  
**Package Version**: 0.1.0-feat-phase-1-platform-foundation  
**Test Coverage**: 90.1% line, 70% branch

---

## Phase 2 Planning - Next Steps

### High Priority Items

1. **Complete Decorator Infrastructure**
   - Validation decorators for `ICommandHandler<TCommand, TResponse>`
   - Validation decorators for `IQueryHandler<TQuery, TResponse>`
   - Logging decorators for commands with response and queries
   - Metrics decorators for commands with response and queries

2. **Unit of Work Implementation**
   - EF Core-based IUnitOfWork implementation
   - Transaction management support
   - Integration with DbContext

3. **Multi-Tenancy Implementation**
   - HTTP context tenant resolver
   - JWT token tenant resolver
   - Tenant isolation middleware

### Medium Priority Items

4. **Additional Decorators**
   - Retry decorator (Polly integration)
   - Circuit breaker decorator
   - Caching decorator for queries
   - Authorization decorator

5. **Real Metrics Integration**
   - Replace NoOpMetrics with OpenTelemetry
   - Prometheus exporter
   - Custom dimensions and tags

### Low Priority Items

6. **Performance Optimization**
   - BenchmarkDotNet performance suite
   - Memory allocation profiling
   - Decorator overhead optimization

7. **Advanced Documentation**
   - Migration guide from MediatR
   - Real-world integration examples
   - Best practices guide

---

## Issues to Create

1. Implement validation decorators for commands with response and queries (#TBD)
2. Implement logging decorators for commands with response and queries (#TBD)
3. Implement metrics decorators for commands with response and queries (#TBD)
4. Create EF Core UnitOfWork implementation (#TBD)
5. Add retry decorator with Polly (#TBD)
6. Add circuit breaker decorator (#TBD)
7. Add caching decorator for queries (#TBD)
8. Implement tenant resolvers (HTTP, JWT, DB) (#TBD)
9. Create migration guide from MediatR (#TBD)
10. Add performance benchmark suite (#TBD)

