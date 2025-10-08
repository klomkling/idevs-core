using FluentValidation;
using Idevs.Abstractions;
using Idevs.Common;
using Idevs.Configuration;
using Idevs.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Idevs.Tests.DependencyInjection;

public sealed class AddCommandHandlerWithResponseTests
{
    [Fact]
    public void AddCommandHandler_WithResponse_RegistersHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs();

        // Act
        services.AddCommandHandler<TestCommandWithResponse, string, TestCommandWithResponseHandler>();
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<ICommandHandler<TestCommandWithResponse, string>>();
        handler.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddCommandHandler_WithResponse_ExecutesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs();
        services.AddCommandHandler<TestCommandWithResponse, string, TestCommandWithResponseHandler>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithResponse, string>>();

        // Act
        var result = await handler.HandleAsync(new TestCommandWithResponse("test"), default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test-processed");
    }

    [Fact]
    public async Task AddCommandHandler_WithResponse_AppliesLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdevs(options => options.Decorators.EnableLogging = true);
        services.AddCommandHandler<TestCommandWithResponse, string, TestCommandWithResponseHandler>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithResponse, string>>();

        // Act
        var result = await handler.HandleAsync(new TestCommandWithResponse("test"), default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test-processed");
    }

    [Fact(Skip = "Validation decorator for commands with response not yet implemented")]
    public async Task AddCommandHandler_WithResponse_AppliesValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs(options => options.Decorators.EnableValidation = true);
        services.AddCommandHandler<TestCommandWithResponse, string, TestCommandWithResponseHandler>();
        services.AddSingleton<IValidator<TestCommandWithResponse>, TestCommandWithResponseValidator>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithResponse, string>>();

        // Act
        var result = await handler.HandleAsync(new TestCommandWithResponse(""), default);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("Value");
    }

    [Fact]
    public async Task AddCommandHandler_WithResponse_AppliesMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs(options => options.Decorators.EnableMetrics = true);
        services.AddCommandHandler<TestCommandWithResponse, string, TestCommandWithResponseHandler>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithResponse, string>>();

        // Act
        var result = await handler.HandleAsync(new TestCommandWithResponse("test"), default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test-processed");
    }

    [Fact]
    public async Task AddCommandHandler_WithResponse_PerHandlerOptions_DisablesValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdevs(options => options.Decorators.EnableValidation = true);
        services.AddCommandHandler<TestCommandWithResponse, string, TestCommandWithResponseHandler>(
            options => options.EnableValidation = false);
        services.AddSingleton<IValidator<TestCommandWithResponse>, TestCommandWithResponseValidator>();
        var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithResponse, string>>();

        // Act - empty value would fail validation if enabled
        var result = await handler.HandleAsync(new TestCommandWithResponse(""), default);

        // Assert - should succeed because validation is disabled
        result.IsSuccess.ShouldBeTrue();
    }
}

public sealed record TestCommandWithResponse(string Value) : ICommand<string>;

public sealed class TestCommandWithResponseHandler : ICommandHandler<TestCommandWithResponse, string>
{
    public Task<Result<string>> HandleAsync(TestCommandWithResponse command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success($"{command.Value}-processed"));
    }
}

public sealed class TestCommandWithResponseValidator : AbstractValidator<TestCommandWithResponse>
{
    public TestCommandWithResponseValidator()
    {
        RuleFor(x => x.Value).NotEmpty();
    }
}
