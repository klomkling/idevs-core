# Phase 6: Sample Applications

[← Back to Phase 6 Overview](../phase-6-release.md)

## Overview

Create fully functional sample applications that demonstrate the Idevs framework's capabilities. These samples serve as reference implementations and starting points for new projects.

## Sample Projects

### 1. Minimal API Sample

A lightweight single-file application demonstrating the basics.

**Structure:**

```text
samples/MinimalApi/
├── MinimalApi.csproj
├── Program.cs
├── appsettings.json
└── README.md
```

**Program.cs Implementation:**

```csharp
using Idevs;
using Idevs.Application;
using Idevs.Infrastructure.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Configure Idevs
builder.Services.AddIdevs(options =>
{
    options.ConnectionString = "Host=localhost;Database=minimal_api;Username=postgres;Password=postgres";
    options.EnableMultiTenancy = false;
    options.EnableAuditLogging = true;
});

var app = builder.Build();

// Ensure database is created
await app.Services.EnsureDatabaseCreatedAsync();

// Seed data
await app.Services.SeedDataAsync();

app.UseIdevs();

// Define minimal endpoints
app.MapGet("/api/products", async (IQueryExecutor executor) =>
{
    var query = new GetAllProductsQuery();
    var result = await executor.ExecuteAsync(query);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
});

app.MapPost("/api/products", async (CreateProductCommand command, ICommandExecutor executor) =>
{
    var result = await executor.ExecuteAsync(command);
    return result.IsSuccess
        ? Results.Created($"/api/products/{result.Value}", result.Value)
        : Results.BadRequest(result.Errors);
});

app.Run();
```

### 2. TodoApi - Layered Architecture

A complete CRUD application following clean architecture principles.

**Structure:**

```text
samples/TodoApi/
├── TodoApi.Api/                    # Web API layer
│   ├── Controllers/
│   │   └── TodosController.cs
│   ├── Program.cs
│   └── appsettings.json
├── TodoApi.Application/            # Application layer
│   ├── Commands/
│   │   ├── CreateTodoCommand.cs
│   │   └── CreateTodoHandler.cs
│   └── Queries/
│       ├── GetTodosQuery.cs
│       └── GetTodosHandler.cs
├── TodoApi.Domain/                 # Domain layer
│   ├── Entities/
│   │   └── Todo.cs
│   └── ValueObjects/
│       └── TodoId.cs
├── TodoApi.Infrastructure/         # Infrastructure layer
│   ├── Persistence/
│   │   └── TodoDbContext.cs
│   └── Repositories/
│       └── TodoRepository.cs
└── README.md
```

**Key Features:**

- Complete CRUD operations
- FluentValidation integration
- Entity Framework Core
- Swagger documentation
- Health checks

### 3. MultiTenantShop - Advanced Example

A multi-tenant e-commerce platform demonstrating advanced patterns.

**Structure:**

```text
samples/MultiTenantShop/
├── Shop.Api/                       # API Gateway
│   ├── Controllers/
│   │   ├── ProductsController.cs
│   │   ├── OrdersController.cs
│   │   └── CustomersController.cs
│   └── Program.cs
├── Shop.Application/               # Application logic
│   ├── Products/
│   ├── Orders/
│   └── Customers/
├── Shop.Domain/                    # Domain models
│   ├── Products/
│   ├── Orders/
│   └── Customers/
├── Shop.Infrastructure/            # Data access
│   ├── Persistence/
│   └── Caching/
├── Shop.Tests/                     # Test suite
│   ├── Unit/
│   └── Integration/
└── README.md
```

**Advanced Features:**

- Multi-tenancy with row-level security
- Domain events
- Distributed caching (Redis)
- Background jobs
- Audit logging
- Soft deletes
- Specification pattern
- Integration tests

### 4. PerformanceBenchmark

BenchmarkDotNet suite for performance testing.

**Structure:**

```text
samples/PerformanceBenchmark/
├── Benchmarks/
│   ├── CommandExecutorBenchmarks.cs
│   ├── QueryExecutorBenchmarks.cs
│   └── RepositoryBenchmarks.cs
├── BenchmarkDotNet.Artifacts/
└── README.md
```

## Sample README Template

Each sample should include a comprehensive README:

```markdown
# [Sample Name]

Brief description of what this sample demonstrates.

## Features

- Feature 1
- Feature 2
- Feature 3

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL 12+
- Docker (optional)

## Getting Started

### 1. Clone Repository

\`\`\`bash
git clone https://github.com/idevs/idevs-core.git
cd samples/[SampleName]
\`\`\`

### 2. Configure Database

Update connection string in `appsettings.json`.

### 3. Run Migrations

\`\`\`bash
dotnet ef database update
\`\`\`

### 4. Run Application

\`\`\`bash
dotnet run
\`\`\`

## Project Structure

Explain the folder structure and key files.

## Key Concepts Demonstrated

- Concept 1: Explanation
- Concept 2: Explanation

## Testing

\`\`\`bash
dotnet test
\`\`\`

## Learn More

- [Getting Started Guide](../../docs/getting-started.md)
- [Architecture Overview](../../docs/architecture.md)
```

## Docker Compose Setup

Include `docker-compose.yml` for easy local development:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: idevs_samples
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  sample_api:
    build: .
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=idevs_samples;Username=postgres;Password=postgres"
      Redis__ConnectionString: "redis:6379"
    ports:
      - "5000:80"
    depends_on:
      - postgres
      - redis

volumes:
  postgres_data:
```

## Sample Data Seeding

Include seed data for demos:

```csharp
public static class SampleDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();
        
        // Check if data already exists
        if (await dbContext.Products.AnyAsync())
            return;
        
        // Seed products
        var products = new[]
        {
            Product.Create("Laptop", 999.99m),
            Product.Create("Mouse", 29.99m),
            Product.Create("Keyboard", 79.99m)
        };
        
        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync();
    }
}
```

## CI/CD for Samples

Include GitHub Actions workflow for sample validation:

```yaml
name: Samples CI

on: [push, pull_request]

jobs:
  build-samples:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        sample: [MinimalApi, TodoApi, MultiTenantShop]
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Build ${{ matrix.sample }}
        run: dotnet build samples/${{ matrix.sample }}
      
      - name: Test ${{ matrix.sample }}
        run: dotnet test samples/${{ matrix.sample }}
```

## Next Steps

- **[Package Configuration](04-package-configuration.md)** - NuGet packaging setup
- **[Release Pipeline](05-release-pipeline.md)** - Automated releases
- **[Performance Benchmarks](08-performance-benchmarks.md)** - Optimization guide

---

[← Back to Phase 6 Overview](../phase-6-release.md) | [Next: Package Configuration →](04-package-configuration.md)
