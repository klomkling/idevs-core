using FluentValidation;
using Idevs.Abstractions;
using Idevs.Common;
using Idevs.Configuration;
using Idevs.Decorators;
using Idevs.Services;
using Idevs.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Idevs.DependencyInjection;

/// <summary>
/// Extension methods for registering Idevs framework services.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers core Idevs framework services with default implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdevs(
        this IServiceCollection services,
        Action<IdevsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register options
        services.Configure<IdevsOptions>(options =>
        {
            configure?.Invoke(options);
        });

        // Register default services (only if not already registered)
        services.TryAddScoped<ITenantContext<string>>(sp =>
            new DefaultTenantContext<string>("default"));

        services.TryAddScoped<ICurrentUser<string>>(sp =>
            new DefaultCurrentUser<string>("anonymous"));

        services.TryAddSingleton<IMetrics, NoOpMetrics>();

        return services;
    }

    /// <summary>
    /// Registers a command handler with optional decorators.
    /// </summary>
    /// <typeparam name="TCommand">The type of command.</typeparam>
    /// <typeparam name="THandler">The type of handler implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDecorators">Optional decorator configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandHandler<TCommand, THandler>(
        this IServiceCollection services,
        Action<DecoratorOptions>? configureDecorators = null)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICommandHandler<TCommand>>(sp =>
        {
            // Get global options
            var globalOptions = sp.GetService<IOptions<IdevsOptions>>()?.Value ?? new IdevsOptions();
            var decoratorOptions = globalOptions.Decorators.Clone();

            // Apply per-handler overrides
            configureDecorators?.Invoke(decoratorOptions);

            // 1. Create core handler
            ICommandHandler<TCommand> handler = ActivatorUtilities.CreateInstance<THandler>(sp);

            // 2. Wrap with metrics (innermost)
            if (decoratorOptions.EnableMetrics)
            {
                var metrics = sp.GetRequiredService<IMetrics>();
                handler = new MetricsCommandHandlerDecorator<TCommand>(handler, metrics);
            }

            // 3. Wrap with validation
            if (decoratorOptions.EnableValidation)
            {
                var validator = sp.GetService<IValidator<TCommand>>();
                if (validator != null)
                {
                    handler = new ValidationCommandHandlerDecorator<TCommand>(handler, validator);
                }
            }

            // 4. Wrap with logging (outermost)
            if (decoratorOptions.EnableLogging)
            {
                var logger = sp.GetRequiredService<ILogger<LoggingCommandHandlerDecorator<TCommand>>>();
                handler = new LoggingCommandHandlerDecorator<TCommand>(handler, logger);
            }

            return handler;
        });

        return services;
    }

    /// <summary>
    /// Registers a command handler that returns a response with optional decorators.
    /// </summary>
    /// <typeparam name="TCommand">The type of command.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <typeparam name="THandler">The type of handler implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDecorators">Optional decorator configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandHandler<TCommand, TResponse, THandler>(
        this IServiceCollection services,
        Action<DecoratorOptions>? configureDecorators = null)
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICommandHandler<TCommand, TResponse>>(sp =>
        {
            // Get global options
            var globalOptions = sp.GetService<IOptions<IdevsOptions>>()?.Value ?? new IdevsOptions();
            var decoratorOptions = globalOptions.Decorators.Clone();

            // Apply per-handler overrides
            configureDecorators?.Invoke(decoratorOptions);

            // Create core handler (decorators for TResponse variant would be implemented similarly)
            ICommandHandler<TCommand, TResponse> handler = ActivatorUtilities.CreateInstance<THandler>(sp);

            // Note: For now, returning undecorated handler
            // TODO: Implement decorator variants for ICommandHandler<TCommand, TResponse>
            return handler;
        });

        return services;
    }

    /// <summary>
    /// Registers a query handler with optional decorators.
    /// </summary>
    /// <typeparam name="TQuery">The type of query.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <typeparam name="THandler">The type of handler implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDecorators">Optional decorator configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQueryHandler<TQuery, TResponse, THandler>(
        this IServiceCollection services,
        Action<DecoratorOptions>? configureDecorators = null)
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IQueryHandler<TQuery, TResponse>>(sp =>
        {
            // Get global options
            var globalOptions = sp.GetService<IOptions<IdevsOptions>>()?.Value ?? new IdevsOptions();
            var decoratorOptions = globalOptions.Decorators.Clone();

            // Apply per-handler overrides
            configureDecorators?.Invoke(decoratorOptions);

            // Create core handler (decorators for Query variant would be implemented similarly)
            IQueryHandler<TQuery, TResponse> handler = ActivatorUtilities.CreateInstance<THandler>(sp);

            // Note: For now, returning undecorated handler
            // TODO: Implement decorator variants for IQueryHandler<TQuery, TResponse>
            return handler;
        });

        return services;
    }
}
