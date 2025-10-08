# Guide 2: Message Queuing

**Phase**: 5 - Infrastructure & Integration  
**Component**: Message Queuing  
**Dependencies**: Phase 2 (Domain Events)

---

## Overview

Message queuing enables reliable async communication between services using RabbitMQ (with MassTransit) or Azure Service Bus.

---

## RabbitMQ with MassTransit

### Installation

```bash
dotnet add package MassTransit --version 8.1.1
dotnet add package MassTransit.RabbitMQ --version 8.1.1
```

### Configuration

```csharp
// Program.cs
using MassTransit;

builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<OrderCreatedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"), h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configure endpoints
        cfg.ConfigureEndpoints(context);

        // Retry policy
        cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2)));

        // Dead letter queue
        cfg.ReceiveEndpoint("error-queue", e =>
        {
            e.ConfigureConsumeTopology = false;
        });
    });
});
```

### Message Contracts

```csharp
namespace Idevs.Contracts.Events;

public sealed record OrderCreatedEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public sealed record PaymentProcessedEvent
{
    public required Guid PaymentId { get; init; }
    public required Guid OrderId { get; init; }
    public required string Status { get; init; }
}
```

### Publishing Messages

```csharp
public sealed class OrderApplicationService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderApplicationService(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public async Task<Result> CreateOrderAsync(CreateOrderCommand command, CancellationToken ct)
    {
        // Create order...

        // Publish event
        await _publishEndpoint.Publish(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return Result.Success();
    }
}
```

### Consuming Messages

```csharp
public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IMediator _mediator;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        _logger.LogInformation("Processing OrderCreated event for Order {OrderId}", context.Message.OrderId);

        try
        {
            var command = new SendOrderConfirmationCommand(context.Message.OrderId, context.Message.CustomerId);
            await _mediator.Send(command, context.CancellationToken);

            _logger.LogInformation("Successfully processed OrderCreated event");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process OrderCreated event");
            throw; // Will retry based on retry policy
        }
    }
}
```

---

## Azure Service Bus

### Installation

```bash
dotnet add package MassTransit.Azure.ServiceBus.Core --version 8.1.1
```

### Configuration

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("ServiceBus"));

        cfg.ConfigureEndpoints(context);

        cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2)));
    });
});
```

---

## Testing

```csharp
public sealed class MessageQueueTests
{
    [Fact]
    public async Task PublishEndpoint_PublishesMessage()
    {
        // Arrange
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var service = new OrderApplicationService(publishEndpoint);
        var command = new CreateOrderCommand(Guid.NewGuid(), 100m);

        // Act
        await service.CreateOrderAsync(command, CancellationToken.None);

        // Assert
        await publishEndpoint.Received(1).Publish(Arg.Any<OrderCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consumer_ProcessesMessage()
    {
        // Arrange
        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var mediator = Substitute.For<IMediator>();
        var consumer = new OrderCreatedConsumer(logger, mediator);
        var message = new OrderCreatedEvent { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), TotalAmount = 100m, CreatedAt = DateTime.UtcNow };
        var context = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context.Message.Returns(message);

        // Act
        await consumer.Consume(context);

        // Assert
        await mediator.Received(1).Send(Arg.Any<SendOrderConfirmationCommand>(), Arg.Any<CancellationToken>());
    }
}
```

---

## Best Practices

- Use message versioning for backward compatibility
- Idempotent consumers (handle duplicate messages)
- Configure appropriate retry policies
- Monitor dead-letter queues
- Use correlation IDs for tracing

---

**Last Updated**: 2025-01-08  
**License**: MIT
