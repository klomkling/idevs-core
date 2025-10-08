# Phase 6: Getting Started Guide

[← Back to Phase 6 Overview](../phase-6-release.md)

## Overview

Get started with the Idevs framework in under 5 minutes. This guide walks you through creating your first application with command handlers, domain models, and API endpoints.

## Prerequisites

Before you begin, ensure you have:

- .NET 8.0 SDK or later
- PostgreSQL 12+ (or Docker)
- Visual Studio 2022 / VS Code / Rider

## Quick Start

### 1. Install NuGet Packages

Create a new Web API project and add the Idevs packages:

```bash
dotnet new webapi -n MyApp
cd MyApp

# Core packages
dotnet add package Idevs
dotnet add package Idevs.Application
dotnet add package Idevs.Domain
dotnet add package Idevs.Infrastructure.PostgreSQL
dotnet add package Idevs.Web
```

### 2. Configure Services

Set up the Idevs framework in your `Program.cs`:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Idevs framework
builder.Services.AddIdevs(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.EnableMultiTenancy = true;
    options.EnableAuditLogging = true;
});

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Use Idevs middleware
app.UseIdevs();

app.MapControllers();
app.Run();
```

### 3. Define Your Domain

Create a domain entity using aggregate root:

```csharp
// Domain/Product.cs
public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product() { } // EF Core

    public static Product Create(string name, decimal price)
    {
        var product = new Product
        {
            Id = ProductId.New(),
            Name = name,
            Price = price
        };

        return product;
    }
}
```

### 4. Create Command Handler

Implement a command handler for business logic:

```csharp
// Application/Products/CreateProductHandler.cs
public sealed class CreateProductHandler
    : ICommandHandler<CreateProductCommand, ProductId>
{
    private readonly IRepository<Product> _repository;

    public CreateProductHandler(IRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductId>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = Product.Create(command.Name, command.Price);

        _repository.Add(product);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(product.Id);
    }
}
```

### 5. Create API Controller

Expose your command through a REST API:

```csharp
// Controllers/ProductsController.cs
[ApiController]
[Route("api/v1/products")]
public class ProductsController : ApiControllerBase
{
    public ProductsController(
        ICommandExecutor commandExecutor,
        IQueryExecutor queryExecutor)
        : base(commandExecutor, queryExecutor)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await CommandExecutor.ExecuteAsync(command, cancellationToken);
        return ToCreatedResult(result, nameof(GetById), new { id = result.Value });
    }
}
```

### 6. Run Migrations

Set up your database schema:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 7. Run Application

Start your application:

```bash
dotnet run
```

Visit `https://localhost:5001/swagger` to see your API!

## Next Steps

Now that you have a working application, explore these topics:

- **[Sample Applications](03-sample-applications.md)** - Complete examples (Minimal API, TodoApi, Multi-tenant Shop)
- **[API Documentation](01-api-documentation.md)** - Generate API docs with DocFX
- **[Migration Guides](07-migration-guides.md)** - Upgrade between versions
- **[Performance Benchmarks](08-performance-benchmarks.md)** - Optimize your application

## Additional Resources

- [Multi-Tenant Setup Tutorial](../../tutorials/multi-tenancy.md)
- [Authentication & Authorization Guide](../../tutorials/auth.md)
- [Testing Strategies](../../tutorials/testing.md)
- [Building a Complete Application](../../tutorials/complete-app.md)

## Troubleshooting

### Connection String Not Found

Ensure your `appsettings.json` contains:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=myapp;Username=postgres;Password=postgres"
  }
}
```

### Migrations Not Found

Install EF Core tools:

```bash
dotnet tool install --global dotnet-ef
```

### Swagger Not Showing

Add Swagger services in `Program.cs`:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

app.UseSwagger();
app.UseSwaggerUI();
```

---

[← Back to Phase 6 Overview](../phase-6-release.md) | [Next: Sample Applications →](03-sample-applications.md)
