# Guide 1: Background Jobs

**Phase**: 5 - Infrastructure & Integration  
**Component**: Background Jobs  
**Dependencies**: Phase 3 (Application Layer)

---

## Overview

Background jobs enable async task execution outside the request/response cycle. This guide covers Hangfire for easy setup and Quartz.NET for advanced scheduling.

---

## Hangfire Implementation

### Installation

```bash
dotnet add package Hangfire --version 1.8.6
dotnet add package Hangfire.SqlServer --version 1.8.6
dotnet add package Hangfire.PostgreSql --version 1.20.3
```

### Configuration

```csharp
// Program.cs
using Hangfire;
using Hangfire.PostgreSql;

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("Hangfire"), new PostgreSqlStorageOptions
    {
        QueuePollInterval = TimeSpan.FromSeconds(15),
        InvisibilityTimeout = TimeSpan.FromMinutes(30)
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "critical", "default", "low" };
});

var app = builder.Build();

// Dashboard with authorization
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

### Service Abstraction

```csharp
namespace Idevs.Infrastructure.BackgroundJobs;

public interface IBackgroundJobService
{
    string Enqueue(Expression<Action> methodCall);
    string Enqueue<T>(Expression<Action<T>> methodCall);
    string Schedule(Expression<Action> methodCall, TimeSpan delay);
    void RecurringJob(string jobId, Expression<Action> methodCall, string cronExpression, TimeZoneInfo? timeZone = null);
    void RemoveRecurringJob(string jobId);
}

public sealed class HangfireBackgroundJobService : IBackgroundJobService
{
    public string Enqueue(Expression<Action> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Action<T>> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
        => BackgroundJob.Schedule(methodCall, delay);

    public void RecurringJob(string jobId, Expression<Action> methodCall, string cronExpression, TimeZoneInfo? timeZone = null)
        => Hangfire.RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression, timeZone ?? TimeZoneInfo.Utc);

    public void RemoveRecurringJob(string jobId)
        => Hangfire.RecurringJob.RemoveIfExists(jobId);
}
```

### Usage Examples

```csharp
public sealed class OrderService
{
    private readonly IBackgroundJobService _jobs;

    public OrderService(IBackgroundJobService jobs) => _jobs = jobs;

    public async Task<Result> PlaceOrderAsync(PlaceOrderCommand command)
    {
        // Place order...

        // Send confirmation email (fire-and-forget)
        _jobs.Enqueue(() => SendOrderConfirmationEmail(command.OrderId));

        // Schedule reminder after 24 hours
        _jobs.Schedule(() => SendOrderReminderEmail(command.OrderId), TimeSpan.FromHours(24));

        return Result.Success();
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendOrderConfirmationEmail(Guid orderId)
    {
        // Implementation
    }

    public async Task SendOrderReminderEmail(Guid orderId)
    {
        // Implementation
    }
}
```

### Recurring Jobs

```csharp
public sealed class RecurringJobsSetup
{
    public static void ConfigureRecurringJobs(IBackgroundJobService jobs)
    {
        // Every day at 2 AM UTC
        jobs.RecurringJob("daily-report", () => GenerateDailyReport(), Cron.Daily(2));

        // Every hour
        jobs.RecurringJob("sync-inventory", () => SyncInventory(), Cron.Hourly());

        // Every 15 minutes
        jobs.RecurringJob("process-webhooks", () => ProcessWebhooks(), "*/15 * * * *");
    }

    [Queue("critical")]
    public static async Task GenerateDailyReport() { }

    [Queue("default")]
    public static async Task SyncInventory() { }

    [Queue("low")]
    public static async Task ProcessWebhooks() { }
}
```

---

## Quartz.NET Implementation

### Installation

```bash
dotnet add package Quartz --version 3.8.0
dotnet add package Quartz.Extensions.Hosting --version 3.8.0
dotnet add package Quartz.Serialization.Json --version 3.8.0
```

### Configuration

```csharp
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 10);

    // Job 1: Daily Report
    var dailyReportJobKey = new JobKey("daily-report-job");
    q.AddJob<DailyReportJob>(opts => opts.WithIdentity(dailyReportJobKey));
    q.AddTrigger(opts => opts
        .ForJob(dailyReportJobKey)
        .WithIdentity("daily-report-trigger")
        .WithCronSchedule("0 0 2 * * ?") // 2 AM daily
        .StartNow());

    // Job 2: Process Pending Orders
    var processPendingJobKey = new JobKey("process-pending-orders");
    q.AddJob<ProcessPendingOrdersJob>(opts => opts.WithIdentity(processPendingJobKey));
    q.AddTrigger(opts => opts
        .ForJob(processPendingJobKey)
        .WithIdentity("process-pending-trigger")
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever())
        .StartNow());
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
```

### Job Implementation

```csharp
using Quartz;

[DisallowConcurrentExecution]
public sealed class DailyReportJob : IJob
{
    private readonly ILogger<DailyReportJob> _logger;
    private readonly IMediator _mediator;

    public DailyReportJob(ILogger<DailyReportJob> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting daily report generation");

        try
        {
            var command = new GenerateDailyReportCommand(DateTime.UtcNow.Date.AddDays(-1));
            await _mediator.Send(command);

            _logger.LogInformation("Daily report generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate daily report");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
```

---

## Testing

```csharp
public sealed class BackgroundJobServiceTests
{
    [Fact]
    public void Enqueue_ReturnsJobId()
    {
        // Arrange
        var service = Substitute.For<IBackgroundJobService>();
        service.Enqueue(Arg.Any<Expression<Action>>()).Returns("job-123");

        // Act
        var jobId = service.Enqueue(() => Console.WriteLine("Test"));

        // Assert
        jobId.ShouldBe("job-123");
        service.Received(1).Enqueue(Arg.Any<Expression<Action>>());
    }

    [Fact]
    public void RecurringJob_ConfiguresSuccessfully()
    {
        // Arrange
        var service = Substitute.For<IBackgroundJobService>();

        // Act
        service.RecurringJob("test-job", () => Console.WriteLine("Test"), Cron.Daily());

        // Assert
        service.Received(1).RecurringJob("test-job", Arg.Any<Expression<Action>>(), Cron.Daily(), null);
    }
}
```

---

## Best Practices

- Use queues to prioritize critical jobs
- Set appropriate retry attempts with `[AutomaticRetry]`
- Use `[DisallowConcurrentExecution]` to prevent overlapping jobs
- Log job start/completion for observability
- Avoid long-running jobs (>5 minutes); break into smaller tasks

---

**Last Updated**: 2025-01-08  
**License**: MIT
