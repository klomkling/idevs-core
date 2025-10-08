# Phase 3: Application Layer — Completion Guide

**Phase**: 3 - Application Layer  
**Status**: Implementation Ready  
**Last Updated**: 2025-01-08

---

## Overview

This guide provides a comprehensive checklist for implementing the Application Layer of the Idevs framework. Use it to track progress and verify that all components are correctly implemented and tested.

---

## Implementation Phases

### Phase 1: Core Infrastructure (Required)

#### 1.1 Install Dependencies
```bash
dotnet add package MediatR --version 12.2.0
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection --version 11.1.0
dotnet add package FluentValidation --version 11.9.0
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.9.0
```

- [ ] MediatR installed
- [ ] FluentValidation installed
- [ ] All packages restored successfully
- [ ] No version conflicts

#### 1.2 Core Interfaces
- [ ] Create `ICommand<TResponse>` marker interface
- [ ] Create `IQuery<TResponse>` marker interface
- [ ] Create `ICommandHandler<TCommand, TResponse>` interface
- [ ] Create `IQueryHandler<TQuery, TResponse>` interface
- [ ] All interfaces inherit from MediatR contracts correctly
- [ ] XML documentation added to all public interfaces

#### 1.3 Dependency Injection Setup
- [ ] Create `DependencyInjection.cs` with `AddApplication()` extension
- [ ] Register MediatR with assembly scanning
- [ ] Register FluentValidation validators
- [ ] Configure behavior pipeline order correctly
- [ ] Test DI registration with simple handler

---

### Phase 2: Pipeline Behaviors (Required)

#### 2.1 Logging Behavior
- [ ] Implement `LoggingBehavior<TRequest, TResponse>`
- [ ] Log request name and correlation ID on entry
- [ ] Log execution time on completion
- [ ] Log errors with full context
- [ ] Register as first behavior in pipeline
- [ ] Test with sample command/query

#### 2.2 Validation Behavior
- [ ] Implement `ValidationBehavior<TRequest, TResponse>`
- [ ] Inject `IEnumerable<IValidator<TRequest>>`
- [ ] Run all validators in parallel
- [ ] Collect and group validation failures
- [ ] Return `Result.Failure` with validation errors
- [ ] Register after logging, before authorization
- [ ] Test with valid and invalid commands

#### 2.3 Authorization Behavior
- [ ] Create `ICurrentUserService` interface
- [ ] Implement `AuthorizationBehavior<TRequest, TResponse>`
- [ ] Check authentication requirement
- [ ] Check role requirements
- [ ] Check permission requirements
- [ ] Return appropriate error (401 Unauthorized / 403 Forbidden)
- [ ] Register after validation, before transaction
- [ ] Test authentication and authorization scenarios

#### 2.4 Transaction Behavior
- [ ] Implement `TransactionBehavior<TRequest, TResponse>`
- [ ] Detect if request is a command
- [ ] Begin transaction before handler
- [ ] Commit transaction on success
- [ ] Rollback on failure
- [ ] Register after authorization, before caching
- [ ] Test with database operations

#### 2.5 Error Handling Behavior
- [ ] Implement `ErrorHandlingBehavior<TRequest, TResponse>`
- [ ] Catch unexpected exceptions
- [ ] Map to appropriate Result<T> failures
- [ ] Log errors with context
- [ ] Register as last behavior
- [ ] Test exception handling

---

### Phase 3: Sample Implementations (Required)

#### 3.1 Sample Command
- [ ] Create `CreateProductCommand` record
- [ ] Implement `CreateProductCommandHandler`
- [ ] Create `CreateProductCommandValidator`
- [ ] Add [Authorize] attribute with permissions
- [ ] Test handler in isolation
- [ ] Test full pipeline with MediatR

#### 3.2 Sample Query
- [ ] Create `GetProductByIdQuery` record
- [ ] Implement `GetProductByIdQueryHandler`
- [ ] Create DTO for response
- [ ] Add optional [Authorize] attribute
- [ ] Test handler in isolation
- [ ] Test full pipeline with MediatR

#### 3.3 Controller Integration
- [ ] Inject `IMediator` in controller
- [ ] Replace direct handler calls with `_mediator.Send()`
- [ ] Map Result<T> to HTTP responses (200, 400, 401, 403, 404)
- [ ] Test API endpoints

---

### Phase 4: Advanced Features (Optional)

#### 4.1 Caching Behavior
- [ ] Implement `CachingBehavior<TRequest, TResponse>`
- [ ] Only cache queries (not commands)
- [ ] Generate cache keys from query properties
- [ ] Use `IDistributedCache` abstraction
- [ ] Implement cache invalidation strategy
- [ ] Configure cache expiration
- [ ] Test with Redis or in-memory cache

#### 4.2 Tenant Isolation
- [ ] Create `ITenantContext` interface
- [ ] Implement `TenantIsolationBehavior`
- [ ] Create tenant-aware repository base class
- [ ] Always filter queries by tenant ID
- [ ] Test cross-tenant data isolation
- [ ] Verify no tenant leakage

#### 4.3 Performance Monitoring
- [ ] Add `PerformanceBehavior<TRequest, TResponse>`
- [ ] Log slow requests (> threshold)
- [ ] Emit metrics to monitoring system
- [ ] Track handler execution times
- [ ] Set up alerting for performance degradation

---

## Verification Checklist

### Code Quality
- [ ] All code follows .NET naming conventions
- [ ] Nullable reference types enabled and warnings resolved
- [ ] No compiler warnings
- [ ] XML documentation on all public APIs
- [ ] Code formatted with `dotnet format`
- [ ] No code smells (SonarQube/Analyzer)

### Testing
- [ ] Unit tests for all handlers (minimum 80% coverage)
- [ ] Unit tests for all validators
- [ ] Unit tests for all behaviors
- [ ] Integration tests for full pipeline
- [ ] All tests passing
- [ ] Test naming follows `Method_Scenario_ExpectedResult` pattern

### Documentation
- [ ] Handler naming conventions documented
- [ ] Validation patterns documented
- [ ] Permission constants documented
- [ ] README updated with usage examples
- [ ] Architecture diagrams up to date

### Security
- [ ] Authorization on all sensitive commands/queries
- [ ] Tenant isolation tested thoroughly
- [ ] No hardcoded credentials
- [ ] Input validation on all commands
- [ ] Sensitive data not logged
- [ ] SQL injection prevention (parameterized queries)

### Performance
- [ ] Async/await used consistently
- [ ] No blocking calls in async methods
- [ ] Efficient query patterns (no N+1)
- [ ] Pagination on list queries
- [ ] Caching on expensive read operations
- [ ] Connection pooling configured

---

## Testing Strategy

### Unit Tests (Required)

**Handler Tests**:
```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsSuccess()
{
    // Arrange: Mock dependencies
    // Act: Call handler.Handle()
    // Assert: Verify result and mock interactions
}
```

**Validator Tests**:
```csharp
[Fact]
public void Validate_InvalidInput_ReturnsFailure()
{
    // Arrange: Create invalid command
    // Act: validator.Validate(command)
    // Assert: result.IsValid == false
}
```

**Behavior Tests**:
```csharp
[Fact]
public async Task Handle_Condition_BehavesCorrectly()
{
    // Arrange: Setup behavior and mock next delegate
    // Act: behavior.Handle(request, next, ct)
    // Assert: Verify next called or error returned
}
```

### Integration Tests (Recommended)

**Full Pipeline Tests**:
```csharp
[Fact]
public async Task Send_InvalidCommand_ReturnsValidationError()
{
    // Arrange: Setup real MediatR with all behaviors
    // Act: var result = await mediator.Send(command)
    // Assert: Verify full pipeline behavior
}
```

### Performance Tests (Optional)

**Load Tests**:
- Test handler throughput (requests/second)
- Measure 95th percentile latency
- Verify no memory leaks under load
- Test concurrent request handling

---

## Common Implementation Issues

### Issue 1: Behavior Order Incorrect
**Symptom**: Validation runs after transaction starts  
**Solution**: Register behaviors in correct order (see Guide 2)

### Issue 2: Validators Not Found
**Symptom**: `IEnumerable<IValidator<T>>` is empty  
**Solution**: Ensure `services.AddValidatorsFromAssembly()` is called

### Issue 3: Authorization Always Fails
**Symptom**: All requests return 401/403  
**Solution**: Verify `ICurrentUserService` correctly extracts user from context

### Issue 4: Transaction Not Rolling Back
**Symptom**: Partial updates on failure  
**Solution**: Ensure `TransactionBehavior` wraps handler and `SaveChangesAsync` is only called on success

### Issue 5: Tenant Data Leakage
**Symptom**: Users see data from other tenants  
**Solution**: Always filter by `TenantId` in repository queries

---

## Performance Benchmarks

### Target Metrics (Per Handler)

| Metric | Target | Acceptable | Action Required |
|--------|--------|------------|-----------------|
| P50 Latency | < 50ms | < 100ms | > 100ms |
| P95 Latency | < 200ms | < 500ms | > 500ms |
| P99 Latency | < 500ms | < 1000ms | > 1000ms |
| Throughput | > 1000 req/s | > 500 req/s | < 500 req/s |
| Memory | < 100 MB | < 200 MB | > 200 MB |
| CPU | < 30% | < 50% | > 50% |

### Measurement Tools
- BenchmarkDotNet for microbenchmarks
- K6 or Artillery for load testing
- Application Insights for production monitoring
- Prometheus + Grafana for metrics

---

## Deployment Checklist

### Pre-Deployment
- [ ] All tests passing in CI/CD
- [ ] Code review completed and approved
- [ ] Security scan passed (no high/critical vulnerabilities)
- [ ] Database migrations applied to staging
- [ ] Configuration validated (connection strings, API keys, etc.)
- [ ] Load testing completed successfully

### Deployment
- [ ] Deploy to staging first
- [ ] Smoke tests passed on staging
- [ ] Blue-green deployment or canary release
- [ ] Monitor error rates during rollout
- [ ] Rollback plan tested and ready

### Post-Deployment
- [ ] Verify all endpoints responding
- [ ] Check error logs for new issues
- [ ] Monitor performance metrics
- [ ] Validate authorization working correctly
- [ ] Confirm database connection pooling healthy

---

## Maintenance Tasks

### Weekly
- [ ] Review error logs
- [ ] Check performance metrics
- [ ] Monitor slow query logs
- [ ] Review failed authorization attempts

### Monthly
- [ ] Update NuGet packages (patch versions)
- [ ] Review and optimize slow handlers
- [ ] Analyze cache hit rates
- [ ] Review audit logs for anomalies

### Quarterly
- [ ] Major NuGet package updates
- [ ] Refactor high-complexity handlers
- [ ] Update documentation
- [ ] Review and update permissions

---

## Support & Resources

### Documentation
- Phase 3 Guides: `docs/context/phase-3-application/`
- Phase 2 (Domain): `docs/context/phase-2-domain/`
- API Documentation: `/api/swagger`

### External Resources
- MediatR: https://github.com/jbogard/MediatR
- FluentValidation: https://docs.fluentvalidation.net
- CQRS Pattern: https://martinfowler.com/bliki/CQRS.html

### Team Contacts
- Architecture Questions: See team wiki
- Security Concerns: See security team channel
- Performance Issues: See DevOps team

---

## Success Criteria

Phase 3 is considered **complete** when:

✅ All required checklist items above are checked  
✅ Minimum 80% test coverage achieved  
✅ All tests passing in CI/CD  
✅ Code review approved by 2+ team members  
✅ Documentation updated and reviewed  
✅ Performance benchmarks met  
✅ Security scan passed  
✅ Successfully deployed to staging  
✅ No critical bugs in production for 2 weeks

---

**Congratulations!** Once all items are complete, you have a production-ready Application Layer following industry best practices.

---

**Next Phase**: Phase 4 - Infrastructure Layer (Data Access, Caching, External Services)

---

**Last Updated**: 2025-01-08  
**Maintained By**: Idevs Framework Team
