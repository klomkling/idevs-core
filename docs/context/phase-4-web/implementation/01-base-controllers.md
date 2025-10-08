# Guide 1: Base API Controllers

**Phase**: 4 - Web/API Layer  
**Component**: Base Controllers & Result Mapping  
**Prerequisites**: Phase 3 (Application Layer)  
**Estimated Time**: 2-3 hours

---

## Table of Contents

1. [Overview](#overview)
2. [Design Rationale](#design-rationale)
3. [ApiControllerBase Implementation](#apicontrollerbase-implementation)
4. [Result to HTTP Mapping](#result-to-http-mapping)
5. [Problem Details (RFC 7807)](#problem-details-rfc-7807)
6. [Usage Examples](#usage-examples)
7. [Unit Testing](#unit-testing)
8. [Best Practices](#best-practices)
9. [Common Pitfalls](#common-pitfalls)
10. [Next Steps](#next-steps)

---

## Overview

Base API controllers provide a foundation for all HTTP endpoints by centralizing:

- **IMediator injection**: Send commands/queries
- **Result<T> mapping**: Convert to HTTP status codes
- **Error handling**: RFC 7807 Problem Details
- **Common helpers**: CreatedAt, NoContent, etc.

### Key Benefits

1. **Consistency**: All controllers follow same patterns
2. **DRY**: No duplication of Result→HTTP mapping
3. **Type Safety**: Compile-time verification
4. **Testability**: Easy to mock and test
5. **Standards Compliance**: RFC 7807 Problem Details

---

## Design Rationale

### Why Base Controller?

**Problem**: Each controller duplicates Result<T> mapping logic:

```csharp
// ❌ Bad: Duplicated in every controller
public async Task<IActionResult> Create(...)
{
    var result = await _mediator.Send(command);
    
    if (result.IsSuccess)
        return Ok(result.Value);
    
    if (result.Error.Type == ErrorType.Validation)
        return BadRequest(result.Error);
        
    if (result.Error.Type == ErrorType.NotFound)
        return NotFound(result.Error);
        
    return StatusCode(500, result.Error);
}
```

**Solution**: Centralize in base controller:

```csharp
// ✅ Good: One place for all mapping
public async Task<IActionResult> Create(...)
{
    var result = await _mediator.Send(command);
    return ToActionResult(result);
}
```

### Why Not Repository Injection?

Controllers should depend on **IMediator**, not specific handlers:

```csharp
// ❌ Bad: Direct handler dependency
public ProductsController(CreateProductHandler handler, GetProductHandler handler2)

// ✅ Good: Mediator abstraction
public ProductsController(IMediator mediator)
```

**Benefits**:
- Controllers don't know about handlers
- Easy to add new commands/queries
- Follows dependency inversion principle

---

## ApiControllerBase Implementation

### Core Implementation

```csharp
namespace Idevs.Web.Controllers;

/// <summary>
/// Base controller for all API controllers.
/// Provides common functionality for Result<T> mapping and error handling.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private readonly IMediator _mediator;

    protected ApiControllerBase(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Sends a command and returns the appropriate HTTP response.
    /// </summary>
    protected async Task<IActionResult> SendCommand<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Sends a query and returns the appropriate HTTP response.
    /// </summary>
    protected async Task<IActionResult> SendQuery<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Maps a Result<T> to the appropriate IActionResult.
    /// </summary>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Type switch
        {
            ErrorType.Validation => BadRequest(ToProblemDetails(result.Error)),
            ErrorType.NotFound => NotFound(ToProblemDetails(result.Error)),
            ErrorType.Forbidden => StatusCode(403, ToProblemDetails(result.Error)),
            ErrorType.Unauthorized => Unauthorized(ToProblemDetails(result.Error)),
            ErrorType.Conflict => Conflict(ToProblemDetails(result.Error)),
            _ => StatusCode(500, ToProblemDetails(result.Error))
        };
    }

    /// <summary>
    /// Maps a Result to the appropriate IActionResult (no value).
    /// </summary>
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        return result.Error.Type switch
        {
            ErrorType.Validation => BadRequest(ToProblemDetails(result.Error)),
            ErrorType.NotFound => NotFound(ToProblemDetails(result.Error)),
            ErrorType.Forbidden => StatusCode(403, ToProblemDetails(result.Error)),
            ErrorType.Unauthorized => Unauthorized(ToProblemDetails(result.Error)),
            ErrorType.Conflict => Conflict(ToProblemDetails(result.Error)),
            _ => StatusCode(500, ToProblemDetails(result.Error))
        };
    }

    /// <summary>
    /// Maps a Result<T> to a CreatedAtAction result.
    /// </summary>
    protected IActionResult ToCreatedResult<T>(
        Result<T> result,
        string actionName,
        object? routeValues = null)
    {
        if (result.IsSuccess)
            return CreatedAtAction(actionName, routeValues, result.Value);

        return ToActionResult(result);
    }

    /// <summary>
    /// Converts an Error to RFC 7807 Problem Details.
    /// </summary>
    private ProblemDetails ToProblemDetails(Error error)
    {
        var statusCode = GetStatusCode(error.Type);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Type = GetTypeUri(error.Type),
            Title = GetTitle(error.Type),
            Detail = error.Message,
            Instance = HttpContext.Request.Path
        };

        // Add trace ID for correlation
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        
        // Add error code for client handling
        problemDetails.Extensions["errorCode"] = error.Code;

        // Add validation errors if present
        if (error.ValidationErrors?.Any() == true)
        {
            problemDetails.Extensions["errors"] = error.ValidationErrors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
        }

        return problemDetails;
    }

    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string GetTypeUri(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        ErrorType.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
        ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };

    private static string GetTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "One or more validation errors occurred",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Forbidden => "Access forbidden",
        ErrorType.Unauthorized => "Authentication required",
        ErrorType.Conflict => "Resource conflict",
        _ => "An error occurred"
    };
}
```

---

## Result to HTTP Mapping

### Mapping Rules

| Result Status | Error Type | HTTP Status | Method |
|---------------|------------|-------------|--------|
| Success | - | 200 OK | `Ok(value)` |
| Success (no value) | - | 204 No Content | `NoContent()` |
| Success (created) | - | 201 Created | `CreatedAtAction()` |
| Failure | Validation | 400 Bad Request | `BadRequest(problem)` |
| Failure | NotFound | 404 Not Found | `NotFound(problem)` |
| Failure | Unauthorized | 401 Unauthorized | `Unauthorized(problem)` |
| Failure | Forbidden | 403 Forbidden | `StatusCode(403, problem)` |
| Failure | Conflict | 409 Conflict | `Conflict(problem)` |
| Failure | Other | 500 Internal Server Error | `StatusCode(500, problem)` |

### Example Responses

**Success (200 OK)**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Product Name",
  "price": 99.99
}
```

**Created (201 Created)**:
```
HTTP/1.1 201 Created
Location: /api/v1/products/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Validation Error (400 Bad Request)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred",
  "status": 400,
  "detail": "Validation failed",
  "instance": "/api/v1/products",
  "traceId": "00-abc123-def456-00",
  "errorCode": "Validation.Error",
  "errors": {
    "Name": ["Name is required", "Name must not exceed 200 characters"],
    "Price": ["Price must be greater than 0"]
  }
}
```

**Not Found (404 Not Found)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Resource not found",
  "status": 404,
  "detail": "Product with ID '3fa85f64...' was not found",
  "instance": "/api/v1/products/3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "traceId": "00-abc123-def456-00",
  "errorCode": "Product.NotFound"
}
```

---

## Problem Details (RFC 7807)

### Why RFC 7807?

RFC 7807 standardizes error responses across APIs:

✅ **Standard format**: Clients know what to expect  
✅ **Machine-readable**: Easy to parse and handle  
✅ **Human-friendly**: Clear error messages  
✅ **Extensible**: Custom fields via `extensions`  
✅ **Tool support**: Swagger, Postman understand it  

### Problem Details Structure

```csharp
public class ProblemDetails
{
    // Standard fields (RFC 7807)
    public string? Type { get; set; }        // URI reference
    public string? Title { get; set; }       // Short description
    public int? Status { get; set; }         // HTTP status code
    public string? Detail { get; set; }      // Detailed explanation
    public string? Instance { get; set; }    // URI of specific occurrence
    
    // Extensions (custom fields)
    public IDictionary<string, object?> Extensions { get; set; }
}
```

### Custom Extensions

```csharp
problemDetails.Extensions["traceId"] = "00-abc123-def456-00";
problemDetails.Extensions["errorCode"] = "Product.NotFound";
problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
problemDetails.Extensions["errors"] = validationErrors;
```

---

## Usage Examples

### Example 1: Simple GET Endpoint

```csharp
[ApiVersion("1.0")]
public sealed class ProductsController : ApiControllerBase
{
    public ProductsController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var query = new GetProductByIdQuery(id);
        return await SendQuery(query);
    }
}
```

### Example 2: POST with Created Response

```csharp
/// <summary>
/// Creates a new product.
/// </summary>
[HttpPost]
[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> CreateProduct(
    [FromBody] CreateProductRequest request,
    CancellationToken cancellationToken)
{
    var command = new CreateProductCommand(
        request.Name,
        request.Price,
        request.CategoryId);

    var result = await _mediator.Send(command, cancellationToken);

    return ToCreatedResult(
        result,
        nameof(GetProduct),
        new { id = result.Value });
}
```

### Example 3: PUT with No Content

```csharp
/// <summary>
/// Updates a product.
/// </summary>
[HttpPut("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateProduct(
    Guid id,
    [FromBody] UpdateProductRequest request,
    CancellationToken cancellationToken)
{
    if (id != request.Id)
        return BadRequest(new ProblemDetails
        {
            Title = "ID Mismatch",
            Detail = "Route ID must match request body ID"
        });

    var command = new UpdateProductCommand(
        request.Id,
        request.Name,
        request.Price);

    var result = await _mediator.Send(command, cancellationToken);
    return ToActionResult(result);
}
```

### Example 4: DELETE

```csharp
/// <summary>
/// Deletes a product (soft delete).
/// </summary>
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteProduct(
    Guid id,
    CancellationToken cancellationToken)
{
    var command = new DeleteProductCommand(id);
    var result = await _mediator.Send(command, cancellationToken);
    return ToActionResult(result);
}
```

---

## Unit Testing

### Test 1: Success Response

```csharp
public sealed class ApiControllerBaseTests
{
    [Fact]
    public void ToActionResult_SuccessResult_ReturnsOkWithValue()
    {
        // Arrange
        var controller = new TestController(Substitute.For<IMediator>());
        var result = Result.Success(42);

        // Act
        var actionResult = controller.TestToActionResult(result);

        // Assert
        actionResult.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult;
        okResult.Value.ShouldBe(42);
    }

    [Fact]
    public void ToActionResult_ValidationError_ReturnsBadRequest()
    {
        // Arrange
        var controller = new TestController(Substitute.For<IMediator>());
        var error = Error.Validation("Test.Error", "Validation failed");
        var result = Result.Failure<int>(error);

        // Act
        var actionResult = controller.TestToActionResult(result);

        // Assert
        actionResult.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)actionResult;
        var problemDetails = badRequest.Value as ProblemDetails;
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(400);
    }

    [Fact]
    public void ToActionResult_NotFoundError_ReturnsNotFound()
    {
        // Arrange
        var controller = new TestController(Substitute.For<IMediator>());
        var error = Error.NotFound("Test.NotFound", "Resource not found");
        var result = Result.Failure<int>(error);

        // Act
        var actionResult = controller.TestToActionResult(result);

        // Assert
        actionResult.ShouldBeOfType<NotFoundObjectResult>();
    }

    // Test controller exposing protected methods
    private sealed class TestController : ApiControllerBase
    {
        public TestController(IMediator mediator) : base(mediator)
        {
        }

        public IActionResult TestToActionResult<T>(Result<T> result)
            => ToActionResult(result);
    }
}
```

---

## Best Practices

### ✅ DO

1. **Always use base controller**
   ```csharp
   public sealed class ProductsController : ApiControllerBase // ✅
   ```

2. **Use SendCommand/SendQuery helpers**
   ```csharp
   return await SendQuery(query); // ✅
   ```

3. **Return Result<T> from handlers**
   ```csharp
   public async Task<Result<ProductDto>> Handle(...) // ✅
   ```

4. **Use ProducesResponseType attributes**
   ```csharp
   [ProducesResponseType(typeof(ProductDto), 200)]
   [ProducesResponseType(typeof(ProblemDetails), 404)]
   ```

5. **Include XML documentation**
   ```csharp
   /// <summary>Gets a product by ID.</summary>
   ```

---

### ❌ DON'T

1. **Don't bypass base controller**
   ```csharp
   public sealed class ProductsController : ControllerBase // ❌
   ```

2. **Don't manually map Result<T>**
   ```csharp
   if (result.IsSuccess) return Ok(result.Value); // ❌ Use ToActionResult
   ```

3. **Don't throw exceptions for business failures**
   ```csharp
   throw new NotFoundException(); // ❌ Return Result.Failure
   ```

4. **Don't return domain entities directly**
   ```csharp
   return Ok(product); // ❌ Map to DTO first
   ```

5. **Don't inject handlers directly**
   ```csharp
   public ProductsController(ICreateProductHandler handler) // ❌
   ```

---

## Common Pitfalls

### Pitfall 1: Forgetting CancellationToken

**Problem**: Not passing cancellation token to mediator.

```csharp
// ❌ Bad
return await SendQuery(query);
```

**Solution**: Always pass cancellation token.

```csharp
// ✅ Good
return await SendQuery(query, cancellationToken);
```

---

### Pitfall 2: Inconsistent Error Responses

**Problem**: Some endpoints return custom error format.

**Solution**: Always use `ToActionResult()` for consistent Problem Details.

---

## Next Steps

1. **Read Guide 2**: CRUD Controllers (`02-crud-controllers.md`)
2. **Implement ApiControllerBase**: Create base controller
3. **Add Problem Details support**: Configure in Program.cs
4. **Create sample controller**: Test all HTTP verbs
5. **Write unit tests**: Test Result→HTTP mapping

---

**Summary**: ApiControllerBase centralizes Result<T> to HTTP mapping, ensuring consistent error handling with RFC 7807 Problem Details across all API endpoints.

---

**Last Updated**: 2025-01-08  
**Next Guide**: 02-crud-controllers.md
