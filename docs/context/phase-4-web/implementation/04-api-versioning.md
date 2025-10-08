# Guide 4: API Versioning

Phase: 4 - Web/API Layer
Component: API Versioning
Depends On: 01-Base Controllers, 02-CRUD Controllers

---

## Overview

Versioning allows you to evolve APIs without breaking existing clients. This guide uses the official aspnet-api-versioning packages to support:
- URL segment versioning (preferred)
- Optional header-based versioning
- API Explorer integration for Swagger grouping
- Deprecation management

---

## Packages

```bash
 dotnet add package Microsoft.AspNetCore.Mvc.Versioning --version 5.1.0
 dotnet add package Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer --version 5.1.0
```

---

## Configuration

```csharp
// Program.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;

    // Support URL segment and optional header
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // e.g., v1, v1.1, v2
    options.SubstituteApiVersionInUrl = true;
});
```

---

## Controllers by Version

```csharp
// V1 controller
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsV1Controller : ApiControllerBase
{
    public ProductsV1Controller(IMediator mediator) : base(mediator) {}

    [HttpGet("{id}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct)
        => SendQuery(new GetProductByIdQuery(id), ct);
}

// V2 controller with new features
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsV2Controller : ApiControllerBase
{
    public ProductsV2Controller(IMediator mediator) : base(mediator) {}

    [HttpGet("{id}")]
    public Task<IActionResult> Get(Guid id, [FromQuery] bool includeDetails, CancellationToken ct)
        => SendQuery(new GetProductByIdQueryV2(id, includeDetails), ct);
}
```

---

## Deprecation

Mark a version as deprecated to guide clients to upgrade.

```csharp
[ApiVersion("1.0", Deprecated = true)]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsV1Controller : ApiControllerBase { }
```

The response will include headers like: "api-deprecated-versions: 1.0" when combined with ReportApiVersions.

---

## Versioning Conventions

- Keep breaking changes in new major versions (e.g., v2)
- Avoid introducing breaking changes in minor versions
- Duplicate controllers per version only when behavior diverges
- Backport only critical fixes to older versions

---

## Swagger Integration

Leverage VersionedApiExplorer to generate a Swagger document per version. See Guide 5 for full Swagger configuration. Example snippet:

```csharp
var provider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"Idevs API {description.GroupName.ToUpperInvariant()}");
    }
});
```

Note: Create ServiceProvider only for bootstrapping UI; prefer to resolve in minimal temporary scope at startup.

---

## Tests

```csharp
public sealed class VersioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public VersioningTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task V1_GetProduct_Returns200()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/products/{id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetProduct_WithDetails_Returns200()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v2/products/{id}?includeDetails=true");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

---

## Common Pitfalls

- Mixing major versions in the same controller leads to ambiguity; split controllers per version
- Forgetting SubstituteApiVersionInUrl = true breaks URL segment replacement
- Omitting ReportApiVersions makes client upgrades harder

---

Last Updated: 2025-01-08
Maintained By: Idevs Framework Team
License: MIT