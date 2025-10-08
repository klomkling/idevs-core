# Phase 6: API Documentation

[← Back to Phase 6 Overview](../phase-6-release.md)

## Overview

Generate comprehensive API documentation using XML documentation comments and DocFX. This ensures all public APIs are well-documented with examples, remarks, and cross-references.

## XML Documentation Standards

All public APIs must include complete XML documentation following these standards:

### Required Elements

- `<summary>` - Brief description (1-2 sentences)
- `<param>` - Description for each parameter
- `<returns>` - Description of return value
- `<exception>` - Documented exceptions that may be thrown

### Recommended Elements

- `<remarks>` - Additional context and usage notes
- `<example>` or `<code>` - Usage examples
- `<seealso>` - Related types and members
- `<typeparam>` - Generic type parameter descriptions

### Example: Complete XML Documentation

```csharp
namespace Idevs.Application.Abstractions;

/// <summary>
/// Executes commands in the application layer with cross-cutting concerns applied.
/// </summary>
/// <remarks>
/// The command executor applies behaviors in a decorator pattern:
/// <list type="number">
///   <item>Validation (FluentValidation)</item>
///   <item>Authorization (policy-based)</item>
///   <item>Logging (structured with correlation)</item>
///   <item>Transaction management</item>
/// </list>
///
/// Example usage:
/// <code>
/// var command = new CreateOrderCommand(
///     CustomerEmail: "customer@example.com",
///     TotalAmount: 100.00m,
///     Currency: "USD"
/// );
///
/// var result = await _commandExecutor.ExecuteAsync(command, cancellationToken);
///
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Order created: {result.Value}");
/// }
/// </code>
/// </remarks>
/// <seealso cref="IQueryExecutor"/>
/// <seealso cref="ICommandHandler{TCommand, TResult}"/>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes a command that returns no result.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to execute.</typeparam>
    /// <param name="command">The command instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    /// <exception cref="ValidationException">Thrown when command validation fails.</exception>
    Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Executes a command that returns a typed result.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to execute.</typeparam>
    /// <typeparam name="TResult">The type of result returned.</typeparam>
    /// <param name="command">The command instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the typed value or errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    /// <exception cref="ValidationException">Thrown when command validation fails.</exception>
    Task<Result<TResult>> ExecuteAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
```

## DocFX Setup

DocFX generates static API documentation websites from XML comments.

### Installation

```bash
dotnet tool install --global docfx
```

### Configuration File

Create `docfx.json` in the `docs/` directory:

```json
{
  "metadata": [
    {
      "src": [
        {
          "src": "../src",
          "files": [
            "Idevs/**.csproj",
            "Idevs.Application/**.csproj",
            "Idevs.Domain/**.csproj",
            "Idevs.Infrastructure/**.csproj",
            "Idevs.Web/**.csproj"
          ]
        }
      ],
      "dest": "api",
      "disableGitFeatures": false,
      "disableDefaultFilter": false
    }
  ],
  "build": {
    "content": [
      {
        "files": [
          "api/**.yml",
          "api/index.md"
        ]
      },
      {
        "files": [
          "articles/**.md",
          "articles/**/toc.yml",
          "toc.yml",
          "*.md"
        ]
      }
    ],
    "resource": [
      {
        "files": [
          "images/**"
        ]
      }
    ],
    "dest": "_site",
    "globalMetadataFiles": [],
    "fileMetadataFiles": [],
    "template": [
      "default",
      "modern"
    ],
    "postProcessors": [],
    "keepFileLink": false,
    "disableGitFeatures": false
  }
}
```

### Build Commands

```bash
# Generate API metadata
docfx metadata docs/docfx.json

# Build the documentation site
docfx build docs/docfx.json

# Serve locally for preview
docfx serve docs/_site
```

## Project Configuration

Enable XML documentation generation in your `.csproj` files:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comment warnings -->
</PropertyGroup>
```

## Documentation Structure

```text
docs/
├── docfx.json              # DocFX configuration
├── index.md                # Landing page
├── toc.yml                 # Table of contents
├── api/                    # Generated API reference
│   └── (generated files)
├── articles/               # Conceptual documentation
│   ├── getting-started.md
│   ├── architecture.md
│   ├── patterns.md
│   └── toc.yml
└── images/                 # Diagrams and screenshots
```

## Best Practices

### DO ✅

- Write summaries in present tense ("Executes..." not "Execute...")
- Include code examples for complex APIs
- Document all exceptions that may be thrown
- Use `<seealso>` to link related types
- Keep summaries concise (1-2 sentences)
- Add `<remarks>` for important details

### DON'T ❌

- Copy-paste generic descriptions
- Document obvious parameters (e.g., "cancellationToken - The cancellation token")
- Use internal implementation details in public docs
- Include TODO comments in XML docs
- Forget to update docs when changing signatures

## Automated Validation

Add documentation checks to CI/CD:

```yaml
# .github/workflows/docs.yml
name: Documentation

on: [push, pull_request]

jobs:
  validate-docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install DocFX
        run: dotnet tool install --global docfx
      
      - name: Build Documentation
        run: docfx build docs/docfx.json
      
      - name: Check for Warnings
        run: |
          if grep -q "warning" docs/_site/build.log; then
            echo "Documentation has warnings"
            exit 1
          fi
```

## Deployment

### GitHub Pages

Deploy to GitHub Pages automatically:

```yaml
- name: Deploy to GitHub Pages
  uses: peaceiris/actions-gh-pages@v3
  with:
    github_token: ${{ secrets.GITHUB_TOKEN }}
    publish_dir: ./docs/_site
```

### Azure Static Web Apps

Deploy to Azure:

```bash
az staticwebapp create \
  --name idevs-docs \
  --resource-group idevs \
  --source docs/_site
```

## Next Steps

- **[Getting Started Guide](02-getting-started.md)** - Quick start for users
- **[Sample Applications](03-sample-applications.md)** - Example projects
- **[Package Configuration](04-package-configuration.md)** - NuGet setup

---

[← Back to Phase 6 Overview](../phase-6-release.md) | [Next: Getting Started →](02-getting-started.md)
