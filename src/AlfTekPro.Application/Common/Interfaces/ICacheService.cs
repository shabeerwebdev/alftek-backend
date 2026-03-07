namespace AlfTekPro.Application.Common.Interfaces;

/// <summary>
/// Distributed cache abstraction. Default TTL is 5 minutes unless overridden.
/// </summary>
public interface ICacheService
{
    /// <summary>Returns the cached value or null on a cache miss.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>Stores a value. Pass null for <paramref name="expiry"/> to use the default TTL (5 min).</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>Removes a single key.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Removes all keys that start with <paramref name="prefix"/>.
    /// Useful for invalidating a collection (e.g., "tenants:{tenantId}:employees:*").
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
