# Glossary

**Document Owner**: Platform Team  
**Last Updated**: 2025-10-04  
**Status**: Active Development

## Purpose

This glossary standardizes terminology used throughout the **Idevs** framework (repo: `idevs-core`) to ensure consistent communication among stakeholders, developers, and documentation. Terms are organized by domain for easy reference.

---

## CQRS Terms

| Term | Definition |
|------|------------|
| **CQRS** | Command Query Responsibility Segregation; architectural pattern separating read (query) and write (command) models to optimize behavior, scalability, and maintainability. |
| **Command** | An operation that changes system state (write). Commands are executed through the command bus with tenant and audit metadata. Examples: `CreateOrderCommand`, `UpdateInventoryCommand`. |
| **Query** | A read-only request returning data without side effects. Queries are dispatched through the query bus with caching and projection support. Examples: `GetOrderByIdQuery`, `SearchProductsQuery`. |
| **Command Handler** | Component responsible for executing a specific command. Implements `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResponse>`. Encapsulates business logic and validation. |
| **Query Handler** | Component responsible for executing a specific query. Implements `IQueryHandler<TQuery, TResponse>`. Focuses on efficient data retrieval and projection. |
| **Decorator Pattern** | Design pattern wrapping handlers with additional behavior (validation, logging, caching) without modifying the handler itself. Enables composable cross-cutting concerns. |
| **Request** | Generic term for either a command or query. |
| **Response** | Result returned from command or query execution. Often wrapped in a `Result<T>` pattern for consistent error handling. |
| **Idempotency Key** | Unique identifier ensuring duplicate command submissions produce the same result. Prevents double-processing of commands. |

---

## Domain-Driven Design (DDD) Terms

| Term | Definition |
|------|------------|
| **Bounded Context** | Logical boundary within which a domain model is defined and applicable. Represents a cohesive subsystem with clear interfaces. Examples: `Inventory`, `Billing`, `Orders`. |
| **Aggregate** | Cluster of domain objects (entities and value objects) treated as a single unit for data consistency. Has a root entity (aggregate root) that enforces invariants. |
| **Aggregate Root** | The main entity within an aggregate responsible for maintaining consistency and acting as the entry point for all operations on the aggregate. |
| **Entity** | Domain object with a unique identity that persists over time. Equality based on ID, not property values. Examples: `Order`, `Customer`, `Product`. |
| **Value Object** | Immutable domain object defined by its attributes rather than identity. Used for descriptive aspects of the domain. Examples: `Money`, `Address`, `DateRange`. |
| **Repository** | Pattern providing collection-like interface for accessing aggregates. Abstracts data access and persistence concerns from domain logic. |
| **Unit of Work** | Pattern coordinating changes to multiple aggregates and ensuring transactional consistency. Tracks changes and commits them atomically. |
| **Domain Event** | Notification that something significant happened in the domain. Used for eventual consistency and inter-aggregate communication. Examples: `OrderPlacedEvent`, `InventoryDepletedEvent`. |
| **Specification** | Pattern encapsulating query criteria in a reusable, composable object. Enables complex query logic to be defined in the domain layer. |
| **Invariant** | Business rule that must always be true for an aggregate to be in a valid state. Enforced by the aggregate root. |

---

## Multi-Tenancy Terms

| Term | Definition |
|------|------------|
| **Tenant** | Isolated customer or organization using the system. Each tenant's data is logically or physically separated from other tenants. |
| **Tenant Context** | Immutable structure containing tenant identifier, tier, correlation IDs, and security attributes. Propagated across request pipeline and handlers. |
| **Tenant Resolution** | Process of identifying the tenant for a given request. Strategies include header-based, subdomain-based, token claim-based, or explicit parameter. |
| **Tenant Identifier** | Unique identifier (typically GUID or string) representing a tenant. Often named `tenant_id` in database schemas. |
| **Tiered Multi-Tenant** | Architecture where multiple tenants share infrastructure (database, compute) with logical isolation (row-level security). Cost-optimized with configurable resource limits per tier. |
| **Dedicated Tenant** | Architecture where a tenant has isolated infrastructure (dedicated database or schema). Provides strongest isolation and custom SLA guarantees. |
| **Row-Level Security (RLS)** | Database feature (e.g., PostgreSQL RLS) automatically filtering queries to include only the current tenant's data. Enforced at the database level. |
| **Tenant Filter** | Application-level query filter ensuring all data access is scoped to the current tenant. Applied automatically by repositories. |
| **Tenant Tier** | Classification of tenant subscription level (e.g., Free, Standard, Premium, Enterprise). Determines resource allocation, features, and SLAs. |
| **Tenant Isolation** | Practice of ensuring complete data and resource separation between tenants to prevent cross-tenant data leakage. |

---

## Persistence Terms

| Term | Definition |
|------|------------|
| **Soft Delete** | Strategy marking entities as deleted (e.g., `IsDeleted = true`, `DeletedAt` timestamp) without physically removing data. Enables audit trails and recovery. |
| **Hard Delete** | Permanent physical removal of data from the database. Typically reserved for purge operations after retention period. |
| **Audit Log** | Immutable record of all changes to entities, including who made the change, when, and what changed. Used for compliance, debugging, and forensics. |
| **Audit Trail** | Complete history of all operations performed on an entity or within a tenant. Includes before/after snapshots of data changes. |
| **Outbox Pattern** | Persistence technique storing domain events in a transactional outbox table alongside entity changes. Ensures reliable event publication. |
| **Inbox Pattern** | Persistence technique storing incoming messages for idempotent processing. Prevents duplicate message handling. |
| **Idempotency** | Property ensuring an operation produces the same result when executed multiple times. Critical for reliability in distributed systems. |
| **Optimistic Concurrency** | Concurrency control strategy using version numbers or ETags to detect conflicts. Transaction fails if another process modified the data. |
| **Pessimistic Concurrency** | Concurrency control strategy using locks to prevent simultaneous modifications. More conservative but can reduce throughput. |
| **Migration** | Versioned script applying schema changes to the database. Managed by EF Core migrations or similar tooling. |
| **Seeding** | Process of populating database with initial or test data. Often part of migration or deployment process. |
| **Change Tracking** | Mechanism monitoring entity modifications to determine what needs to be persisted. EF Core provides automatic change tracking. |

---

## Observability Terms

| Term | Definition |
|------|------------|
| **Metrics** | Numeric measurements describing system behavior over time. Examples: request count, error rate, latency percentiles, cache hit ratio. |
| **Traces** | Records of request flows through distributed systems. Capture timing, dependencies, and context as requests traverse components. |
| **Logs** | Text-based records of events occurring in the system. Structured logs include machine-readable fields (JSON) for querying and analysis. |
| **Correlation ID** | Unique identifier assigned to a request and propagated across all components and services. Enables tracing a single request end-to-end. |
| **Span** | Single unit of work within a trace. Represents an operation (e.g., database query, HTTP call) with start time, duration, and metadata. |
| **Trace Context** | W3C standard for propagating trace information (trace ID, span ID, flags) across service boundaries. |
| **Structured Logging** | Logging approach using key-value pairs instead of plain text. Enables powerful querying and analysis. Example: `{"Level":"Info","TenantId":"abc","Message":"Order created"}`. |
| **Log Level** | Severity of a log event: Trace, Debug, Information, Warning, Error, Critical. Used for filtering and alerting. |
| **Event ID** | Unique identifier for a specific type of log event. Enables filtering and categorization. |
| **PII Redaction** | Process of removing or masking Personally Identifiable Information (PII) from logs and traces to protect user privacy. |
| **SLO (Service Level Objective)** | Target level of service quality (e.g., 99.9% uptime, P99 latency <150ms). Used for monitoring and alerting. |
| **SLA (Service Level Agreement)** | Contractual commitment to meet specific SLOs. Often includes penalties for non-compliance. |
| **RED Metrics** | Key performance indicators: **R**equest rate, **E**rror rate, **D**uration (latency). Standard for monitoring services. |
| **USE Metrics** | Infrastructure monitoring: **U**tilization, **S**aturation, **E**rrors. Useful for capacity planning. |
| **Dashboard** | Visual display of metrics, traces, and logs for monitoring system health and performance. |
| **Alert** | Notification triggered when a metric crosses a threshold or anomaly is detected. Enables proactive incident response. |

---

## Build & Versioning Terms

| Term | Definition |
|------|------------|
| **Git Flow** | Branching model with dedicated branches for features, releases, and hotfixes. Uses `main` for production and `develop` for integration. |
| **GitVersion** | Tool calculating semantic versions based on Git history, branch names, and commit messages. Integrates with Conventional Commits. |
| **Conventional Commits** | Specification for commit message format: `<type>(<scope>): <subject>`. Types include `feat`, `fix`, `docs`, `chore`, `test`, `refactor`, `perf`, `ci`. Enables automated versioning and changelog generation. |
| **Semantic Versioning (SemVer)** | Versioning scheme: `Major.Minor.Patch`. Major for breaking changes, minor for new features, patch for bug fixes. |
| **Prerelease Tag** | Version suffix indicating non-production release (e.g., `1.0.0-alpha.5`, `2.1.0-beta.3`). Published to preview feeds. |
| **Build Metadata** | Additional version information (e.g., commit SHA, build number) appended with `+`. Not used for precedence. |
| **Branch Protection** | Repository settings requiring pull request approval, passing CI checks, and preventing force pushes before merging. |
| **CI (Continuous Integration)** | Practice of automatically building and testing code changes as they are committed. |
| **CD (Continuous Deployment)** | Practice of automatically deploying code changes to production after passing CI checks. |
| **Artifact** | Build output (e.g., NuGet package, DLL, Docker image) produced by CI pipeline and used for deployment. |
| **NuGet Package** | Distributable unit containing compiled code, dependencies, and metadata. Published to NuGet.org or GitHub Packages. |

---

## Testing Terms

| Term | Definition |
|------|------------|
| **TDD (Test-Driven Development)** | Development approach: write failing test (red), implement minimal code to pass (green), refactor for quality. Ensures testability and coverage. |
| **Unit Test** | Test verifying a single component (class, method) in isolation. Uses mocks/stubs for dependencies. Fast and deterministic. |
| **Integration Test** | Test verifying interaction between multiple components (e.g., repository + database, API + service layer). Uses real or containerized dependencies. |
| **End-to-End Test** | Test verifying complete user workflows from UI to database. Simulates real user scenarios. |
| **Acceptance Test** | Test validating that a feature meets business requirements and acceptance criteria. Often written in Given-When-Then format. |
|| **xUnit** | .NET testing framework with test discovery, assertions, and fixtures. Preferred framework for the Idevs framework. |
| **Shouldly** | Assertion library providing fluent, readable assertions. Example: `result.ShouldBeOfType<SuccessResult>()`. |
| **NSubstitute** | Mocking library for creating test doubles (mocks, stubs, fakes). Example: `var repo = Substitute.For<IRepository>()`. |
| **Coverage** | Percentage of code executed by tests. Branch coverage measures decision points; line coverage measures lines executed. |
| **Branch Coverage** | Metric measuring percentage of decision paths (if/else, switch) covered by tests. More rigorous than line coverage. |
| **Mutation Testing** | Technique injecting faults into code to verify tests catch the changes. Measures test effectiveness. |
| **Test Fixture** | Shared context or setup for multiple tests. Examples: database initialization, test data builders. |
| **Arrange-Act-Assert (AAA)** | Test structure pattern: Arrange (setup), Act (execute), Assert (verify). Improves readability. |
| **Mocking** | Creating test doubles that simulate behavior of real dependencies. Enables isolated unit testing. |

---

## Cross-Cutting Concerns Implementation

**The Idevs framework uses standard .NET patterns instead of a mediator for cross-cutting concerns:**

| Layer | Pattern | Use Cases | Example |
|-------|---------|-----------|----------|
| **Web** | ASP.NET Core Middleware | Tenant resolution, correlation IDs, exception handling, rate limiting | `app.UseMiddleware<TenantResolutionMiddleware>()` |
| **Controller** | Action Filters | Model validation, authorization, result transformation | `[ValidateModel]`, `[Authorize]` |
| **Application** | Decorator Pattern | Handler validation, logging, caching, performance tracking, retries | `ValidatingCommandHandler<T>`, `LoggingCommandHandler<T>` |
| **Persistence** | EF Core Interceptors | Audit logging, tenant filtering, soft-delete, change tracking | `SaveChangesInterceptor`, `DbCommandInterceptor` |

**Benefits of this approach:**
- ✅ Standard .NET patterns (no custom abstractions)
- ✅ Layer-appropriate (concerns at the right level)
- ✅ Explicit and debuggable (no "magic" routing)
- ✅ No reflection (aligns with framework constraints)
- ✅ Composable (mix and match as needed)
- ✅ Testable (each decorator independently)

---

## Cross-Cutting Terms

| Term | Definition |
|------|------------|
| **Middleware** | ASP.NET Core component processing HTTP requests in a pipeline. Handles cross-cutting concerns like tenant resolution, correlation IDs, exception handling at the web layer. |
| **Action Filter** | ASP.NET Core attribute executing code before/after controller actions. Used for validation, authorization, result transformation. |
| **EF Core Interceptor** | Hook into EF Core operations (queries, saves) to add behavior like audit logging, tenant filtering, soft-delete enforcement. |
| **Decorator Pattern** | Design pattern wrapping objects to add behavior without modifying original. Used for handler-level concerns (validation, logging, caching). |
| **Validation** | Process of verifying input conforms to business rules and constraints. Uses FluentValidation for declarative rule definition. Implemented via decorators or action filters. |
| **Authorization** | Process of determining whether a user or tenant has permission to perform an operation. Often policy-based (e.g., RBAC). |
| **Authentication** | Process of verifying identity of a user or system. Typically uses JWT tokens, OIDC, or OAuth 2.0. |
| **Caching** | Storing frequently accessed data in fast storage (memory, Redis) to reduce latency and database load. |
| **Cache Key** | Unique identifier for cached data. Must include tenant identifier in multi-tenant systems. |
| **Cache Invalidation** | Process of removing stale data from cache when underlying data changes. One of the "two hard problems in computer science". |
| **Read-Through Cache** | Caching pattern where cache automatically loads data from source on miss. Simplifies application code. |
| **Write-Through Cache** | Caching pattern where writes update both cache and underlying data store synchronously. |
| **Cache-Aside** | Caching pattern where application explicitly manages cache population and invalidation. |
| **Retry Policy** | Strategy defining how and when to retry failed operations. Typically uses exponential backoff. |
| **Circuit Breaker** | Pattern preventing calls to failing downstream services. Opens (fails fast) after threshold, half-opens for testing, closes when healthy. |
| **Exponential Backoff** | Retry strategy increasing wait time exponentially between attempts. Reduces load on failing services. |
| **Rate Limiting** | Mechanism restricting number of requests from a client or tenant in a time window. Prevents abuse and ensures fair resource allocation. |
| **Throttling** | Delaying or rejecting requests when system is under heavy load. Protects system stability. |
| **Bulkhead** | Isolation pattern allocating fixed resources to different tenants or operations. Prevents resource exhaustion. |
| **Dead Letter Queue (DLQ)** | Queue storing messages that failed processing after retries. Enables manual review and reprocessing. |
| **Health Check** | Endpoint or mechanism reporting system health status. Used by load balancers and monitoring systems. |
| **Graceful Degradation** | Strategy reducing functionality under stress rather than failing completely. Examples: disabling non-critical features, returning cached data. |

---

## Offline Sync Terms

| Term | Definition |
|------|------------|
| **Offline-First** | Architecture prioritizing operation without network connectivity. Synchronizes when connection available. |
| **Delta Sync** | Synchronization strategy transmitting only changes since last sync. Reduces bandwidth and improves performance. |
| **Sync Token** | Identifier representing point in time or change sequence. Used to request incremental changes since last sync. |
| **ETag** | Entity tag representing specific version of a resource. Used for optimistic concurrency and conditional requests. |
| **Conflict Detection** | Process identifying when same entity modified offline by multiple clients. Uses version numbers or timestamps. |
| **Conflict Resolution** | Strategy for resolving detected conflicts. Options: last-write-wins, manual resolution, merge strategies, custom logic. |
| **Last-Write-Wins (LWW)** | Conflict resolution strategy where most recent modification takes precedence. Simple but may lose data. |
| **Vector Clock** | Data structure tracking causal relationships between events in distributed system. Enables conflict detection. |
| **Sync Checkpoint** | Saved position in sync process allowing resumption after interruption. Prevents full resync. |
| **Tombstone** | Marker indicating entity was deleted. Required for syncing deletions to offline clients. |

---

## References

- [Discovery Summary](discovery-summary.md)
- [CQRS Framework Plan](cqrs-framework-plan.md)
- [Threat Model](threat-model.md)
- [Observability Blueprint](observability-blueprint.md)

---

**Maintenance**: This glossary should be updated as new patterns, practices, and terminology emerge. Propose additions via pull requests with clear definitions and context.
