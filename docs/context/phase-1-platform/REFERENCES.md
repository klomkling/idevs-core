# Phase 1: External References

## Official Documentation

### .NET Platform

- [.NET SDK Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [.NET 8 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [.NET CLI Overview](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [global.json Overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)

### Build & Configuration

- [MSBuild Reference](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild)
- [Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)
- [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [EditorConfig Documentation](https://editorconfig.org/)

### Versioning & Git

- [GitVersion Documentation](https://gitversion.net/docs/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Git Flow](https://nvie.com/posts/a-successful-git-branching-model/)
- [Semantic Versioning](https://semver.org/)

### CI/CD

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [GitHub Actions: Service Containers](https://docs.github.com/en/actions/using-containerized-services/about-service-containers)
- [GitHub Actions: Workflow Syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)
- [GitHub Packages](https://docs.github.com/en/packages)

### Testing

- [xUnit Documentation](https://xunit.net/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [EF Core InMemory Provider](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)
- [Coverlet Code Coverage](https://github.com/coverlet-coverage/coverlet)
- [NetArchTest](https://github.com/BenMorris/NetArchTest)

### NuGet Packaging

- [NuGet Package Creation](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package)
- [NuGet Package Metadata](https://learn.microsoft.com/en-us/nuget/reference/nuspec)
- [SourceLink](https://github.com/dotnet/sourcelink)
- [NuGet Best Practices](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)

### Dependency Injection

- [Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [DI Guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [Service Lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)

## Tools

### Development Tools

- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [JetBrains Rider](https://www.jetbrains.com/rider/)
- [Visual Studio Code + C# Dev Kit](https://code.visualstudio.com/)

### CLI Tools

```bash
# Install global tools
dotnet tool install --global GitVersion.Tool
dotnet tool install --global dotnet-reportgenerator-globaltool
dotnet tool install --global dotnet-format
```

### Analysis Tools

- [ReSharper](https://www.jetbrains.com/resharper/)
- [SonarLint](https://www.sonarsource.com/products/sonarlint/)
- [.NET Analyzers](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)

## Internal Documents

- [Phase 0: Discovery](../phase-0-discovery/phase-0-discovery.md)
- [ADR-0004: Release Governance](../adrs/ADR-0004-Release-Governance.md)
- [ADR-0005: DI Container Strategy](../adrs/ADR-0005-DI-Container-Strategy.md)
- [CQRS Framework Plan](../cqrs-framework-plan.md)

## Related Phases

- **Next:** [Phase 2: Domain Model](../phase-2-domain/phase-2-domain.md)
- [Phase 3: Application Layer](../phase-3-application/phase-3-application.md)
- [Phase 4: Web Layer](../phase-4-web/phase-4-web.md)
- [Phase 5: Infrastructure](../phase-5-infrastructure/phase-5-infrastructure.md)
- [Phase 6: Release](../phase-6-release/phase-6-release.md)

## Community Resources

- [.NET Foundation](https://dotnetfoundation.org/)
- [.NET Blog](https://devblogs.microsoft.com/dotnet/)
- [Stack Overflow - .NET](https://stackoverflow.com/questions/tagged/.net)
- [r/dotnet](https://www.reddit.com/r/dotnet/)

## Books & Articles

- **Clean Architecture** by Robert C. Martin
- **Domain-Driven Design** by Eric Evans
- **Building Microservices** by Sam Newman
- [Microsoft Architecture Guides](https://learn.microsoft.com/en-us/azure/architecture/)
