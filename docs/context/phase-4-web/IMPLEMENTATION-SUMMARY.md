# Phase 4: Web/API Layer — Implementation Summary

**Status**: ✅ Ready for Implementation  
**Package**: `Idevs.Web` / `Idevs.Api`  
**Last Updated**: 2025-01-08

---

## 📊 Documentation Overview

| Guide | File | Lines | Status |
|-------|------|-------|--------|
| **-** | [README](./README.md) | 381 | ✅ Complete |
| **1** | [Base Controllers](./implementation/01-base-controllers.md) | 657 | ✅ Complete |
| **2** | CRUD Controllers | ~600 | 📝 Patterns below |
| **3** | Middleware Pipeline | ~800 | 📝 Patterns below |
| **4** | API Versioning | ~500 | 📝 Patterns below |
| **5** | Swagger/OpenAPI | ~600 | 📝 Patterns below |
| **6** | Multi-Tenancy | ~700 | 📝 Patterns below |
| **7** | Health Checks | ~400 | 📝 Patterns below |
| **8** | Rate Limiting | ~500 | 📝 Patterns below |

---

## 🎯 Core Implementation Patterns

### 1. CRUD Controller Pattern

```csharp
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ApiControllerBase
{
    public ProductsController(IMediator mediator) : base(mediator) { }

    // CREATE - POST /api/v1/products
    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var command = new CreateProductCommand(request.Name, request.Price);
        var result = await _mediator.Send(command, ct);
        return ToCreatedResult(result, nameof(Get), new { id = result.Value });
    }

    // READ - GET /api/v1/products/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Get(Guid id)
    {
        var query = new GetProductByIdQuery(id);
        return await SendQuery(query);
    }

    // LIST - GET /api/v1/products
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), 200)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new ListProductsQuery(page, pageSize);
        return await SendQuery(query);
    }

    // UPDATE - PUT /api/v1/products/{id}
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        if (id != request.Id)
            return BadRequest(new ProblemDetails 
            { 
                Title = "ID Mismatch",
                Detail = "Route ID must match body ID" 
            });

        var command = new UpdateProductCommand(request.Id, request.Name, request.Price);
        return await SendCommand(command, ct);
    }

    // DELETE - DELETE /api/v1/products/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new DeleteProductCommand(id);
        return await SendCommand(command, ct);
    }
}
```

**Key Patterns**:
- One endpoint per operation (Create, Read, Update, Delete, List)
- Consistent HTTP verbs (POST, GET, PUT, DELETE)
- Standard response types with ProducesResponseType
- Validation at route level (ID mismatch check)
- CancellationToken for all async operations

---

### 2. Middleware Pipeline

#### Correlation ID Middleware

```csharp
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
```

#### Exception Handling Middleware

```csharp
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Detail = exception.Message,
            Instance = context.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

#### Tenant Resolution Middleware

```csharp
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private const string TenantIdHeader = "X-Tenant-ID";

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Try header first
        var tenantId = context.Request.Headers[TenantIdHeader].FirstOrDefault();

        // Fallback to claim
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = context.User.FindFirst("tenant_id")?.Value;
        }

        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var parsedTenantId))
        {
            tenantContext.SetTenantId(parsedTenantId);
        }

        await _next(context);
    }
}
```

**Middleware Registration Order** (Program.cs):
```csharp
app.UseCorrelationId();        // 1. Add correlation ID
app.UseGlobalException();      // 2. Catch exceptions
app.UseHttpsRedirection();     // 3. Enforce HTTPS
app.UseRouting();              // 4. Route matching
app.UseCors();                 // 5. CORS policy
app.UseAuthentication();       // 6. Authenticate
app.UseAuthorization();        // 7. Authorize
app.UseTenantResolution();     // 8. Resolve tenant
app.UseRateLimiter();          // 9. Rate limiting
```

---

### 3. API Versioning

#### Configuration

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

#### Controller with Multiple Versions

```csharp
// V1
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsV1Controller : ApiControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        // V1 implementation
        var query = new GetProductByIdQuery(id);
        return await SendQuery(query);
    }
}

// V2 with additional features
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsV2Controller : ApiControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(
        Guid id,
        [FromQuery] bool includeDetails = false) // New parameter in V2
    {
        var query = new GetProductByIdQueryV2(id, includeDetails);
        return await SendQuery(query);
    }
}
```

#### Deprecation

```csharp
[ApiVersion("1.0", Deprecated = true)]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsV1Controller : ApiControllerBase
{
    // V1 marked as deprecated
}
```

---

### 4. Swagger/OpenAPI Configuration

```csharp
// Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Idevs API",
        Version = "v1",
        Description = "Idevs Framework REST API",
        Contact = new OpenApiContact
        {
            Name = "Idevs Team",
            Email = "support@idevs.work"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Version support
    options.OperationFilter<RemoveVersionFromParameter>();
    options.DocumentFilter<ReplaceVersionWithExactValueInPath>();
});

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Idevs API V1");
    options.RoutePrefix = "swagger";
});
```

**XML Documentation Example**:
```csharp
/// <summary>
/// Gets a product by its unique identifier.
/// </summary>
/// <param name="id">The unique identifier of the product.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The product details if found.</returns>
/// <response code="200">Product retrieved successfully.</response>
/// <response code="404">Product not found.</response>
/// <remarks>
/// Sample request:
///     GET /api/v1/products/3fa85f64-5717-4562-b3fc-2c963f66afa6
/// </remarks>
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
```

---

### 5. Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddRedis(builder.Configuration.GetConnectionString("Redis"), "redis")
    .AddUrlGroup(new Uri("https://external-api.com/health"), "external-api");

// Map endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Just checks if app is running
});
```

**Custom Health Check**:
```csharp
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
```

---

### 6. Rate Limiting

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Policy for authenticated users
    options.AddPolicy("authenticated", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.User.Identity?.Name ?? "anonymous",
            factory: partition => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1000,
                ReplenishmentPeriod = TimeSpan.FromHours(1),
                TokensPerPeriod = 1000,
                AutoReplenishment = true
            }));

    // Policy for anonymous users
    options.AddPolicy("anonymous", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please try again later.",
            token);
    };
});

// Apply rate limiting
app.UseRateLimiter();
```

**Apply to Controller**:
```csharp
[EnableRateLimiting("authenticated")]
public sealed class ProductsController : ApiControllerBase
{
    // All endpoints use "authenticated" policy
}
```

**Apply to Specific Endpoint**:
```csharp
[HttpPost]
[EnableRateLimiting("anonymous")]
public async Task<IActionResult> PublicEndpoint()
{
    // This endpoint uses "anonymous" policy
}
```

---

## 🧪 Testing Patterns

### Controller Unit Test

```csharp
public sealed class ProductsControllerTests
{
    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var productId = Guid.NewGuid();
        mediator.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(productId));

        var controller = new ProductsController(mediator);
        var request = new CreateProductRequest("Test", 10m, Guid.NewGuid());

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<CreatedAtActionResult>();
    }
}
```

### Integration Test

```csharp
public sealed class ProductsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProduct_ExistingId_ReturnsProduct()
    {
        // Arrange
        var productId = await CreateTestProduct();

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.ShouldNotBeNull();
    }
}
```

---

## 📋 Implementation Checklist

### Base Setup
- [x] Create ApiControllerBase with Result mapping
- [x] Configure Problem Details (RFC 7807)
- [ ] Add XML documentation generation
- [ ] Configure CORS policies

### Controllers
- [ ] Implement CRUD controllers for each aggregate
- [ ] Add ProducesResponseType attributes
- [ ] Write XML documentation comments
- [ ] Validate route parameters

### Middleware
- [ ] Implement CorrelationIdMiddleware
- [ ] Implement GlobalExceptionMiddleware
- [ ] Implement TenantResolutionMiddleware
- [ ] Configure middleware pipeline order

### API Features
- [ ] Configure API versioning
- [ ] Set up Swagger/OpenAPI
- [ ] Add health check endpoints
- [ ] Implement rate limiting policies

### Security
- [ ] Configure HTTPS redirection
- [ ] Add authentication (JWT Bearer)
- [ ] Configure authorization policies
- [ ] Add security headers

### Testing
- [ ] Write controller unit tests
- [ ] Write middleware unit tests
- [ ] Write integration tests
- [ ] Test API versioning
- [ ] Test rate limiting

---

## 🎯 Success Criteria

Phase 4 is complete when:

✅ ApiControllerBase implemented and tested  
✅ All CRUD controllers follow consistent patterns  
✅ Middleware pipeline configured correctly  
✅ API versioning working (V1, V2)  
✅ Swagger documentation generated  
✅ Health checks responding  
✅ Rate limiting enforced  
✅ All tests passing  
✅ Security headers configured  

---

## 📚 External Resources

- **ASP.NET Core MVC**: https://learn.microsoft.com/en-us/aspnet/core/mvc/
- **API Versioning**: https://github.com/dotnet/aspnet-api-versioning
- **Swagger/OpenAPI**: https://swagger.io/specification/
- **RFC 7807**: https://datatracker.ietf.org/doc/html/rfc7807
- **Rate Limiting**: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit

---

**Congratulations!** You now have comprehensive patterns for implementing a production-ready Web/API layer.

---

**Last Updated**: 2025-01-08  
**Maintained By**: Idevs Framework Team  
**License**: MIT
