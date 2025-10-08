# Phase 5: Infrastructure & Integration — Implementation Documentation

**Status**: 🚧 In Progress  
**Package**: `Idevs.Infrastructure`  
**Dependencies**: Phase 2 (Domain), Phase 3 (Application)  
**Target Framework**: .NET 8.0  
**Last Updated**: 2025-01-08

---

## 📚 Overview

The Infrastructure Layer provides concrete implementations for external concerns: background jobs, message queuing, caching, email/SMS, file storage, and integration events. This layer is a **plugin** to the application — the domain and application layers never depend on infrastructure implementations.

### Key Responsibilities

1. **Background Jobs**: Scheduled tasks, recurring jobs, fire-and-forget
2. **Message Queuing**: Pub/sub patterns, reliable messaging
3. **Caching**: Distributed caching with Redis
4. **Email/SMS**: Transactional and bulk messaging
5. **File Storage**: Cloud storage (S3, Azure Blob)
6. **Integration Events**: Event-driven architecture
7. **Outbox Pattern**: Reliable message delivery

---

## 📊 Quick Navigation

### Implementation Guides

| # | Guide | Focus | Est. Time |
|---|-------|-------|-----------|
| **1** | Background Jobs | Hangfire/Quartz integration | 3-4 hours |
| **2** | Message Queuing | RabbitMQ/Azure Service Bus | 4-5 hours |
| **3** | Caching | Redis distributed cache | 2-3 hours |
| **4** | Email/SMS Services | SMTP, SendGrid, Twilio | 2-3 hours |
| **5** | File Storage | S3, Azure Blob, abstractions | 2-3 hours |
| **6** | Integration Events | Domain event publishing | 3-4 hours |
| **7** | Outbox Pattern | Reliable messaging | 3-4 hours |
| **8** | External Services | HTTP clients, resilience | 2-3 hours |

### Supporting Documents

| Document | Purpose |
|----------|---------|
| [Existing Phase 5 Doc](./phase-5-infrastructure.md) | Original comprehensive overview |
| [Completion Summary](./completion-summary.md) | Existing summary |
| [References](./REFERENCES.md) | External resources |

**Total Implementation Time**: ~22-31 hours

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Phase 4: Web/API Layer                        │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│                 Phase 3: Application Layer                       │
│            (Commands, Queries, Handlers)                         │
└────────────────────────────┬────────────────────────────────────┘
                             │
                   ┌─────────▼─────────┐
                   │  Phase 2: Domain   │
                   │  (Entities, Events)│
                   └─────────┬──────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│              Phase 5: Infrastructure (Cross-Cutting)             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Background   │  │   Message    │  │   Caching    │          │
│  │    Jobs      │  │    Queue     │  │   (Redis)    │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │  Email/SMS   │  │ File Storage │  │  Integration │          │
│  │   Services   │  │   (S3/Blob)  │  │    Events    │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### Prerequisites

```bash
# Redis (for caching)
docker run -d -p 6379:6379 redis:alpine

# RabbitMQ (for messaging)
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:management

# SQL Server (for Hangfire)
docker run -d -p 1433:1433 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourPassword123!' mcr.microsoft.com/mssql/server:2022-latest
```

### Install Packages

```bash
# Background Jobs (choose one)
dotnet add package Hangfire --version 1.8.6
dotnet add package Hangfire.SqlServer --version 1.8.6
# OR
dotnet add package Quartz --version 3.8.0
dotnet add package Quartz.Extensions.Hosting --version 3.8.0

# Message Queuing (choose one)
dotnet add package RabbitMQ.Client --version 6.6.0
dotnet add package MassTransit.RabbitMQ --version 8.1.1
# OR
dotnet add package Azure.Messaging.ServiceBus --version 7.16.1

# Caching
dotnet add package StackExchange.Redis --version 2.7.4
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 8.0.0

# Email
dotnet add package MailKit --version 4.3.0
dotnet add package SendGrid --version 9.28.1

# File Storage
dotnet add package AWSSDK.S3 --version 3.7.200
dotnet add package Azure.Storage.Blobs --version 12.19.1

# Resilience
dotnet add package Polly --version 8.2.0
dotnet add package Polly.Extensions.Http --version 3.0.0
```

### Configuration Example

```csharp
// Program.cs
using Hangfire;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Background Jobs (Hangfire)
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("Hangfire")));

builder.Services.AddHangfireServer();

// Caching (Redis)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "IdevsCache:";
});

// Message Queue (RabbitMQ with MassTransit)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Hangfire Dashboard
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.Run();
```

---

## 🎯 Key Patterns

### 1. Background Jobs Pattern

```csharp
public interface IBackgroundJobService
{
    string Enqueue(Expression<Action> methodCall);
    string Schedule(Expression<Action> methodCall, TimeSpan delay);
    void RecurringJob(string jobId, Expression<Action> methodCall, string cronExpression);
}

public sealed class HangfireBackgroundJobService : IBackgroundJobService
{
    public string Enqueue(Expression<Action> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
        => BackgroundJob.Schedule(methodCall, delay);

    public void RecurringJob(string jobId, Expression<Action> methodCall, string cronExpression)
        => RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
}
```

### 2. Message Queue Pattern

```csharp
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}

public sealed class RabbitMqPublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RabbitMqPublisher(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
        => _publishEndpoint.Publish(message, ct);
}
```

### 3. Caching Pattern

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
        };
        await _cache.SetAsync(key, bytes, options, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _cache.RemoveAsync(key, ct);
}
```

### 4. Outbox Pattern

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

public interface IOutboxService
{
    Task AddAsync(object message, CancellationToken ct = default);
    Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default);
}
```

---

## 🧪 Testing Strategy

### Background Job Testing

```csharp
[Fact]
public void BackgroundJob_Enqueues_Successfully()
{
    // Arrange
    var jobService = Substitute.For<IBackgroundJobService>();
    jobService.Enqueue(Arg.Any<Expression<Action>>())
        .Returns("job-id-123");

    // Act
    var jobId = jobService.Enqueue(() => Console.WriteLine("Test"));

    // Assert
    jobId.ShouldBe("job-id-123");
}
```

### Cache Testing

```csharp
[Fact]
public async Task Cache_StoresAndRetrieves_Value()
{
    // Arrange
    var cache = Substitute.For<ICacheService>();
    var key = "test-key";
    var value = new { Name = "Test" };

    cache.GetAsync<object>(key).Returns(value);

    // Act
    var result = await cache.GetAsync<object>(key);

    // Assert
    result.ShouldBe(value);
}
```

---

## 📋 Implementation Checklist

### Background Jobs
- [ ] Install Hangfire or Quartz
- [ ] Configure job storage (SQL Server/PostgreSQL)
- [ ] Implement IBackgroundJobService
- [ ] Add recurring jobs
- [ ] Configure dashboard security
- [ ] Add job monitoring

### Message Queuing
- [ ] Install RabbitMQ/Service Bus client
- [ ] Configure connection strings
- [ ] Implement IMessagePublisher
- [ [ ] Implement message consumers
- [ ] Add error handling and retries
- [ ] Configure dead-letter queues

### Caching
- [ ] Install Redis
- [ ] Configure IDistributedCache
- [ ] Implement ICacheService
- [ ] Add cache key conventions
- [ ] Implement cache invalidation
- [ ] Add cache monitoring

### Email/SMS
- [ ] Choose provider (SendGrid/Twilio)
- [ ] Implement IEmailService
- [ ] Implement ISmsService
- [ ] Add template rendering
- [ ] Configure retry policies
- [ ] Add delivery tracking

### File Storage
- [ ] Choose provider (S3/Azure Blob)
- [ ] Implement IFileStorageService
- [ ] Add upload/download methods
- [ ] Configure access policies
- [ ] Add file metadata tracking

### Integration Events
- [ ] Implement IDomainEventPublisher
- [ ] Add event serialization
- [ ] Configure event routing
- [ ] Add event versioning
- [ ] Implement event replay

### Outbox Pattern
- [ ] Create OutboxMessage entity
- [ ] Implement IOutboxService
- [ ] Add background processor
- [ ] Configure retry logic
- [ ] Add monitoring

---

## 🎯 Success Criteria

Phase 5 is complete when:

✅ Background jobs execute reliably  
✅ Messages publish and consume correctly  
✅ Cache reduces database load  
✅ Emails/SMS send successfully  
✅ Files upload/download from cloud  
✅ Integration events trigger across boundaries  
✅ Outbox ensures message delivery  
✅ All services have resilience policies  
✅ Monitoring and alerting configured  
✅ Tests passing with >80% coverage  

---

## 📚 Learning Resources

### Background Jobs
- **Hangfire**: https://www.hangfire.io/
- **Quartz.NET**: https://www.quartz-scheduler.net/

### Message Queuing
- **MassTransit**: https://masstransit.io/
- **RabbitMQ**: https://www.rabbitmq.com/
- **Azure Service Bus**: https://learn.microsoft.com/en-us/azure/service-bus-messaging/

### Caching
- **Redis**: https://redis.io/docs/
- **Caching in .NET**: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed

### Email/SMS
- **MailKit**: https://github.com/jstedfast/MailKit
- **SendGrid**: https://sendgrid.com/docs/
- **Twilio**: https://www.twilio.com/docs/

### File Storage
- **AWS S3**: https://docs.aws.amazon.com/s3/
- **Azure Blob Storage**: https://learn.microsoft.com/en-us/azure/storage/blobs/

### Patterns
- **Outbox Pattern**: https://microservices.io/patterns/data/transactional-outbox.html
- **Saga Pattern**: https://microservices.io/patterns/data/saga.html

---

## 🔄 Next Steps

After completing Phase 5:
1. **Phase 6: Testing** — Comprehensive test strategies
2. **Phase 7: Deployment** — Docker, Kubernetes, CI/CD

---

**Last Updated**: 2025-01-08  
**Maintained By**: Idevs Framework Team  
**License**: MIT
