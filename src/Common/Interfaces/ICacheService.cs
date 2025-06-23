namespace Common.Interfaces;

public interface ICacheService
{
    // Basic operations
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    // Pattern operations
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    // Get or Set pattern
    Task<T> GetOrSetAsync<T>(
        string key, 
        Func<Task<T>> getItem, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default);

    // Bulk operations
    Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    // Cache refresh
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);
    
    // Hash operations (Redis specific, no-op for memory cache)
    Task<T?> HashGetAsync<T>(string key, string field, CancellationToken cancellationToken = default);
    Task HashSetAsync<T>(string key, string field, T value, CancellationToken cancellationToken = default);
    Task<bool> HashExistsAsync(string key, string field, CancellationToken cancellationToken = default);
    Task HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default);
}