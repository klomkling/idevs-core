# Guide 2: CRUD Controllers

**Phase**: 4 - Web/API Layer  
**Component**: CRUD Controllers  
**Depends On**: [Guide 1: Base Controllers](./01-base-controllers.md)  
**Prerequisites**: MediatR, FluentValidation, Idevs.Application layer

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [RESTful Design Principles](#restful-design-principles)
3. [Implementation Patterns](#implementation-patterns)
4. [Request/Response Models](#requestresponse-models)
5. [Validation Strategies](#validation-strategies)
6. [Error Handling](#error-handling)
7. [Pagination & Filtering](#pagination--filtering)
8. [Testing](#testing)
9. [Best Practices](#best-practices)
10. [Common Pitfalls](#common-pitfalls)

---

## Overview

CRUD controllers provide RESTful HTTP endpoints that translate HTTP requests into commands/queries and map results back to HTTP responses. They act as thin orchestration layers following the single-responsibility principle.

### Key Responsibilities

- **Route Management**: Define URL patterns and HTTP verb mappings
- **Request Binding**: Parse and validate incoming HTTP requests
- **Command/Query Dispatch**: Send requests to MediatR handlers
- **Response Mapping**: Convert handler results to HTTP responses
- **Documentation**: Provide OpenAPI/Swagger metadata

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      HTTP Request                            │
│  POST /api/v1/products  { "name": "...", "price": 10.99 }  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                 ProductsController                           │
│  • Parse request body → CreateProductRequest                 │
│  • Map to command → CreateProductCommand                     │
│  • Validate via FluentValidation                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    IMediator.Send()                          │
│  → ValidationBehavior → AuthorizationBehavior → Handler     │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Result<Guid> (Success/Failure)                  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│          ToCreatedResult() → 201 Created                     │
│   Location: /api/v1/products/{id}                            │
└─────────────────────────────────────────────────────────────┘
```

---

## RESTful Design Principles

### HTTP Verb Mapping

| Operation | HTTP Verb | Endpoint | Response Code | Idempotent |
|-----------|-----------|----------|---------------|------------|
| **Create** | POST | `/api/v1/products` | 201 Created | ❌ No |
| **Read** | GET | `/api/v1/products/{id}` | 200 OK | ✅ Yes |
| **List** | GET | `/api/v1/products` | 200 OK | ✅ Yes |
| **Update** | PUT | `/api/v1/products/{id}` | 204 No Content | ✅ Yes |
| **Partial Update** | PATCH | `/api/v1/products/{id}` | 204 No Content | ✅ Yes |
| **Delete** | DELETE | `/api/v1/products/{id}` | 204 No Content | ✅ Yes |

### Status Code Guidelines

```csharp
// Success Codes
200 OK              // GET: Resource retrieved
201 Created         // POST: Resource created
204 No Content      // PUT/PATCH/DELETE: Operation succeeded, no response body

// Client Error Codes
400 Bad Request     // Invalid request data
401 Unauthorized    // Authentication required
403 Forbidden       // Insufficient permissions
404 Not Found       // Resource doesn't exist
409 Conflict        // Business rule violation (e.g., duplicate key)
422 Unprocessable   // Validation failed

// Server Error Codes
500 Internal Error  // Unhandled exception
503 Service Unavailable // Dependency failure
```

### Resource Naming Conventions

```
✅ GOOD:
/api/v1/products
/api/v1/products/{id}
/api/v1/products/{id}/variants
/api/v1/categories/{categoryId}/products

❌ BAD:
/api/v1/getProducts      // Verb in URL
/api/v1/product          // Singular noun
/api/v1/Products         // Inconsistent casing
```

---

## Implementation Patterns

### 1. Basic CRUD Controller

```csharp
using Idevs.Web.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Idevs.Api.Controllers.V1;

/// <summary>
/// Manages product catalog operations.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ApiControllerBase
{
    public ProductsController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">Product creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created product.</returns>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="409">Product with same SKU already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Sku,
            request.Price,
            request.CategoryId);

        var result = await _mediator.Send(command, cancellationToken);

        return ToCreatedResult(result, nameof(Get), new { id = result.Value });
    }

    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The product details.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        return await SendQuery(query, cancellationToken);
    }

    /// <summary>
    /// Lists products with pagination and filtering.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (max 100).</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="searchTerm">Optional search term.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of products.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListProductsQuery(page, pageSize, categoryId, searchTerm);
        return await SendQuery(query, cancellationToken);
    }

    /// <summary>
    /// Updates an existing product (full replacement).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="request">Updated product details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Product updated successfully.</response>
    /// <response code="400">Invalid request data or ID mismatch.</response>
    /// <response code="404">Product not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        // Guard: Ensure route ID matches body ID
        if (id != request.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ID Mismatch",
                Detail = "The product ID in the URL must match the ID in the request body.",
                Instance = HttpContext.Request.Path
            });
        }

        var command = new UpdateProductCommand(
            request.Id,
            request.Name,
            request.Price,
            request.CategoryId);

        return await SendCommand(command, cancellationToken);
    }

    /// <summary>
    /// Partially updates a product (only specified fields).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Product patched successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Product not found.</response>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(
        Guid id,
        [FromBody] PatchProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PatchProductCommand(
            id,
            request.Name,
            request.Price,
            request.CategoryId);

        return await SendCommand(command, cancellationToken);
    }

    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Product deleted successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="409">Product cannot be deleted (has active orders).</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);
        return await SendCommand(command, cancellationToken);
    }
}
```

---

## Request/Response Models

### Request DTOs

Request DTOs should be simple, anemic data containers that map cleanly to commands/queries.

```csharp
namespace Idevs.Api.Models.Requests;

/// <summary>
/// Request to create a new product.
/// </summary>
public sealed record CreateProductRequest
{
    /// <summary>
    /// Product name.
    /// </summary>
    /// <example>Wireless Mouse</example>
    public required string Name { get; init; }

    /// <summary>
    /// Stock Keeping Unit (SKU).
    /// </summary>
    /// <example>WM-001</example>
    public required string Sku { get; init; }

    /// <summary>
    /// Product price in USD.
    /// </summary>
    /// <example>29.99</example>
    public required decimal Price { get; init; }

    /// <summary>
    /// Category ID.
    /// </summary>
    public required Guid CategoryId { get; init; }
}

/// <summary>
/// Request to update an existing product (full replacement).
/// </summary>
public sealed record UpdateProductRequest
{
    /// <summary>
    /// Product ID (must match route parameter).
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Updated product name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Updated price.
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Updated category ID.
    /// </summary>
    public required Guid CategoryId { get; init; }
}

/// <summary>
/// Request to partially update a product (only specified fields).
/// </summary>
public sealed record PatchProductRequest
{
    /// <summary>
    /// Optional new name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Optional new price.
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// Optional new category ID.
    /// </summary>
    public Guid? CategoryId { get; init; }
}
```

### Response DTOs

```csharp
namespace Idevs.Api.Models.Responses;

/// <summary>
/// Product details.
/// </summary>
public sealed record ProductDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Sku { get; init; }
    public required decimal Price { get; init; }
    public required Guid CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Paginated result wrapper.
/// </summary>
public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

---

## Validation Strategies

### 1. Request Model Validation (FluentValidation)

```csharp
using FluentValidation;

namespace Idevs.Api.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Product name must be between 1 and 200 characters.");

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[A-Z0-9\-]+$")
            .WithMessage("SKU must contain only uppercase letters, numbers, and hyphens.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than zero.");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category ID is required.");
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.CategoryId)
            .NotEmpty();
    }
}
```

### 2. Validation Registration (Program.cs)

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

// Configure automatic validation behavior
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(error => new
            {
                Field = e.Key,
                Message = error.ErrorMessage
            }))
            .ToList();

        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(problemDetails);
    };
});
```

### 3. Route Parameter Validation

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> Update(
    Guid id,
    [FromBody] UpdateProductRequest request,
    CancellationToken cancellationToken)
{
    // Validate ID is not empty
    if (id == Guid.Empty)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Invalid ID",
            Detail = "Product ID cannot be empty.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    // Validate route ID matches body ID
    if (id != request.Id)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "ID Mismatch",
            Detail = $"Route ID '{id}' must match request body ID '{request.Id}'.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    var command = new UpdateProductCommand(request.Id, request.Name, request.Price, request.CategoryId);
    return await SendCommand(command, cancellationToken);
}
```

---

## Error Handling

### Business Rule Violations

```csharp
// Domain Layer (Product.cs)
public sealed class Product : AggregateRoot<Guid>
{
    public Result UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            return Result.Failure(ProductErrors.InvalidPrice);

        if (newPrice > 100000)
            return Result.Failure(ProductErrors.PriceExceedsMaximum);

        Price = newPrice;
        return Result.Success();
    }
}

public static class ProductErrors
{
    public static readonly Error InvalidPrice = new(
        "Product.InvalidPrice",
        "Product price must be greater than zero.");

    public static readonly Error PriceExceedsMaximum = new(
        "Product.PriceExceedsMaximum",
        "Product price cannot exceed $100,000.");

    public static readonly Error NotFound = new(
        "Product.NotFound",
        "The specified product was not found.");

    public static readonly Error DuplicateSku = new(
        "Product.DuplicateSku",
        "A product with this SKU already exists.");
}
```

### Error Mapping in Controller

```csharp
// ApiControllerBase (inherited by all controllers)
protected IActionResult ToResult<T>(Result<T> result)
{
    if (result.IsSuccess)
        return Ok(result.Value);

    // Map domain errors to HTTP status codes
    return result.Error.Code switch
    {
        var code when code.EndsWith(".NotFound") => NotFound(ToProblemDetails(result.Error)),
        var code when code.EndsWith(".DuplicateSku") => Conflict(ToProblemDetails(result.Error)),
        var code when code.EndsWith(".Unauthorized") => Forbid(),
        _ => BadRequest(ToProblemDetails(result.Error))
    };
}

private ProblemDetails ToProblemDetails(Error error)
{
    return new ProblemDetails
    {
        Type = $"https://api.idevs.work/errors/{error.Code}",
        Title = error.Code,
        Detail = error.Message,
        Status = GetStatusCode(error.Code),
        Instance = HttpContext.Request.Path
    };
}
```

---

## Pagination & Filtering

### Query String Parameters

```csharp
public sealed record ListProductsQuery : IQuery<PagedResult<ProductDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? CategoryId { get; init; }
    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; } = "Name";
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
}

public enum SortDirection
{
    Ascending,
    Descending
}
```

### Controller Endpoint

```csharp
/// <summary>
/// Lists products with advanced filtering and sorting.
/// </summary>
/// <param name="page">Page number (1-based, default 1).</param>
/// <param name="pageSize">Items per page (1-100, default 20).</param>
/// <param name="categoryId">Filter by category.</param>
/// <param name="searchTerm">Search in name and SKU.</param>
/// <param name="sortBy">Sort field (Name, Price, CreatedAt).</param>
/// <param name="sortDirection">Sort direction (Ascending, Descending).</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Paginated products.</returns>
[HttpGet]
[ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> List(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] Guid? categoryId = null,
    [FromQuery] string? searchTerm = null,
    [FromQuery] string sortBy = "Name",
    [FromQuery] SortDirection sortDirection = SortDirection.Ascending,
    CancellationToken cancellationToken = default)
{
    // Validate pagination parameters
    if (page < 1)
        return BadRequest("Page number must be at least 1.");

    if (pageSize is < 1 or > 100)
        return BadRequest("Page size must be between 1 and 100.");

    var query = new ListProductsQuery
    {
        Page = page,
        PageSize = pageSize,
        CategoryId = categoryId,
        SearchTerm = searchTerm,
        SortBy = sortBy,
        SortDirection = sortDirection
    };

    return await SendQuery(query, cancellationToken);
}
```

### Pagination Response Headers

```csharp
[HttpGet]
public async Task<IActionResult> List(/* parameters */)
{
    var result = await SendQuery(query, cancellationToken);

    if (result is OkObjectResult okResult && okResult.Value is PagedResult<ProductDto> pagedResult)
    {
        // Add pagination metadata to response headers
        Response.Headers.Append("X-Pagination-TotalCount", pagedResult.TotalCount.ToString());
        Response.Headers.Append("X-Pagination-TotalPages", pagedResult.TotalPages.ToString());
        Response.Headers.Append("X-Pagination-CurrentPage", pagedResult.Page.ToString());
        Response.Headers.Append("X-Pagination-PageSize", pagedResult.PageSize.ToString());

        // Add navigation links
        if (pagedResult.HasNextPage)
        {
            var nextPageUrl = Url.Action(nameof(List), new { page = pagedResult.Page + 1, pageSize = pagedResult.PageSize });
            Response.Headers.Append("Link", $"<{nextPageUrl}>; rel=\"next\"");
        }

        if (pagedResult.HasPreviousPage)
        {
            var prevPageUrl = Url.Action(nameof(List), new { page = pagedResult.Page - 1, pageSize = pagedResult.PageSize });
            Response.Headers.Append("Link", $"<{prevPageUrl}>; rel=\"prev\"");
        }
    }

    return result;
}
```

---

## Testing

### Unit Tests

```csharp
using NSubstitute;
using Shouldly;
using Xunit;

namespace Idevs.Api.Tests.Controllers;

public sealed class ProductsControllerTests
{
    private readonly IMediator _mediator;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new ProductsController(_mediator);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201Created()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Sku = "TEST-001",
            Price = 99.99m,
            CategoryId = Guid.NewGuid()
        };

        _mediator.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(productId));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.StatusCode.ShouldBe(StatusCodes.Status201Created);
        createdResult.Value.ShouldBe(productId);
        createdResult.ActionName.ShouldBe(nameof(ProductsController.Get));
    }

    [Fact]
    public async Task Create_DuplicateSku_Returns409Conflict()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Sku = "TEST-001",
            Price = 99.99m,
            CategoryId = Guid.NewGuid()
        };

        _mediator.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>(ProductErrors.DuplicateSku));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var conflictResult = result.ShouldBeOfType<ConflictObjectResult>();
        conflictResult.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        
        var problemDetails = conflictResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Detail.ShouldContain("SKU already exists");
    }

    [Fact]
    public async Task Get_ExistingId_Returns200Ok()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productDto = new ProductDto
        {
            Id = productId,
            Name = "Test Product",
            Sku = "TEST-001",
            Price = 99.99m,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Electronics",
            CreatedAt = DateTime.UtcNow
        };

        _mediator.Send(Arg.Any<GetProductByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(productDto));

        // Act
        var result = await _controller.Get(productId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldBe(productDto);
    }

    [Fact]
    public async Task Get_NonExistingId_Returns404NotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetProductByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ProductDto>(ProductErrors.NotFound));

        // Act
        var result = await _controller.Get(productId, CancellationToken.None);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Update_IdMismatch_Returns400BadRequest()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var bodyId = Guid.NewGuid();
        var request = new UpdateProductRequest
        {
            Id = bodyId,
            Name = "Updated Product",
            Price = 149.99m,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var result = await _controller.Update(routeId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("ID Mismatch");
    }

    [Fact]
    public async Task Delete_ExistingProduct_Returns204NoContent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mediator.Send(Arg.Any<DeleteProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.Delete(productId, CancellationToken.None);

        // Assert
        var noContentResult = result.ShouldBeOfType<NoContentResult>();
        noContentResult.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task List_ValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var pagedResult = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Product 1", Sku = "P1", Price = 10m, CategoryId = Guid.NewGuid(), CategoryName = "Cat1", CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "Product 2", Sku = "P2", Price = 20m, CategoryId = Guid.NewGuid(), CategoryName = "Cat2", CreatedAt = DateTime.UtcNow }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<ListProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(pagedResult));

        // Act
        var result = await _controller.List(1, 20, null, null, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedPagedResult = okResult.Value.ShouldBeOfType<PagedResult<ProductDto>>();
        returnedPagedResult.Items.Count.ShouldBe(2);
        returnedPagedResult.TotalCount.ShouldBe(2);
    }
}
```

### Integration Tests

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Idevs.Api.IntegrationTests.Controllers;

public sealed class ProductsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_ValidData_Returns201()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Integration Test Product",
            Sku = $"INT-{Guid.NewGuid():N}",
            Price = 49.99m,
            CategoryId = await GetTestCategoryId()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var productId = await response.Content.ReadFromJsonAsync<Guid>();
        productId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetProduct_ExistingId_Returns200()
    {
        // Arrange
        var productId = await CreateTestProduct();

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(productId);
    }

    [Fact]
    public async Task ListProducts_WithPagination_ReturnsPagedResult()
    {
        // Arrange
        await CreateTestProduct();
        await CreateTestProduct();

        // Act
        var response = await _client.GetAsync("/api/v1/products?page=1&pageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        pagedResult.ShouldNotBeNull();
        pagedResult.Items.Count.ShouldBeGreaterThan(0);
    }

    private async Task<Guid> CreateTestProduct()
    {
        var request = new CreateProductRequest
        {
            Name = $"Test Product {Guid.NewGuid():N}",
            Sku = $"TEST-{Guid.NewGuid():N}",
            Price = 29.99m,
            CategoryId = await GetTestCategoryId()
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> GetTestCategoryId()
    {
        // Implementation to get or create a test category
        return Guid.NewGuid(); // Simplified for example
    }
}
```

---

## Best Practices

### 1. ✅ Use Explicit Response Types

```csharp
// ✅ GOOD: Clear documentation and response types
[HttpGet("{id}")]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> Get(Guid id)
{
    var query = new GetProductByIdQuery(id);
    return await SendQuery(query);
}

// ❌ BAD: No response type documentation
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
{
    var query = new GetProductByIdQuery(id);
    return await SendQuery(query);
}
```

### 2. ✅ Validate Route Parameters

```csharp
// ✅ GOOD: Validate before processing
[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, UpdateProductRequest request)
{
    if (id == Guid.Empty)
        return BadRequest("Invalid product ID.");

    if (id != request.Id)
        return BadRequest("Route ID must match body ID.");

    // Continue processing...
}

// ❌ BAD: No validation
[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, UpdateProductRequest request)
{
    var command = new UpdateProductCommand(request);
    return await SendCommand(command);
}
```

### 3. ✅ Use CancellationToken

```csharp
// ✅ GOOD: Supports request cancellation
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
{
    var query = new GetProductByIdQuery(id);
    return await SendQuery(query, cancellationToken);
}

// ❌ BAD: No cancellation support
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
{
    var query = new GetProductByIdQuery(id);
    return await SendQuery(query);
}
```

### 4. ✅ Follow Naming Conventions

```csharp
// ✅ GOOD: RESTful naming
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> List() { }
    [HttpGet("{id}")] public async Task<IActionResult> Get(Guid id) { }
    [HttpPost] public async Task<IActionResult> Create(...) { }
    [HttpPut("{id}")] public async Task<IActionResult> Update(...) { }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(Guid id) { }
}

// ❌ BAD: Non-RESTful naming
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> GetAllProducts() { }
    [HttpGet("{id}")] public async Task<IActionResult> GetProductById(Guid id) { }
    [HttpPost] public async Task<IActionResult> AddNewProduct(...) { }
}
```

### 5. ✅ Use XML Documentation

```csharp
// ✅ GOOD: Comprehensive documentation
/// <summary>
/// Creates a new product.
/// </summary>
/// <param name="request">Product creation details.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The ID of the created product.</returns>
/// <response code="201">Product created successfully.</response>
/// <response code="400">Invalid request data.</response>
/// <remarks>
/// Sample request:
///     POST /api/v1/products
///     {
///        "name": "Wireless Mouse",
///        "sku": "WM-001",
///        "price": 29.99,
///        "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
///     }
/// </remarks>
[HttpPost]
public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken)
```

---

## Common Pitfalls

### ❌ Pitfall 1: Fat Controllers

```csharp
// ❌ BAD: Business logic in controller
[HttpPost]
public async Task<IActionResult> Create(CreateProductRequest request)
{
    // DON'T DO THIS!
    if (await _dbContext.Products.AnyAsync(p => p.Sku == request.Sku))
        return Conflict("Duplicate SKU");

    var product = new Product
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Price = request.Price
    };

    _dbContext.Products.Add(product);
    await _dbContext.SaveChangesAsync();

    return CreatedAtAction(nameof(Get), new { id = product.Id }, product.Id);
}

// ✅ GOOD: Thin controller, delegate to handler
[HttpPost]
public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct)
{
    var command = new CreateProductCommand(request.Name, request.Sku, request.Price);
    var result = await _mediator.Send(command, ct);
    return ToCreatedResult(result, nameof(Get), new { id = result.Value });
}
```

### ❌ Pitfall 2: Inconsistent Error Responses

```csharp
// ❌ BAD: Inconsistent error formats
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
{
    var product = await _repository.GetByIdAsync(id);
    
    if (product is null)
        return NotFound("Product not found"); // Plain string

    return Ok(product);
}

// ✅ GOOD: RFC 7807 Problem Details
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, CancellationToken ct)
{
    var query = new GetProductByIdQuery(id);
    var result = await _mediator.Send(query, ct);
    
    return result.IsSuccess
        ? Ok(result.Value)
        : NotFound(new ProblemDetails
        {
            Title = "Product Not Found",
            Detail = $"No product exists with ID '{id}'.",
            Status = StatusCodes.Status404NotFound,
            Instance = HttpContext.Request.Path
        });
}
```

### ❌ Pitfall 3: Missing Pagination

```csharp
// ❌ BAD: Returns all records (potential performance issue)
[HttpGet]
public async Task<IActionResult> List()
{
    var products = await _repository.GetAllAsync();
    return Ok(products); // Could be thousands of records!
}

// ✅ GOOD: Always paginate collections
[HttpGet]
public async Task<IActionResult> List(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    var query = new ListProductsQuery(page, pageSize);
    return await SendQuery(query, ct);
}
```

### ❌ Pitfall 4: Swallowing Exceptions

```csharp
// ❌ BAD: Catches and hides exceptions
[HttpPost]
public async Task<IActionResult> Create(CreateProductRequest request)
{
    try
    {
        var command = new CreateProductCommand(request.Name, request.Price);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    catch (Exception)
    {
        return StatusCode(500, "Something went wrong"); // No details!
    }
}

// ✅ GOOD: Let global exception handler deal with it
[HttpPost]
public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct)
{
    var command = new CreateProductCommand(request.Name, request.Price);
    var result = await _mediator.Send(command, ct);
    return ToCreatedResult(result, nameof(Get), new { id = result.Value });
}
```

### ❌ Pitfall 5: Synchronous Operations

```csharp
// ❌ BAD: Blocking async operation
[HttpGet("{id}")]
public IActionResult Get(Guid id)
{
    var query = new GetProductByIdQuery(id);
    var result = _mediator.Send(query).Result; // DEADLOCK RISK!
    return Ok(result);
}

// ✅ GOOD: Async all the way
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, CancellationToken ct)
{
    var query = new GetProductByIdQuery(id);
    return await SendQuery(query, ct);
}
```

---

## Summary

CRUD controllers in the Idevs framework:

✅ **Are thin orchestrators** that delegate to MediatR handlers  
✅ **Follow RESTful conventions** for HTTP verbs and status codes  
✅ **Use RFC 7807 Problem Details** for consistent error responses  
✅ **Support pagination and filtering** for collection endpoints  
✅ **Validate input** using FluentValidation and route guards  
✅ **Document with XML comments** for Swagger generation  
✅ **Test thoroughly** with unit and integration tests  

---

**Next Guide**: [03-middleware-pipeline.md](./03-middleware-pipeline.md) — Correlation IDs, exception handling, tenant resolution

---

**Last Updated**: 2025-01-08  
**Maintained By**: Idevs Framework Team  
**License**: MIT
