# Phase 1: Hybrid Testing Strategy

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Define testing approach balancing speed and thoroughness using a hybrid of InMemory and PostgreSQL tests.

## Testing Pyramid

```text
       ┌───────────────┐
       │   E2E Tests   │  Small (PostgreSQL, slow)
       │  (PostgreSQL) │  Run on main/develop
       └───────────────┘
      ┌─────────────────┐
      │  Integration    │  Medium (InMemory, fast)
      │   Tests         │  Run on every PR
      │  (InMemory)     │
      └─────────────────┘
    ┌───────────────────┐
    │    Unit Tests     │   Large (Mocked, very fast)
    │    (Mocked)       │   Run on every commit
    └───────────────────┘
```

## Test Categories

### 1. Unit Tests (Majority)

**Purpose:** Test individual components in isolation

```csharp
public class CreateOrderHandlerTests
{
    [Fact]
    public async Task Handle_ValidOrder_ReturnsSuccess()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();
        var handler = new CreateOrderHandler(repository);
        var command = new CreateOrderCommand { /* ... */ };
        
        // Act
        var result = await handler.HandleAsync(command);
        
        // Assert
        result.IsSuccess.ShouldBeTrue();
        await repository.Received(1).AddAsync(Arg.Any<Order>());
    }
}
```

- **Database:** None (mocked with NSubstitute)
- **Speed:** Very fast (milliseconds)
- **Coverage Target:** ≥80% of business logic

### 2. Integration Tests (InMemory)

**Purpose:** Test component interactions, EF Core queries

```csharp
public class OrderRepositoryIntegrationTests
{
    private readonly DbContextOptions<AppDbContext> _options;
    
    public OrderRepositoryIntegrationTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
    }
    
    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        await using var context = new AppDbContext(_options);
        var repository = new OrderRepository(context);
        var order = new Order { /* ... */ };
        await repository.AddAsync(order);
        
        // Act
        var result = await repository.GetByIdAsync(order.Id);
        
        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(order.Id);
    }
}
```

- **Database:** EF Core InMemory provider
- **Speed:** Fast (seconds)
- **Limitations:** No constraints, no stored procs

### 3. E2E Tests (PostgreSQL)

**Purpose:** Full integration with real database

```csharp
[Trait("Category", "E2E")]
[Trait("Category", "RequiresPostgreSQL")]
public class OrderWorkflowE2ETests : IClassFixture<PostgreSQLFixture>
{
    private readonly PostgreSQLFixture _fixture;
    
    public OrderWorkflowE2ETests(PostgreSQLFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task CompleteOrderWorkflow_WithRealDatabase_Success()
    {
        // Full workflow test with PostgreSQL
        // ...
    }
}
```

- **Database:** PostgreSQL (via Testcontainers)
- **Speed:** Slow (minutes)
- **When:** On main/develop branches only

## CI Strategy

### Pull Requests

```text
├─ Unit Tests ✅
├─ Integration Tests - InMemory ✅
├─ Architecture Tests ✅
└─ Coverage Check ≥80% ✅
```

### Main/Develop Branches

```text
├─ Unit Tests ✅
├─ Integration Tests - InMemory ✅
├─ E2E Tests - PostgreSQL ✅
├─ Architecture Tests ✅
└─ Coverage Check ≥80% ✅
```

## Local Development

### Option A: InMemory Only (Recommended)

```bash
dotnet test  # Runs unit + integration tests with InMemory
```

### Option B: With PostgreSQL

```bash
# Start PostgreSQL
docker-compose up -d postgres

# Run all tests
export USE_REAL_DATABASE=true
dotnet test
```

## Docker Compose

```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: idevs_core_dev
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
```

## Next Steps

- **[NuGet Packaging](09-nuget-packaging.md)** - Package configuration
- **[Dependency Injection](10-dependency-injection.md)** - DI strategy

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: NuGet Packaging →](09-nuget-packaging.md)
