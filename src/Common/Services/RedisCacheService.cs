using System.Text.Json;
using Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Common.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IDatabase? _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache distributedCache, 
        IConnectionMultiplexer? connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _database = connectionMultiplexer?.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(30); // Default sliding expiration
            }

            await _distributedCache.SetStringAsync(key, json, options, cancellationToken);
            _logger.LogDebug("Cache set for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_database != null)
            {
                return await _database.KeyExistsAsync(key);
            }

            // Fallback for IDistributedCache
            var value = await _distributedCache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_database != null)
            {
                var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern);
                
                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }

                _logger.LogDebug("Removed cache entries matching pattern: {Pattern}", pattern);
            }
            else
            {
                _logger.LogWarning("Pattern removal not supported without direct Redis connection");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
        }
    }

    public Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_database != null)
            {
                var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern);
                return Task.FromResult(keys.Select(k => k.ToString()).AsEnumerable());
            }
            
            _logger.LogWarning("Pattern search not supported without direct Redis connection");
            return Task.FromResult(Enumerable.Empty<string>());
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
        var tasks = items.Select(item => SetAsync(item.Key, item.Value, expiration, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var tasks = keys.Select(async key => new { Key = key, Value = await GetAsync<T>(key, cancellationToken) });
        var results = await Task.WhenAll(tasks);
        
        return results.ToDictionary(r => r.Key, r => r.Value);
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var tasks = keys.Select(key => RemoveAsync(key, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RefreshAsync(key, cancellationToken);
            _logger.LogDebug("Cache refreshed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache for key: {Key}", key);
        }
    }

    // Hash operations (Redis specific)
    public async Task<T?> HashGetAsync<T>(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_database != null)
            {
                var value = await _database.HashGetAsync(key, field);
                if (!value.HasValue)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }

            // Fallback to regular cache
            return await GetAsync<T>($"{key}:{field}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash value for key: {Key}, field: {Field}", key, field);
            return default;
        }
    }

    public async Task HashSetAsync<T>(string key, string field, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_database != null)
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                await _database.HashSetAsync(key, field, json);
                _logger.LogDebug("Hash set for key: {Key}, field: {Field}", key, field);
            }
            else
            {
                // Fallback to regular cache
                await SetAsync($"{key}:{field}", value, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash value for key: {Key}, field: {Field}", key, field);
        }
    }

    public async Task<bool> HashExistsAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_database != null)
            {
                return await _database.HashExistsAsync(key, field);
            }

            // Fallback to regular cache
            return await ExistsAsync($"{key}:{field}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking hash existence for key: {Key}, field: {Field}", key, field);
            return false;
        }
    }

    public async Task HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_database != null)
            {
                await _database.HashDeleteAsync(key, field);
                _logger.LogDebug("Hash deleted for key: {Key}, field: {Field}", key, field);
            }
            else
            {
                // Fallback to regular cache
                await RemoveAsync($"{key}:{field}", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hash value for key: {Key}, field: {Field}", key, field);
        }
    }
}