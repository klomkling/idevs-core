using Idevs.Configuration;
using Idevs.DependencyInjection;
using Idevs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Idevs.Tests.DependencyInjection;

public class AddIdevsTests
{
    [Fact]
    public void AddIdevs_RegistersCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdevs();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tenantContext = serviceProvider.GetService<ITenantContext<string>>();
        tenantContext.ShouldNotBeNull();
        tenantContext.TenantId.ShouldBe("default");
        tenantContext.IsMultiTenant.ShouldBeFalse();

        var currentUser = serviceProvider.GetService<ICurrentUser<string>>();
        currentUser.ShouldNotBeNull();
        currentUser.IsAuthenticated.ShouldBeFalse();

        var metrics = serviceProvider.GetService<IMetrics>();
        metrics.ShouldNotBeNull();
    }

    [Fact]
    public void AddIdevs_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdevs(options =>
        {
            options.ServiceName = "TestService";
            options.ServiceVersion = "1.0.0";
            options.Decorators.EnableLogging = false;
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<IdevsOptions>>();

        // Assert
        options.Value.ServiceName.ShouldBe("TestService");
        options.Value.ServiceVersion.ShouldBe("1.0.0");
        options.Value.Decorators.EnableLogging.ShouldBeFalse();
        options.Value.Decorators.EnableValidation.ShouldBeTrue();
        options.Value.Decorators.EnableMetrics.ShouldBeTrue();
    }

    [Fact]
    public void AddIdevs_AllowsServiceOverrides()
    {
        // Arrange
        var services = new ServiceCollection();
        var customMetrics = new CustomMetrics();
        services.AddSingleton<IMetrics>(customMetrics);

        // Act
        services.AddIdevs();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var metrics = serviceProvider.GetRequiredService<IMetrics>();
        metrics.ShouldBeSameAs(customMetrics);
    }

    private sealed class CustomMetrics : IMetrics
    {
        public void IncrementCounter(string name, long increment = 1, params KeyValuePair<string, string>[] tags) { }
        public IDisposable StartTimer(string name, params KeyValuePair<string, string>[] tags) => null!;
        public void ObserveHistogram(string name, double value, params KeyValuePair<string, string>[] tags) { }
        public void RecordGauge(string name, double value, params KeyValuePair<string, string>[] tags) { }
    }
}
