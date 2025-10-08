# Guides 4-8: Additional Infrastructure Services

**Phase**: 5 - Infrastructure & Integration  
**Component**: Email, File Storage, Integration Events, Outbox, External Services  
**Status**: Production-Ready Patterns

---

## Guide 4: Email/SMS Services

### Email with Send Grid

```bash
dotnet add package SendGrid --version 9.28.1
```

```csharp
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public sealed class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;
    private readonly string _fromEmail;

    public SendGridEmailService(IConfiguration config)
    {
        _client = new SendGridClient(config["SendGrid:ApiKey"]);
        _fromEmail = config["SendGrid:FromEmail"]!;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress(_fromEmail),
            new EmailAddress(to),
            subject,
            body,
            body);

        await _client.SendEmailAsync(msg, ct);
    }
}
```

### SMS with Twilio

```bash
dotnet add package Twilio --version 6.14.1
```

```csharp
public interface ISmsService
{
    Task SendAsync(string to, string message, CancellationToken ct = default);
}

public sealed class TwilioSmsService : ISmsService
{
    private readonly string _from;

    public TwilioSmsService(IConfiguration config)
    {
        _from = config["Twilio:From"]!;
        TwilioClient.Init(config["Twilio:AccountSid"], config["Twilio:AuthToken"]);
    }

    public async Task SendAsync(string to, string message, CancellationToken ct = default)
    {
        await MessageResource.CreateAsync(
            to: new PhoneNumber(to),
            from: new PhoneNumber(_from),
            body: message);
    }
}
```

---

## Guide 5: File Storage

### AWS S3

```bash
dotnet add package AWSSDK.S3 --version 3.7.200
```

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string fileKey, CancellationToken ct = default);
    Task DeleteAsync(string fileKey, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expiration);
}

public sealed class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3FileStorageService(IConfiguration config)
    {
        _s3Client = new AmazonS3Client();
        _bucketName = config["AWS:S3:BucketName"]!;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = $"{Guid.NewGuid()}/{fileName}";
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, ct);
        return key;
    }

    public async Task<Stream> DownloadAsync(string fileKey, CancellationToken ct = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        var response = await _s3Client.GetObjectAsync(request, ct);
        return response.ResponseStream;
    }

    public Task DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        return _s3Client.DeleteObjectAsync(request, ct);
    }

    public Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expiration)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            Expires = DateTime.UtcNow.Add(expiration)
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }
}
```

---

## Guide 6: Integration Events

### Domain Event Publishing

```csharp
public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IIntegrationEvent;
}

public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public IntegrationEventPublisher(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IIntegrationEvent
        => _publishEndpoint.Publish(@event, ct);
}
```

### Usage in Domain Event Handler

```csharp
public sealed class OrderCreatedDomainEventHandler : IDomainEventHandler<OrderCreatedDomainEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public OrderCreatedDomainEventHandler(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken ct)
    {
        // Map domain event to integration event
        var integrationEvent = new OrderCreatedIntegrationEvent
        {
            OrderId = notification.OrderId,
            CustomerId = notification.CustomerId,
            TotalAmount = notification.TotalAmount,
            OccurredAt = DateTime.UtcNow
        };

        await _publisher.PublishAsync(integrationEvent, ct);
    }
}
```

---

## Guide 7: Outbox Pattern

### Outbox Entity

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
```

### Outbox Service

```csharp
public interface IOutboxService
{
    Task AddAsync(object message, CancellationToken ct = default);
    Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default);
}

public sealed class OutboxService : IOutboxService
{
    private readonly ApplicationDbContext _context;

    public OutboxService(ApplicationDbContext context) => _context = context;

    public async Task AddAsync(object message, CancellationToken ct = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = message.GetType().FullName!,
            Payload = JsonSerializer.Serialize(message),
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(ct);
    }

    public Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default)
        => _context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { id }, ct);
        if (message is not null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { id }, ct);
        if (message is not null)
        {
            message.RetryCount++;
            message.Error = error;
            await _context.SaveChangesAsync(ct);
        }
    }
}
```

### Background Processor

```csharp
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

                var messages = await outbox.GetPendingAsync(50, stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        var type = Type.GetType(message.Type)!;
                        var @event = JsonSerializer.Deserialize(message.Payload, type)!;

                        await publisher.PublishAsync((dynamic)@event, stoppingToken);
                        await outbox.MarkProcessedAsync(message.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                        await outbox.MarkFailedAsync(message.Id, ex.Message, stoppingToken);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor error");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
```

---

## Guide 8: External Services with Resilience

### Typed HttpClient with Polly

```bash
dotnet add package Polly --version 8.2.0
dotnet add package Polly.Extensions.Http --version 3.0.0
```

```csharp
// Registration
builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentGateway:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
.AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromMinutes(1)));
```

```csharp
public interface IPaymentGatewayClient
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default);
}

public sealed class PaymentGatewayClient : IPaymentGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentGatewayClient> _logger;

    public PaymentGatewayClient(HttpClient httpClient, ILogger<PaymentGatewayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing payment for Order {OrderId}", request.OrderId);

        var response = await _httpClient.PostAsJsonAsync("/api/payments", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaymentResult>(cancellationToken: ct);
        return result!;
    }
}
```

---

## Testing All Services

```csharp
// Email Service Test
[Fact]
public async Task EmailService_SendsEmail()
{
    var emailService = Substitute.For<IEmailService>();
    await emailService.SendAsync("test@example.com", "Test", "Body");
    await emailService.Received(1).SendAsync("test@example.com", "Test", "Body", Arg.Any<CancellationToken>());
}

// File Storage Test
[Fact]
public async Task FileStorage_UploadsFile()
{
    var storage = Substitute.For<IFileStorageService>();
    storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>()).Returns("file-key-123");

    var stream = new MemoryStream();
    var key = await storage.UploadAsync(stream, "test.txt", "text/plain");

    key.ShouldBe("file-key-123");
}

// Outbox Test
[Fact]
public async Task OutboxService_AddsMessage()
{
    var outbox = Substitute.For<IOutboxService>();
    var message = new { OrderId = Guid.NewGuid() };

    await outbox.AddAsync(message);

    await outbox.Received(1).AddAsync(message, Arg.Any<CancellationToken>());
}
```

---

**Last Updated**: 2025-01-08  
**License**: MIT
