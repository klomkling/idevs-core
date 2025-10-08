# Phase 4: Web/API Layer — Implementation Documentation

**Status**: ✅ Ready for Implementation  
**Package**: `Idevs.Web` / `Idevs.Api`  
**Dependencies**: Phase 3 (Application Layer)  
**Target Framework**: ASP.NET Core 8.0  
**Last Updated**: 2025-01-08

---

## 📚 Overview

The Web/API Layer provides HTTP endpoints that expose the Application Layer through RESTful APIs. It handles HTTP-specific concerns while keeping controllers thin and focused on orchestration.

### Key Responsibilities

1. **HTTP Endpoints**: RESTful API controllers
2. **Request/Response Mapping**: DTOs to commands/queries
3. **Result Mapping**: Result<T> to HTTP status codes
4. **Middleware Pipeline**: Cross-cutting concerns (CORS, auth, logging)
5. **API Documentation**: Swagger/OpenAPI
6. **Error Handling**: RFC 7807 Problem Details
7. **Versioning**: Multiple API versions support

---

## 📊 Quick Navigation

### Implementation Guides

| # | Guide | Focus | Est. Time |
|---|-------|-------|-----------|
| **1** | [Base Controllers](./implementation/01-base-controllers.md) | ApiControllerBase, Result mapping | 2-3 hours |
| **2** | [CRUD Controllers](./implementation/02-crud-controllers.md) | RESTful endpoints, HTTP verbs | 2 hours |
| **3** | [Middleware Pipeline](./implementation/03-middleware-pipeline.md) | Correlation, exception handling, logging | 3-4 hours |
| **4** | [API Versioning](./implementation/04-api-versioning.md) | URL/header versioning, deprecation | 2 hours |
| **5** | [Swagger/OpenAPI](./implementation/05-swagger-openapi.md) | API documentation, XML comments | 2 hours |
| **6** | [Multi-Tenancy](./implementation/06-multi-tenancy.md) | Tenant resolution, isolation | 2-3 hours |
| **7** | [Health Checks](./implementation/07-health-checks.md) | Liveness, readiness probes | 1-2 hours |
| **8** | [Rate Limiting](./implementation/08-rate-limiting.md) | Per-user/IP limits, policies | 2 hours |

### Supporting Documents

| Document | Purpose |
|----------|---------|
| [Completion Guide](./COMPLETION-GUIDE.md) | Implementation checklist and verification |
| [References](./REFERENCES.md) | External resources and documentation |

**Total Implementation Time**: ~16-22 hours

---

## 🚀 Quick Start

### 1. Install Dependencies

```bash
dotnet add package Microsoft.AspNetCore.Mvc.Versioning --version 5.1.0
dotnet add package Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer --version 5.1.0
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
dotnet add package AspNetCoreRateLimit --version 5.0.0
```

### 2. Create Base Controller

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private readonly IMediator _mediator;

    protected ApiControllerBase(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected async Task<IActionResult> SendCommand<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return ToActionResult(result);
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Type switch
        {
            ErrorType.Validation => BadRequest(ToProblemDetails(result.Error)),
            ErrorType.NotFound => NotFound(ToProblemDetails(result.Error)),
            ErrorType.Forbidden => Forbid(),
            ErrorType.Unauthorized => Unauthorized(),
            _ => StatusCode(500, ToProblemDetails(result.Error))
        };
    }

    private ProblemDetails ToProblemDetails(Error error)
    {
        return new ProblemDetails
        {
            Status = GetStatusCode(error.Type),
            Title = error.Code,
            Detail = error.Message,
            Instance = HttpContext.Request.Path,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };
    }

    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError
    };
}
```

### 3. Implement Sample Controller

```csharp
[ApiVersion("1.0")]
[Authorize]
public sealed class ProductsController : ApiControllerBase
{
    public ProductsController(IMediator mediator) : base(mediator)
    {
    }

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

        var result = await SendCommand(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProduct), new { id = result.Value }, result.Value)
            : ToActionResult(result);
    }

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

### 4. Configure Middleware Pipeline

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddApiVersioning();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication(typeof(CreateProductCommand).Assembly);

var app = builder.Build();

// Configure middleware pipeline (order matters!)
app.UseCorrelationId();           // 1. Add correlation ID
app.UseExceptionHandler();        // 2. Handle exceptions
app.UseHttpsRedirection();        // 3. Redirect to HTTPS
app.UseRouting();                 // 4. Route requests
app.UseCors();                    // 5. Apply CORS
app.UseAuthentication();          // 6. Authenticate user
app.UseAuthorization();           // 7. Authorize user
app.UseTenantResolution();        // 8. Resolve tenant
app.UseRateLimiting();            // 9. Apply rate limits

app.MapControllers();             // Map controller endpoints
app.MapHealthChecks("/health");   // Health check endpoint

app.Run();
```

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────┐
│              HTTP Request                        │
│         (JSON/XML, Headers, Query Params)        │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│           Middleware Pipeline                    │
│  • Correlation ID                                │
│  • Exception Handling                            │
│  • Authentication                                │
│  • Tenant Resolution                             │
│  • Rate Limiting                                 │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│            API Controller                        │
│  • Model Binding                                 │
│  • Input Validation                              │
│  • Map DTO → Command/Query                       │
│  • Send via IMediator                            │
│  • Map Result<T> → IActionResult                 │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│        Application Layer (Phase 3)               │
│  • Command/Query Handlers                        │
│  • Validation, Authorization                     │
│  • Business Logic Execution                      │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│              HTTP Response                       │
│  • Status Code (200, 400, 404, 500)             │
│  • Response Body (JSON)                          │
│  • Headers (Correlation-ID, etc.)                │
└─────────────────────────────────────────────────┘
```

---

## 📝 Key Design Principles

1. **Thin Controllers**: Controllers orchestrate, don't implement business logic
2. **Dependency Inversion**: Controllers depend on IMediator, not specific handlers
3. **Result Mapping**: Consistent conversion of Result<T> to HTTP responses
4. **RFC 7807**: Use Problem Details for all errors
5. **Correlation**: Track requests across layers with correlation IDs
6. **Versioning**: Support multiple API versions simultaneously
7. **Documentation**: Auto-generate Swagger docs from XML comments

---

## 🧪 Testing Strategy

### Unit Tests

```csharp
public sealed class ProductsControllerTests
{
    [Fact]
    public async Task CreateProduct_ValidRequest_Returns201Created()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var productId = Guid.NewGuid();
        
        mediator
            .Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(productId));

        var controller = new ProductsController(mediator);
        var request = new CreateProductRequest("Test", 10m, Guid.NewGuid());

        // Act
        var result = await controller.CreateProduct(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.Value.ShouldBe(productId);
    }

    [Fact]
    public async Task GetProduct_NotFound_Returns404()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        
        mediator
            .Send(Arg.Any<GetProductByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ProductDto>(Error.NotFound("Product.NotFound", "Product not found")));

        var controller = new ProductsController(mediator);

        // Act
        var result = await controller.GetProduct(Guid.NewGuid());

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }
}
```

### Integration Tests

```csharp
public sealed class ProductsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateProductRequest("Test Product", 99.99m, Guid.NewGuid());

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
    }
}
```

---

## 🔒 Security Checklist

- [ ] HTTPS enforced (redirect HTTP to HTTPS)
- [ ] Authentication configured (JWT Bearer tokens)
- [ ] Authorization policies on sensitive endpoints
- [ ] CORS configured for allowed origins
- [ ] Rate limiting enabled
- [ ] Anti-forgery tokens for state-changing requests
- [ ] Request size limits configured
- [ ] Security headers added (HSTS, CSP, X-Frame-Options)

---

## 📊 Performance Targets

| Metric | Target | Maximum |
|--------|--------|---------|
| Response Time (P50) | < 100ms | < 200ms |
| Response Time (P95) | < 300ms | < 500ms |
| Response Time (P99) | < 500ms | < 1000ms |
| Throughput | > 1000 req/s | > 500 req/s |

---

## 🔗 External Resources

- **ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/
- **API Versioning**: https://github.com/dotnet/aspnet-api-versioning
- **Swagger/OpenAPI**: https://swagger.io/docs/
- **RFC 7807**: https://datatracker.ietf.org/doc/html/rfc7807

---

## 📞 Support & Contribution

- **Issues**: Report in project issue tracker
- **Documentation**: See implementation guides in `./implementation/`
- **Architecture Questions**: Review Phase 3 documentation

---

**Last Updated**: 2025-01-08  
**Maintained By**: Idevs Framework Team  
**License**: MIT
