using System.Collections.Concurrent;
using System.Text.Json;
using Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Common.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, bool> _cacheKeys;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _cacheKeys = new ConcurrentDictionary<string, bool>();
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult((T?)value);
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult(default(T?));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return Task.FromResult(default(T?));
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(30); // Default sliding expiration
            }

            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _cacheKeys.TryRemove(key, out _);
                _logger.LogDebug("Cache entry evicted. Key: {Key}, Reason: {Reason}", key, reason);
            });

            _memoryCache.Set(key, value, options);
            _cacheKeys.TryAdd(key, true);
            
            _logger.LogDebug("Cache set for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            _logger.LogDebug("Cache removed for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = _cacheKeys.ContainsKey(key);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return Task.FromResult(false);
        }
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var keysToRemove = _cacheKeys.Keys
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }

            _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
    }

    public Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var matchingKeys = _cacheKeys.Keys
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            return Task.FromResult<IEnumerable<string>>(matchingKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys by pattern: {Pattern}", pattern);
            return Task.FromResult(Enumerable.Empty<string>());
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key, 
        Func<Task<T>> getItem, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await getItem();
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            await SetAsync(item.Key, item.Value, expiration, cancellationToken);
        }
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, T?>();
        
        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            result[key] = value;
        }

        return result;
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            await RemoveAsync(key, cancellationToken);
        }
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // Memory cache doesn't need refresh, but we can touch the key to reset sliding expiration
        if (_memoryCache.TryGetValue(key, out var value))
        {
            _memoryCache.Set(key, value, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30)
            });
        }
        
        return Task.CompletedTask;
    }

    // Hash operations (no-op for memory cache)
    public Task<T?> HashGetAsync<T>(string key, string field, CancellationToken cancellationToken = default)
    {
        var hashKey = $"{key}:{field}";
        return GetAsync<T>(hashKey, cancellationToken);
    }

    public Task HashSetAsync<T>(string key, string field, T value, CancellationToken cancellationToken = default)
    {
        var hashKey = $"{key}:{field}";
        return SetAsync(hashKey, value, cancellationToken: cancellationToken);
    }

    public Task<bool> HashExistsAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        var hashKey = $"{key}:{field}";
        return ExistsAsync(hashKey, cancellationToken);
    }

    public Task HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        var hashKey = $"{key}:{field}";
        return RemoveAsync(hashKey, cancellationToken);
    }

    private static bool IsPatternMatch(string key, string pattern)
    {
        // Simple wildcard pattern matching
        if (pattern.Contains('*'))
        {
            var regexPattern = pattern.Replace("*", ".*");
            return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
        }
        
        return key.Contains(pattern);
    }
}