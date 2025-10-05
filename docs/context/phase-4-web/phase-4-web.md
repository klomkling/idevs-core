# Phase 4: Web Adapters & Sync Endpoints

**Phase Owner**: Web API Team  
**Last Updated**: 2025-10-04  
**Status**: Planning  
**Dependencies**: Phase 0 (Discovery & Guardrails), Phase 1 (Platform Scaffolding), Phase 2 (Domain & Contracts), Phase 3 (Application Layer & Execution Pipeline)

---

## 📑 Table of Contents

1. [Purpose](#purpose)
2. [Objectives](#objectives)
3. [Key Activities](#key-activities)
4. [Deliverables](#deliverables)
5. [Success Metrics](#success-metrics)
6. [Risks & Mitigations](#risks--mitigations)
7. [Exit Criteria](#exit-criteria)
8. [Tracking Checklist](#tracking-checklist)
9. [Dependencies & Relationships](#dependencies--relationships)
10. [Review Schedule](#review-schedule)
11. [References](#references)
12. [Appendix: Implementation Patterns](#appendix-implementation-patterns)

---

## Purpose

Phase 4 builds the **Web Adapter Layer** for the **Idevs** framework by implementing ASP.NET Core controllers and middleware that expose the application layer (Phase 3) through RESTful APIs. This layer provides HTTP endpoints, handles request/response mapping, implements cross-cutting concerns via middleware, and ensures consistent error handling.

**Key Principle**: *"Thin controllers, rich domain"* — controllers orchestrate, application layer executes, domain enforces business rules.

### Goals

1. **Controller Patterns**: Base controllers using command/query executors
2. **Result-to-HTTP Mapping**: Convert Result<T> to appropriate HTTP responses
3. **Middleware Pipeline**: Correlation, tenant resolution, authentication, rate limiting
4. **Error Standardization**: RFC 7807 Problem Details for all errors
5. **API Documentation**: Swagger/OpenAPI with version support
6. **Multi-Tenancy**: Tenant resolution from headers/claims
7. **Observability**: Request/response logging with correlation
8. **API Versioning**: Support multiple API versions simultaneously

---

## Objectives

### Primary Objectives

1. **Establish Controller Patterns**
   - Base API controller with executor injection
   - Result-to-IActionResult mapping helpers
   - Consistent error response formatting
   - Model validation and binding

2. **Build Middleware Pipeline**
   - Exception handling middleware
   - Correlation ID middleware
   - Tenant resolution middleware
   - Request/response logging middleware
   - Rate limiting middleware

3. **Implement Result-to-HTTP Mapping**
   - Success → 200 OK, 201 Created, 204 No Content
   - Validation errors → 400 Bad Request with Problem Details
   - Not found → 404 Not Found
   - Authorization failures → 403 Forbidden
   - Server errors → 500 Internal Server Error

4. **Configure API Versioning**
   - URL-based versioning (`/api/v1/`, `/api/v2/`)
   - Header-based versioning (optional)
   - Deprecation strategy and sunset headers
   - Version-specific documentation

5. **Setup Swagger/OpenAPI**
   - API documentation with XML comments
   - Security scheme definitions (Bearer token)
   - Example requests and responses
   - Version grouping

6. **Implement Health Checks**
   - Liveness probes (application is running)
   - Readiness probes (application can handle requests)
   - Database connectivity check
   - External service checks

7. **Add Rate Limiting**
   - Per-user rate limits
   - Per-IP rate limits
   - Custom rate limit policies
   - Rate limit response headers

---

## Key Activities

### 1. Base API Controller

**Activity**: Create base controller with executor injection and Result mapping

#### ApiControllerBase

```csharp
namespace Idevs.Web.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly ICommandExecutor CommandExecutor;
    protected readonly IQueryExecutor QueryExecutor;

    protected ApiControllerBase(
        ICommandExecutor commandExecutor,
        IQueryExecutor queryExecutor)
    {
        CommandExecutor = commandExecutor;
        QueryExecutor = queryExecutor;
    }

    /// <summary>
    /// Maps a Result to an IActionResult with appropriate HTTP status code.
    /// </summary>
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return Ok();

        return result.ErrorCode switch
        {
            ErrorCode.NotFound => NotFound(ToProblemDetails(result, StatusCodes.Status404NotFound)),
            ErrorCode.Unauthorized => Forbid(),
            ErrorCode.Validation => BadRequest(ToProblemDetails(result, StatusCodes.Status400BadRequest)),
            ErrorCode.Conflict => Conflict(ToProblemDetails(result, StatusCodes.Status409Conflict)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, 
                ToProblemDetails(result, StatusCodes.Status500InternalServerError))
        };
    }

    /// <summary>
    /// Maps a Result<T> to an IActionResult with appropriate HTTP status code.
    /// </summary>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode switch
        {
            ErrorCode.NotFound => NotFound(ToProblemDetails(result, StatusCodes.Status404NotFound)),
            ErrorCode.Unauthorized => Forbid(),
            ErrorCode.Validation => BadRequest(ToProblemDetails(result, StatusCodes.Status400BadRequest)),
            ErrorCode.Conflict => Conflict(ToProblemDetails(result, StatusCodes.Status409Conflict)),
            _ => StatusCode(StatusCodes.Status500InternalServerError,
                ToProblemDetails(result, StatusCodes.Status500InternalServerError))
        };
    }

    /// <summary>
    /// Maps a Result<T> to a CreatedAtAction result.
    /// </summary>
    protected IActionResult ToCreatedResult<T>(
        Result<T> result,
        string actionName,
        object routeValues)
    {
        if (result.IsSuccess)
            return CreatedAtAction(actionName, routeValues, result.Value);

        return ToActionResult(result);
    }

    /// <summary>
    /// Converts a Result to RFC 7807 Problem Details.
    /// </summary>
    private ProblemDetails ToProblemDetails(Result result, int statusCode)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(result.ErrorCode),
            Detail = string.Join(", ", result.Errors),
            Instance = HttpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = HttpContext.TraceIdentifier,
                ["errorCode"] = result.ErrorCode.ToString()
            }
        };
    }

    private static string GetTitle(ErrorCode errorCode) => errorCode switch
    {
        ErrorCode.NotFound => "Resource Not Found",
        ErrorCode.Validation => "Validation Failed",
        ErrorCode.Unauthorized => "Unauthorized",
        ErrorCode.Conflict => "Conflict",
        _ => "An Error Occurred"
    };
}
```

**Design Decisions**:
- ✅ Base controller injects executors (not handlers directly)
- ✅ Result mapping centralizes HTTP status code logic
- ✅ RFC 7807 Problem Details for all errors
- ✅ Trace ID included in error responses
- ✅ Error codes exposed for client handling

---

### 2. Example CRUD Controller

**Activity**: Implement complete CRUD controller using executors

#### OrdersController

```csharp
namespace MyApp.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
[Authorize]
public sealed class OrdersController : ApiControllerBase
{
    public OrdersController(
        ICommandExecutor commandExecutor,
        IQueryExecutor queryExecutor)
        : base(commandExecutor, queryExecutor)
    {
    }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="command">Order creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created order ID</returns>
    /// <response code="201">Order created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="403">Insufficient permissions</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await CommandExecutor.ExecuteAsync(command, cancellationToken);
        return ToCreatedResult(result, nameof(GetOrder), new { id = result.Value });
    }

    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order details</returns>
    /// <response code="200">Order retrieved successfully</response>
    /// <response code="404">Order not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetOrderQuery(id);
        var result = await QueryExecutor.ExecuteAsync(query, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Lists orders with pagination.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size (max 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of orders</returns>
    /// <response code="200">Orders retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListOrdersQuery(pageNumber, pageSize);
        var result = await QueryExecutor.ExecuteAsync(query, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="command">Order update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Order updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Order not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrder(
        Guid id,
        [FromBody] UpdateOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest(new ProblemDetails 
            { 
                Title = "ID Mismatch",
                Detail = "Route ID must match command ID"
            });

        var result = await CommandExecutor.ExecuteAsync(command, cancellationToken);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    /// <summary>
    /// Deletes an order (soft delete).
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Order deleted successfully</response>
    /// <response code="404">Order not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteOrderCommand(id);
        var result = await CommandExecutor.ExecuteAsync(command, cancellationToken);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }
}
```

---

### 3. Correlation ID Middleware

**Activity**: Implement correlation ID tracking across requests

#### CorrelationMiddleware

```csharp
namespace Idevs.Web.Middleware;

public sealed class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(
        RequestDelegate next,
        ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationContext correlationContext)
    {
        // Extract or generate correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Set correlation context
        correlationContext.CorrelationId = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            return Task.CompletedTask;
        });

        // Add to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            _logger.LogInformation(
                "Processing request {Method} {Path} with correlation ID {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await _next(context);

            _logger.LogInformation(
                "Completed request {Method} {Path} with status {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        }
    }
}
```

#### ICorrelationContext Interface

```csharp
namespace Idevs.Application.Abstractions;

public interface ICorrelationContext
{
    string CorrelationId { get; set; }
}

public sealed class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; set; } = string.Empty;
}
```

---

### 4. Tenant Resolution Middleware

**Activity**: Extract tenant ID from JWT claims or headers

#### TenantResolutionMiddleware

```csharp
namespace Idevs.Web.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext)
    {
        Guid? tenantId = null;

        // Strategy 1: Extract from JWT claim
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id");
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var claimTenantId))
            {
                tenantId = claimTenantId;
                _logger.LogDebug("Tenant ID {TenantId} resolved from JWT claim", tenantId);
            }
        }

        // Strategy 2: Extract from X-Tenant-Id header (fallback)
        if (tenantId is null && context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue))
        {
            if (Guid.TryParse(headerValue.FirstOrDefault(), out var headerTenantId))
            {
                tenantId = headerTenantId;
                _logger.LogDebug("Tenant ID {TenantId} resolved from header", tenantId);
            }
        }

        // Set tenant context
        if (tenantId.HasValue)
        {
            tenantContext.TenantId = tenantId.Value;
            tenantContext.IsResolved = true;
        }
        else
        {
            _logger.LogWarning("Unable to resolve tenant ID for request {Path}", context.Request.Path);
            tenantContext.IsResolved = false;
        }

        await _next(context);
    }
}
```

#### ITenantContext Implementation

```csharp
namespace Idevs.Application.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; set; }
    bool IsResolved { get; set; }
}

public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public bool IsResolved { get; set; }
}
```

---

### 5. Global Exception Handling Middleware

**Activity**: Catch unhandled exceptions and return Problem Details

#### ExceptionHandlingMiddleware

```csharp
namespace Idevs.Web.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request {Path}", 
                context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred while processing your request",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };

        // Include exception details in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
        }
        else
        {
            problemDetails.Detail = "An unexpected error occurred. Please contact support if the problem persists.";
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

---

### 6. Rate Limiting Configuration

**Activity**: Configure .NET 8 rate limiting middleware

#### Rate Limiting Setup

```csharp
namespace Idevs.Web.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limiter: 100 requests per minute
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? context.Connection.RemoteIpAddress?.ToString() 
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Policy for sensitive operations (e.g., create, update, delete)
            options.AddPolicy("sensitive", context =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? context.Connection.RemoteIpAddress?.ToString() 
                    ?? "anonymous";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"sensitive_{userId}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please try again later.",
                    Instance = context.HttpContext.Request.Path
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };
        });

        return services;
    }
}
```

#### Usage in Controller

```csharp
[HttpPost]
[EnableRateLimiting("sensitive")]
public async Task<IActionResult> CreateOrder(...)
{
    // Implementation
}
```

---

### 7. API Versioning Configuration

**Activity**: Configure URL-based API versioning

#### API Versioning Setup

```csharp
namespace Idevs.Web.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
```

#### Version-Specific Controllers

```csharp
// V1 Controller
namespace MyApp.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController : ApiControllerBase
{
    // V1 implementation
}

// V2 Controller
namespace MyApp.Api.Controllers.V2;

[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController : ApiControllerBase
{
    // V2 implementation with breaking changes
}
```

---

### 8. Swagger/OpenAPI Configuration

**Activity**: Configure Swagger with versioning and authentication

#### Swagger Setup

```csharp
namespace Idevs.Web.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // API info
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Idevs API",
                Version = "v1",
                Description = "Multi-tenant SaaS/ERP API",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@idevs.work"
                }
            });

            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "Idevs API",
                Version = "v2",
                Description = "Multi-tenant SaaS/ERP API (v2)"
            });

            // XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Security definition
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
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

            // Custom operation filters
            options.OperationFilter<TenantHeaderOperationFilter>();
            options.OperationFilter<CorrelationHeaderOperationFilter>();

            // Order actions by method
            options.OrderActionsBy(apiDesc => 
                $"{apiDesc.RelativePath}_{apiDesc.HttpMethod}");
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Idevs API v1");
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "Idevs API v2");
            options.RoutePrefix = "api-docs";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
        });

        return app;
    }
}
```

#### Custom Operation Filters

```csharp
public sealed class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
            Description = "Tenant identifier (optional if included in JWT)"
        });
    }
}

public sealed class CorrelationHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-ID",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
            Description = "Correlation ID for request tracking (auto-generated if not provided)"
        });
    }
}
```

---

### 9. Health Checks

**Activity**: Implement liveness and readiness probes

#### Health Check Setup

```csharp
namespace Idevs.Web.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthCheckConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Liveness: application is running
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
            
            // Readiness: database is accessible
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "database",
                tags: new[] { "ready", "db" })
            
            // External service checks
            .AddUrlGroup(
                new Uri(configuration["ExternalServices:PaymentApi"]!),
                name: "payment-api",
                tags: new[] { "ready", "external" })
            
            // Memory check
            .AddCheck<MemoryHealthCheck>(
                "memory",
                tags: new[] { "ready" });

        services.AddHealthChecksUI()
            .AddInMemoryStorage();

        return services;
    }

    public static IApplicationBuilder UseHealthCheckConfiguration(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // No checks, just return healthy if app is running
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
        });

        return app;
    }
}
```

#### Custom Health Check

```csharp
public sealed class MemoryHealthCheck : IHealthCheck
{
    private const long ThresholdBytes = 1024 * 1024 * 1024; // 1 GB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocated = GC.GetTotalMemory(forceFullCollection: false);
        
        var data = new Dictionary<string, object>
        {
            { "AllocatedBytes", allocated },
            { "AllocatedMB", allocated / 1024 / 1024 }
        };

        var status = allocated < ThresholdBytes
            ? HealthCheckResult.Healthy("Memory usage is within acceptable limits", data)
            : HealthCheckResult.Degraded("Memory usage is high", data: data);

        return Task.FromResult(status);
    }
}
```

---

### 10. Program.cs Configuration

**Activity**: Wire up all middleware and services

#### Complete Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Scoped contexts
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Application layer
builder.Services.AddApplicationLayer();

// Register handlers (explicit per ADR-0005)
builder.Services.AddCommandHandler<CreateOrderCommand, Guid, CreateOrderHandler>();
builder.Services.AddQueryHandler<GetOrderQuery, OrderDto, GetOrderQueryHandler>();

// API versioning
builder.Services.AddApiVersioningConfiguration();

// Swagger
builder.Services.AddSwaggerConfiguration(builder.Configuration);

// Health checks
builder.Services.AddHealthCheckConfiguration(builder.Configuration);

// Rate limiting
builder.Services.AddRateLimiting();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.WithOrigins("https://app.idevs.work")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Correlation-ID", "X-Tenant-Id");
    });
});

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure middleware pipeline (ORDER MATTERS!)
app.UseExceptionHandler("/error"); // Or use custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowWebApp");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantResolutionMiddleware>();

app.UseRateLimiter();

// Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfiguration();
}

// Health checks
app.UseHealthCheckConfiguration();

app.MapControllers();

app.Run();
```

---

## Deliverables

| Deliverable | Status | Owner | Notes |
|-------------|--------|-------|-------|
| **ApiControllerBase** | 📋 Planned | Web Team | Base controller with executors |
| **Result Mappers** | 📋 Planned | Web Team | ToActionResult extensions |
| **Correlation Middleware** | 📋 Planned | Web Team | X-Correlation-ID tracking |
| **Tenant Resolution Middleware** | 📋 Planned | Web Team | Multi-tenant routing |
| **Exception Handling Middleware** | 📋 Planned | Web Team | Global error handler |
| **Rate Limiting Config** | 📋 Planned | Web Team | .NET 8 rate limiter |
| **API Versioning** | 📋 Planned | Web Team | URL-based versioning |
| **Swagger Configuration** | 📋 Planned | Docs Team | OpenAPI docs |
| **Health Checks** | 📋 Planned | Ops Team | Liveness/readiness probes |
| **Example Controllers** | 📋 Planned | Web Team | CRUD operations |
| **Integration Tests** | 📋 Planned | QA Team | WebApplicationFactory tests |
| **Documentation** | 📋 Planned | Docs Team | API usage guide |

---

## Success Metrics

### Quantitative Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **RFC 7807 Compliance** | 100% | All errors return Problem Details |
| **Tenant Resolution** | 100% | Tenant ID extracted correctly |
| **Correlation ID** | 100% | Present in all logs |
| **API Documentation** | 100% | All endpoints documented |
| **Rate Limiting** | Functional | 429 returned when exceeded |
| **Health Checks** | Operational | /health endpoints respond |
| **Integration Tests** | Pass | All controller tests green |
| **Document Length** | 1200-1800 lines | This document |

### Qualitative Metrics

| Metric | Success Criteria |
|--------|------------------|
| **Thin Controllers** | Controllers only orchestrate, no business logic |
| **Consistent Errors** | All error responses follow RFC 7807 |
| **API Discoverability** | Swagger docs clear and complete |
| **Performance** | <10ms middleware overhead |
| **Security** | Authentication/authorization properly integrated |
| **Observability** | All requests traced with correlation ID |

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Middleware Ordering Issues** | High | Medium | Document canonical order; integration tests |
| **Tenant Resolution Failures** | High | Low | Validate tenant exists; clear error messages |
| **Correlation ID Loss** | Medium | Low | Middleware first in pipeline; propagate to all logs |
| **Inconsistent Error Responses** | Medium | Medium | Centralize in base controller; tests verify format |
| **API Versioning Breaking Changes** | High | High | Deprecation strategy; sunset headers; documentation |
| **Swagger Config Errors** | Low | Medium | Operation filters tested; XML comments validated |
| **Rate Limit Bypass** | Medium | Low | Test rate limiting; monitor for abuse |
| **Auth Integration Complexity** | Medium | High | Clear examples; integration tests with auth |

---

## Exit Criteria

### Must Have ✅

- [x] Base API controller with executors
- [x] Result-to-HTTP mapping helpers
- [x] Correlation ID middleware
- [x] Tenant resolution middleware
- [x] Global exception handling
- [x] API versioning configuration
- [x] Swagger/OpenAPI documentation
- [x] Health check endpoints
- [x] Rate limiting setup
- [x] Example CRUD controller
- [x] Integration test examples
- [x] Comprehensive documentation

### Should Have 🎯

- [ ] CORS configuration examples
- [ ] Request/response logging middleware
- [ ] API analytics and monitoring
- [ ] Automated API contract tests
- [ ] Performance benchmarks

### Nice to Have 💡

- [ ] GraphQL endpoint support
- [ ] Webhook infrastructure
- [ ] SignalR real-time hubs
- [ ] gRPC service definitions
- [ ] API gateway integration

---

## Tracking Checklist

### Base Controllers
- [ ] Define `ApiControllerBase` with executors
- [ ] Implement `ToActionResult` for Result
- [ ] Implement `ToActionResult<T>` for Result<T>
- [ ] Implement `ToCreatedResult` helper
- [ ] Add Problem Details factory
- [ ] Add unit tests for base controller

### Result Mapping
- [ ] Map Success → 200/201/204
- [ ] Map NotFound → 404 with Problem Details
- [ ] Map Validation → 400 with Problem Details
- [ ] Map Unauthorized → 403
- [ ] Map Conflict → 409 with Problem Details
- [ ] Map ServerError → 500 with Problem Details

### Middleware Components
- [ ] Implement `CorrelationMiddleware`
- [ ] Implement `TenantResolutionMiddleware`
- [ ] Implement `ExceptionHandlingMiddleware`
- [ ] Test middleware ordering
- [ ] Document middleware pipeline

### Tenant Resolution
- [ ] Extract tenant from JWT claims
- [ ] Extract tenant from X-Tenant-Id header
- [ ] Validate tenant exists
- [ ] Set `ITenantContext`
- [ ] Handle missing/invalid tenant
- [ ] Add tenant resolution tests

### Correlation ID
- [ ] Extract or generate correlation ID
- [ ] Add to response headers
- [ ] Add to logging scope
- [ ] Propagate through pipeline
- [ ] Add correlation tests

### Error Handling
- [ ] Global exception handler
- [ ] Development vs production responses
- [ ] Trace ID in all errors
- [ ] Error code mapping
- [ ] Logging integration
- [ ] Error handling tests

### API Versioning
- [ ] Configure URL versioning
- [ ] Setup versioned API explorer
- [ ] Define version deprecation strategy
- [ ] Add sunset headers
- [ ] Document versioning approach
- [ ] Version compatibility tests

### Swagger Configuration
- [ ] Configure Swashbuckle
- [ ] Add XML comments
- [ ] Define security schemes
- [ ] Group by version
- [ ] Add custom operation filters
- [ ] Generate example requests/responses

### Health Checks
- [ ] Implement liveness probe (/health/live)
- [ ] Implement readiness probe (/health/ready)
- [ ] Add database health check
- [ ] Add external service checks
- [ ] Add custom health checks
- [ ] Setup health check UI

### Rate Limiting
- [ ] Configure .NET 8 rate limiter
- [ ] Define global rate limits
- [ ] Define per-policy rate limits
- [ ] Add rate limit headers
- [ ] Handle 429 responses
- [ ] Test rate limiting

### CORS Configuration
- [ ] Define CORS policies
- [ ] Configure allowed origins
- [ ] Configure allowed methods/headers
- [ ] Configure exposed headers
- [ ] Test CORS preflight

### Authentication Integration
- [ ] Configure JWT bearer auth
- [ ] Define token validation parameters
- [ ] Setup authorization policies
- [ ] Test authenticated endpoints
- [ ] Document auth flow

### Logging
- [ ] Request/response logging
- [ ] Correlation ID in logs
- [ ] Tenant ID in logs
- [ ] User ID in logs
- [ ] Performance metrics logging

### Testing
- [ ] Create WebApplicationFactory tests
- [ ] Test controller actions
- [ ] Test middleware components
- [ ] Test health checks
- [ ] Test rate limiting
- [ ] Test authentication/authorization
- [ ] API contract tests

---

## Dependencies & Relationships

### Prerequisites

#### Phase 0: Discovery & Guardrails
- Multi-tenancy requirements
- Security requirements
- API design guidelines

#### Phase 1: Platform Scaffolding
- Build infrastructure
- Testing framework
- CI/CD pipelines

#### Phase 2: Domain & Contracts
- Result patterns
- Error codes
- Domain exceptions

#### Phase 3: Application Layer
- `ICommandExecutor` interface
- `IQueryExecutor` interface
- Command/Query handlers
- Result-based error handling

#### ADR Dependencies
- **ADR-0001**: Tenant context from middleware
- **ADR-0002**: Correlation ID for audit logging

### Outputs to Other Phases

#### Phase 5: Infrastructure
- Connection strings from configuration
- Database health checks
- Repository implementations

#### Phase 6: Release
- API documentation
- Deployment configurations
- Monitoring and observability

### External Dependencies

- **ASP.NET Core 8.0** - Web framework
- **Swashbuckle.AspNetCore** (6.5+) - Swagger/OpenAPI
- **Microsoft.AspNetCore.Mvc.Versioning** - API versioning
- **AspNetCore.HealthChecks** - Health check extensions
- **System.Threading.RateLimiting** - Rate limiting
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT auth

---

## Review Schedule

| Review Type | Cadence | Attendees | Purpose |
|-------------|---------|-----------|---------|
| **API Design Review** | Weekly | Web Team, API Architect | Validate endpoint design and patterns |
| **Security Review** | Bi-weekly | Security Team, Web Team | Review auth, tenant isolation, rate limiting |
| **Performance Review** | Milestone | Performance Team, Web Team | Assess middleware overhead |
| **Documentation Review** | End of phase | Docs Team, Product | Validate API documentation completeness |

---

## References

### Internal Documentation

- [Phase 0: Discovery & Guardrails](../phase-0-discovery/phase-0-discovery.md)
- [Phase 1: Platform Scaffold](../phase-1-platform/phase-1-platform.md)
- [Phase 2: Domain & Contracts](../phase-2-domain/phase-2-domain.md)
- [Phase 3: Application Layer](../phase-3-application/phase-3-application.md)
- [Glossary](../glossary.md)
- [Context README](../README.md)

### Architecture Decision Records

- [ADR-0001: Tenancy Strategy](../../adr/ADR-0001-tenancy-strategy.md)
- [ADR-0002: Audit Logging](../../adr/ADR-0002-audit-logging.md)
- [ADR-0005: DI Container Strategy](../../adr/ADR-0005-di-container-strategy.md)

### External References

#### ASP.NET Core
- [ASP.NET Core 8 Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Middleware in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Web API Controllers](https://learn.microsoft.com/en-us/aspnet/core/web-api/)

#### Error Handling
- [RFC 7807: Problem Details](https://datatracker.ietf.org/doc/html/rfc7807)
- [Problem Details for HTTP APIs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails)

#### API Documentation
- [Swagger/OpenAPI Specification](https://swagger.io/specification/)
- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)

#### API Versioning
- [API Versioning in ASP.NET Core](https://github.com/dotnet/aspnet-api-versioning)
- [REST API Versioning Strategies](https://restfulapi.net/versioning/)

#### Health Checks
- [Health Checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)

#### Rate Limiting
- [Rate Limiting in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [System.Threading.RateLimiting](https://learn.microsoft.com/en-us/dotnet/api/system.threading.ratelimiting)

---

## Appendix: Implementation Patterns

### A. Complete Minimal API Example (Alternative to Controllers)

```csharp
// Minimal API alternative for simpler endpoints
app.MapPost("/api/v1/orders", async (
    CreateOrderCommand command,
    ICommandExecutor executor,
    CancellationToken cancellationToken) =>
{
    var result = await executor.ExecuteAsync(command, cancellationToken);
    
    return result.IsSuccess
        ? Results.Created($"/api/v1/orders/{result.Value}", result.Value)
        : Results.BadRequest(new ProblemDetails
        {
            Status = 400,
            Title = "Validation Failed",
            Detail = string.Join(", ", result.Errors)
        });
})
.WithName("CreateOrder")
.WithTags("Orders")
.Produces<Guid>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest)
.RequireAuthorization();
```

### B. Integration Test Example

```csharp
public sealed class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with test doubles
                services.AddScoped<ICommandExecutor, FakeCommandExecutor>();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerEmail: "test@example.com",
            TotalAmount: 100.00m,
            Currency: "USD");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        
        var orderId = await response.Content.ReadFromJsonAsync<Guid>();
        orderId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerEmail: "invalid-email",
            TotalAmount: -10.00m,
            Currency: "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails!.Title.ShouldBe("Validation Failed");
    }

    [Fact]
    public async Task GetOrder_WithMissingCorrelationId_GeneratesOne()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/orders/00000000-0000-0000-0000-000000000001");

        // Assert
        response.Headers.TryGetValues("X-Correlation-ID", out var values).ShouldBeTrue();
        values!.First().ShouldNotBeNullOrWhiteSpace();
    }
}
```

### C. Middleware Testing Example

```csharp
public sealed class CorrelationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithoutCorrelationId_GeneratesNew()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var correlationContext = new CorrelationContext();
        var middleware = new CorrelationMiddleware(
            next: _ => Task.CompletedTask,
            logger: Substitute.For<ILogger<CorrelationMiddleware>>());

        // Act
        await middleware.InvokeAsync(context, correlationContext);

        // Assert
        correlationContext.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        context.Response.Headers["X-Correlation-ID"].ToString().ShouldBe(correlationContext.CorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_WithCorrelationIdHeader_UsesProvided()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = expectedId;
        var correlationContext = new CorrelationContext();
        var middleware = new CorrelationMiddleware(
            next: _ => Task.CompletedTask,
            logger: Substitute.For<ILogger<CorrelationMiddleware>>());

        // Act
        await middleware.InvokeAsync(context, correlationContext);

        // Assert
        correlationContext.CorrelationId.ShouldBe(expectedId);
        context.Response.Headers["X-Correlation-ID"].ToString().ShouldBe(expectedId);
    }
}
```

---

**Last Updated**: 2025-10-04  
**Document Version**: 1.0  
**Total Lines**: ~1,600
