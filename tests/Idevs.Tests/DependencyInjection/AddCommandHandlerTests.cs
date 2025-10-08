using FluentValidation;
using Idevs.Abstractions;
using Idevs.DependencyInjection;
using Idevs.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Idevs.Tests.DependencyInjection;

public class AddCommandHandlerTests
{
    [Fact]
    public async Task AddCommandHandler_RegistersAndExecutesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdevs();
        services.AddCommandHandler<TestCommand, TestCommandHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand>>();

        // Act
        var result = await handler.HandleAsync(new TestCommand("Test", 42));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AddCommandHandler_WithValidator_ValidatesCommand()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdevs();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        services.AddCommandHandler<TestCommand, TestCommandHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand>>();

        // Act - Invalid command (Value = 0)
        var result = await handler.HandleAsync(new TestCommand("Test", 0));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("VALIDATION_ERROR");
        result.Error.Details.ShouldNotBeNull();
        result.Error.Details!.ContainsKey("Value").ShouldBeTrue();
    }

    [Fact]
    public async Task AddCommandHandler_WithoutValidator_SkipsValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdevs();
        services.AddCommandHandler<TestCommand, TestCommandHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand>>();

        // Act - Invalid command but no validator
        var result = await handler.HandleAsync(new TestCommand("", 0));

        // Assert - Should succeed since no validator is registered
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AddCommandHandler_WithDecoratorConfig_AppliesToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdevs();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        
        services.AddCommandHandler<TestCommand, TestCommandHandler>(options =>
        {
            options.EnableLogging = false;
            options.EnableValidation = false;
            options.EnableMetrics = false;
        });

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand>>();

        // Act - Invalid command but validation disabled
        var result = await handler.HandleAsync(new TestCommand("", 0));

        // Assert - Should succeed since decorators are disabled
        result.IsSuccess.ShouldBeTrue();
    }
}
