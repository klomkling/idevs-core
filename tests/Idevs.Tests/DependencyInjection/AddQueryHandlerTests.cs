using FluentValidation;
using Idevs.Abstractions;
using Idevs.Common;
using Idevs.Configuration;
using Idevs.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Idevs.Tests.DependencyInjection;

public sealed class AddQueryHandlerTests
{
    [Fact]
    public void AddQueryHandler_RegistersHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs();

        // Act
        services.AddQueryHandler<TestQuery, string, TestQueryHandler>();
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IQueryHandler<TestQuery, string>>();
        handler.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddQueryHandler_ExecutesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs();
        services.AddQueryHandler<TestQuery, string, TestQueryHandler>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, string>>();

        // Act
        var result = await handler.HandleAsync(new TestQuery("test"), default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("query-test-result");
    }

    [Fact]
    public async Task AddQueryHandler_AppliesLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdevs(options => options.Decorators.EnableLogging = true);
        services.AddQueryHandler<TestQuery, string, TestQueryHandler>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, string>>();

        // Act
        var result = await handler.HandleAsync(new TestQuery("test"), default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("query-test-result");
    }

    [Fact(Skip = "Validation decorator for queries not yet implemented")]
    public async Task AddQueryHandler_AppliesValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs(options => options.Decorators.EnableValidation = true);
        services.AddQueryHandler<TestQuery, string, TestQueryHandler>();
        services.AddSingleton<IValidator<TestQuery>, TestQueryValidator>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, string>>();

        // Act
        var result = await handler.HandleAsync(new TestQuery(""), default);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("Filter");
    }

    [Fact]
    public async Task AddQueryHandler_AppliesMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs(options => options.Decorators.EnableMetrics = true);
        services.AddQueryHandler<TestQuery, string, TestQueryHandler>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, string>>();

        // Act
        var result = await handler.HandleAsync(new TestQuery("test"), default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("query-test-result");
    }

    [Fact]
    public async Task AddQueryHandler_PerHandlerOptions_DisablesValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs(options => options.Decorators.EnableValidation = true);
        services.AddQueryHandler<TestQuery, string, TestQueryHandler>(
            options => options.EnableValidation = false);
        services.AddSingleton<IValidator<TestQuery>, TestQueryValidator>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, string>>();

        // Act - empty filter would fail validation if enabled
        var result = await handler.HandleAsync(new TestQuery(""), default);

        // Assert - should succeed because validation is disabled
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AddQueryHandler_AllDecoratorsEnabled_WorksTogether()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdevs(options =>
        {
            options.Decorators.EnableLogging = true;
            options.Decorators.EnableValidation = true;
            options.Decorators.EnableMetrics = true;
        });
        services.AddQueryHandler<TestQuery, string, TestQueryHandler>();
        services.AddSingleton<IValidator<TestQuery>, TestQueryValidator>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, string>>();

        // Act
        var result = await handler.HandleAsync(new TestQuery("test"), default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("query-test-result");
    }
}

public sealed record TestQuery(string Filter) : IQuery<string>;

public sealed class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<Result<string>> HandleAsync(TestQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success($"query-{query.Filter}-result"));
    }
}

public sealed class TestQueryValidator : AbstractValidator<TestQuery>
{
    public TestQueryValidator()
    {
        RuleFor(x => x.Filter).NotEmpty();
    }
}
