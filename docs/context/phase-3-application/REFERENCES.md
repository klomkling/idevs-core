# Phase 3: Application Layer — References

**Last Updated**: 2025-01-08  
**Status**: ✅ Validated

---

## Official Documentation

### MediatR
- **GitHub Repository**: https://github.com/jbogard/MediatR
- **NuGet Package**: https://www.nuget.org/packages/MediatR
- **Wiki**: https://github.com/jbogard/MediatR/wiki
- **Version**: 12.2.0
- **License**: Apache-2.0

### FluentValidation
- **Official Documentation**: https://docs.fluentvalidation.net
- **GitHub Repository**: https://github.com/FluentValidation/FluentValidation
- **NuGet Package**: https://www.nuget.org/packages/FluentValidation
- **Version**: 11.9.0
- **License**: Apache-2.0

### Microsoft.Extensions.Logging
- **Official Documentation**: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging
- **GitHub Repository**: https://github.com/dotnet/runtime
- **NuGet Package**: https://www.nuget.org/packages/Microsoft.Extensions.Logging
- **License**: MIT

### Microsoft.Extensions.DependencyInjection
- **Official Documentation**: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
- **GitHub Repository**: https://github.com/dotnet/runtime
- **NuGet Package**: https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection
- **License**: MIT

---

## Architecture Patterns

### CQRS (Command Query Responsibility Segregation)
- **Martin Fowler's CQRS**: https://martinfowler.com/bliki/CQRS.html
- **Microsoft CQRS Pattern**: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- **CQRS Journey**: https://learn.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10)

### Mediator Pattern
- **Refactoring Guru - Mediator**: https://refactoring.guru/design-patterns/mediator
- **Microsoft - Mediator Pattern**: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api

### Pipeline Behavior Pattern
- **Decorator Pattern**: https://refactoring.guru/design-patterns/decorator
- **Chain of Responsibility**: https://refactoring.guru/design-patterns/chain-of-responsibility

### Result Pattern
- **Railway Oriented Programming**: https://fsharpforfunandprofit.com/posts/recipe-part2/
- **Functional Error Handling**: https://enterprisecraftsmanship.com/posts/functional-c-handling-failures-input-errors/

---

## Clean Architecture

### Books
- **Clean Architecture** by Robert C. Martin (Uncle Bob)
  - Publisher: Prentice Hall
  - ISBN: 978-0134494166

### Articles
- **The Clean Architecture**: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- **Clean Architecture with .NET**: https://jasontaylor.dev/clean-architecture-getting-started/
- **Microsoft Clean Architecture**: https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures

---

## Domain-Driven Design

### Books
- **Domain-Driven Design** by Eric Evans
  - Publisher: Addison-Wesley
  - ISBN: 978-0321125217

- **Implementing Domain-Driven Design** by Vaughn Vernon
  - Publisher: Addison-Wesley
  - ISBN: 978-0321834577

### Articles
- **DDD Reference**: https://www.domainlanguage.com/ddd/reference/
- **Aggregate Pattern**: https://martinfowler.com/bliki/DDD_Aggregate.html
- **Value Objects**: https://martinfowler.com/bliki/ValueObject.html

---

## Validation

### FluentValidation Resources
- **Getting Started**: https://docs.fluentvalidation.net/en/latest/start.html
- **Built-in Validators**: https://docs.fluentvalidation.net/en/latest/built-in-validators.html
- **Custom Validators**: https://docs.fluentvalidation.net/en/latest/custom-validators.html
- **Async Validation**: https://docs.fluentvalidation.net/en/latest/async.html
- **ASP.NET Core Integration**: https://docs.fluentvalidation.net/en/latest/aspnet.html

---

## Authorization & Security

### ASP.NET Core Security
- **Authentication & Authorization**: https://learn.microsoft.com/en-us/aspnet/core/security/
- **Policy-based Authorization**: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies
- **Claims-based Authorization**: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims

### Security Best Practices
- **OWASP Top 10**: https://owasp.org/www-project-top-ten/
- **OWASP ASVS**: https://owasp.org/www-project-application-security-verification-standard/
- **Security in .NET**: https://learn.microsoft.com/en-us/dotnet/standard/security/

---

## Logging & Observability

### Structured Logging
- **Serilog**: https://serilog.net/
- **NLog**: https://nlog-project.org/
- **Serilog Best Practices**: https://benfoster.io/blog/serilog-best-practices/

### Application Insights
- **Overview**: https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview
- **ASP.NET Core Integration**: https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core

### OpenTelemetry
- **Official Site**: https://opentelemetry.io/
- **.NET Documentation**: https://opentelemetry.io/docs/instrumentation/net/

---

## Testing

### Unit Testing
- **xUnit Documentation**: https://xunit.net/
- **Shouldly Assertions**: https://docs.shouldly.org/
- **NSubstitute Mocking**: https://nsubstitute.github.io/

### Integration Testing
- **ASP.NET Core Integration Tests**: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- **WebApplicationFactory**: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests#basic-tests-with-the-default-webapplicationfactory

### Test-Driven Development
- **TDD by Example** by Kent Beck
- **Growing Object-Oriented Software, Guided by Tests** by Steve Freeman & Nat Pryce

---

## Performance

### Benchmarking
- **BenchmarkDotNet**: https://benchmarkdotnet.org/
- **Performance Best Practices**: https://learn.microsoft.com/en-us/dotnet/framework/performance/performance-tips

### Caching
- **IDistributedCache**: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed
- **Redis**: https://redis.io/docs/
- **Memory Caching**: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory

---

## Community Resources

### Blogs
- **Jimmy Bogard** (MediatR creator): https://jimmybogard.com/
- **Vladimir Khorikov** (DDD & Clean Code): https://enterprisecraftsmanship.com/
- **Steve Smith** (Ardalis): https://ardalis.com/
- **Jason Taylor** (Clean Architecture): https://jasontaylor.dev/

### GitHub Repositories
- **Clean Architecture Template**: https://github.com/jasontaylordev/CleanArchitecture
- **eShopOnWeb**: https://github.com/dotnet-architecture/eShopOnWeb
- **Sample CQRS App**: https://github.com/kgrzybek/sample-dotnet-core-cqrs-api

---

## Tools

### Development
- **Rider / Visual Studio**: IDE with excellent .NET support
- **dotnet CLI**: Command-line interface for .NET
- **Resharper**: Code analysis and refactoring

### Code Quality
- **SonarQube**: Static code analysis
- **StyleCop Analyzers**: Code style enforcement
- **Roslynator**: Code analysis and refactoring

### Documentation
- **DocFX**: .NET API documentation generator
- **Swagger/OpenAPI**: API documentation

---

## Related Idevs Documentation

- **Phase 2 - Domain Layer**: `../phase-2-domain/README.md`
- **Phase 1 - Platform Setup**: `../phase-1-platform/README.md`
- **Phase 4 - Web/API Layer**: `../phase-4-web/README.md`
- **Phase 5 - Infrastructure**: `../phase-5-infrastructure/README.md`

---

## Contributing

Found a broken link or want to add a resource?
- Open an issue in the project repository
- Submit a pull request with the update
- Follow the contribution guidelines in CONTRIBUTING.md

---

**Maintained By**: Idevs Framework Team  
**License**: MIT
