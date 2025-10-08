# Guide 3: Caching

**Phase**: 5 - Infrastructure & Integration  
**Component**: Distributed Caching  
**Dependencies**: None

---

## Overview

Distributed caching with Redis reduces database load and improves performance. This guide covers cache-aside pattern, invalidation, and multi-tenant keys.

---

## Installation

```bash
dotnet add package StackExchange.Redis --version 2.7.4
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 8.0.0
```

---

## Configuration

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "Idevs:";
});
```

---

## Service Abstraction

```csharp
namespace Idevs.Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null) return default;

        return JsonSerializer.Deserialize<T>(bytes);
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

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Requires direct Redis connection for pattern-based removal
        _logger.LogWarning("Pattern-based removal requires direct Redis access");
        return Task.CompletedTask;
    }
}
```

---

## Cache-Aside Pattern

```csharp
public sealed class ProductQueryService
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _repository;
    private readonly ITenantContext _tenant;

    public ProductQueryService(ICacheService cache, IProductRepository repository, ITenantContext tenant)
    {
        _cache = cache;
        _repository = repository;
        _tenant = tenant;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var cacheKey = $"tenant:{_tenant.TenantId}:product:{id}";

        // Try cache first
        var cached = await _cache.GetAsync<ProductDto>(cacheKey, ct);
        if (cached is not null) return cached;

        // Cache miss: fetch from database
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null) return null;

        var dto = MapToDto(product);

        // Store in cache
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(15), ct);

        return dto;
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price
    };
}
```

---

## Cache Invalidation

```csharp
public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenant;

    public UpdateProductCommandHandler(IProductRepository repository, ICacheService cache, ITenantContext tenant)
    {
        _repository = repository;
        _cache = cache;
        _tenant = tenant;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(request.Id, ct);
        if (product is null) return Result.Failure(ProductErrors.NotFound);

        product.UpdateDetails(request.Name, request.Price);
        await _repository.UpdateAsync(product, ct);

        // Invalidate cache
        var cacheKey = $"tenant:{_tenant.TenantId}:product:{request.Id}";
        await _cache.RemoveAsync(cacheKey, ct);

        return Result.Success();
    }
}
```

---

## Testing

```csharp
public sealed class CacheServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsCachedValue()
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

    [Fact]
    public async Task SetAsync_StoresValue()
    {
        // Arrange
        var cache = Substitute.For<ICacheService>();
        var key = "test-key";
        var value = new { Name = "Test" };

        // Act
        await cache.SetAsync(key, value, TimeSpan.FromMinutes(10));

        // Assert
        await cache.Received(1).SetAsync(key, value, TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }
}
```

---

## Best Practices

- Use tenant-scoped keys: `tenant:{tenantId}:entity:{id}`
- Set appropriate TTL (time-to-live) based on data volatility
- Invalidate cache on updates/deletes
- Handle cache failures gracefully (fallback to database)
- Monitor cache hit/miss ratio

---

**Last Updated**: 2025-01-08  
**License**: MIT
