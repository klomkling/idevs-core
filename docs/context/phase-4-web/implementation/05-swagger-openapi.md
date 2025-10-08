# Guide 5: Swagger/OpenAPI

Phase: 4 - Web/API Layer
Component: Swagger/OpenAPI
Depends On: 04-API Versioning

---

## Overview

Swagger/OpenAPI provides interactive documentation and a contract for clients. This guide configures:
- Per-version Swagger documents
- JWT Bearer authentication support
- XML comments inclusion without using reflection
- Operation and document filters for versioning cleanliness

---

## Packages

```bash
 dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

---

## XML Documentation Setup

Enable XML docs in the project file so comments appear in Swagger:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

Include all XML doc files found in the app base directory (no reflection):

```csharp
// Program.cs
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Idevs API",
        Version = "v1",
        Description = "Idevs Framework REST API",
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    // Security: JWT Bearer
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });

    // XML comments: include all XML files in base directory
    foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
    {
        options.IncludeXmlComments(xml, includeControllerXmlComments: true);
    }

    // Remove version parameter from query (we use URL segment)
    options.OperationFilter<RemoveVersionFromParameter>();
    options.DocumentFilter<ReplaceVersionWithExactValueInPath>();
});
```

---

## Versioned Swagger Documents

Use the ApiVersionDescriptionProvider to generate a document per version.

```csharp
var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"Idevs API {description.GroupName.ToUpperInvariant()}");
    }
});
```

---

## Filters for Versioning Cleanliness

```csharp
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class RemoveVersionFromParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var versionParameter = operation.Parameters.FirstOrDefault(p => p.Name == "version");
        if (versionParameter != null)
        {
            operation.Parameters.Remove(versionParameter);
        }
    }
}

public sealed class ReplaceVersionWithExactValueInPath : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new OpenApiPaths();
        foreach (var (key, value) in swaggerDoc.Paths)
        {
            paths.Add(key.Replace("v{version}", swaggerDoc.Info.Version, StringComparison.OrdinalIgnoreCase), value);
        }
        swaggerDoc.Paths = paths;
    }
}
```

---

## Example Controller XML Comments

```csharp
/// <summary>
/// Retrieves a product by its unique identifier.
/// </summary>
/// <param name="id">Product ID.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The product details.</returns>
/// <response code="200">Success</response>
/// <response code="404">Not found</response>
[HttpGet("{id}")]
public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
```

---

## Common Pitfalls

- Forgetting to enable XML docs in the project file prevents comments from appearing
- Not adding security requirement shows lock icons but doesn’t require auth
- Failing to filter the version parameter clutters operations

---

Last Updated: 2025-01-08
Maintained By: Idevs Framework Team
License: MIT