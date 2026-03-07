using System.Text.Json;
using AlfTekPro.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace AlfTekPro.Infrastructure.Common;

public class RedisCacheService : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _mux;

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer mux)
    {
        _cache = cache;
        _mux = mux;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null) return default;
        return JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? DefaultTtl
        };
        await _cache.SetAsync(key, bytes, options, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _cache.RemoveAsync(key, ct);

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Use SCAN to find all matching keys then DEL in batch
        var db = _mux.GetDatabase();
        var server = _mux.GetServers().FirstOrDefault();
        if (server is null) return;

        var pattern = $"{prefix}*";
        var keys = new List<RedisKey>();

        await foreach (var key in server.KeysAsync(pattern: pattern).WithCancellation(ct))
            keys.Add(key);

        if (keys.Count > 0)
            await db.KeyDeleteAsync(keys.ToArray());
    }
}
