using System.Reflection;
using System.Text;
using Castle.DynamicProxy;
using Common.Attributes;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Common.Interceptors;

public class CacheInterceptor : IInterceptor
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInterceptor> _logger;

    public CacheInterceptor(ICacheService cacheService, ILogger<CacheInterceptor> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        
        // Handle cache eviction first (before invocation if specified)
        var evictAttribute = method.GetCustomAttribute<CacheEvictAttribute>();
        if (evictAttribute?.BeforeInvocation == true)
        {
            HandleCacheEviction(evictAttribute, invocation);
        }

        // Handle caching
        var cacheAttribute = method.GetCustomAttribute<CacheAttribute>();
        if (cacheAttribute != null)
        {
            HandleCaching(cacheAttribute, invocation);
        }
        else
        {
            invocation.Proceed();
        }

        // Handle cache eviction after invocation
        if (evictAttribute?.BeforeInvocation == false || evictAttribute?.BeforeInvocation == null)
        {
            if (evictAttribute != null)
            {
                HandleCacheEviction(evictAttribute, invocation);
            }
        }

        // Handle cache update
        var updateAttribute = method.GetCustomAttribute<CacheUpdateAttribute>();
        if (updateAttribute != null)
        {
            HandleCacheUpdate(updateAttribute, invocation);
        }
    }

    private void HandleCaching(CacheAttribute cacheAttribute, IInvocation invocation)
    {
        var cacheKey = GenerateCacheKey(cacheAttribute, invocation);
        
        if (IsAsyncMethod(invocation.Method))
        {
            HandleAsyncCaching(cacheAttribute, invocation, cacheKey);
        }
        else
        {
            HandleSyncCaching(cacheAttribute, invocation, cacheKey);
        }
    }

    private void HandleAsyncCaching(CacheAttribute cacheAttribute, IInvocation invocation, string cacheKey)
    {
        var returnType = invocation.Method.ReturnType;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var method = typeof(CacheInterceptor)
                .GetMethod(nameof(CacheAsyncMethod), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.MakeGenericMethod(resultType);
            
            invocation.ReturnValue = method?.Invoke(this, new object[] { cacheAttribute, invocation, cacheKey });
        }
    }

    private async Task<T> CacheAsyncMethod<T>(CacheAttribute cacheAttribute, IInvocation invocation, string cacheKey)
    {
        try
        {
            var cachedValue = await _cacheService.GetAsync<T>(cacheKey);
            if (cachedValue != null && (!cacheAttribute.IgnoreNullValues || !IsNullOrDefault(cachedValue)))
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
            invocation.Proceed();

            if (invocation.ReturnValue is Task<T> task)
            {
                var result = await task;
                if (result != null || !cacheAttribute.IgnoreNullValues)
                {
                    var expiration = cacheAttribute.SlidingExpiration 
                        ? TimeSpan.FromMinutes(cacheAttribute.ExpirationMinutes)
                        : TimeSpan.FromMinutes(cacheAttribute.ExpirationMinutes);
                    
                    await _cacheService.SetAsync(cacheKey, result, expiration);
                    _logger.LogDebug("Cached result for key: {CacheKey}", cacheKey);
                }
                return result;
            }
            
            throw new InvalidOperationException("Method return type is not compatible with caching");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cache interceptor for key: {CacheKey}", cacheKey);
            invocation.Proceed();
            
            if (invocation.ReturnValue is Task<T> fallbackTask)
            {
                return await fallbackTask;
            }
            
            return default(T)!;
        }
    }

    private void HandleSyncCaching(CacheAttribute cacheAttribute, IInvocation invocation, string cacheKey)
    {
        try
        {
            var cachedValue = _cacheService.GetAsync<object>(cacheKey).GetAwaiter().GetResult();
            if (cachedValue != null && (!cacheAttribute.IgnoreNullValues || !IsNullOrDefault(cachedValue)))
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                invocation.ReturnValue = cachedValue;
                return;
            }

            _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
            invocation.Proceed();

            if (invocation.ReturnValue != null || !cacheAttribute.IgnoreNullValues)
            {
                var expiration = TimeSpan.FromMinutes(cacheAttribute.ExpirationMinutes);
                _cacheService.SetAsync(cacheKey, invocation.ReturnValue, expiration).GetAwaiter().GetResult();
                _logger.LogDebug("Cached result for key: {CacheKey}", cacheKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cache interceptor for key: {CacheKey}", cacheKey);
            invocation.Proceed();
        }
    }

    private void HandleCacheEviction(CacheEvictAttribute evictAttribute, IInvocation invocation)
    {
        try
        {
            if (evictAttribute.EvictAll)
            {
                // Note: This would require a more sophisticated cache implementation
                _logger.LogWarning("EvictAll is not implemented - consider using specific patterns");
                return;
            }

            if (evictAttribute.KeyPatterns != null)
            {
                foreach (var pattern in evictAttribute.KeyPatterns)
                {
                    var expandedPattern = ExpandPattern(pattern, invocation);
                    _cacheService.RemoveByPatternAsync(expandedPattern).GetAwaiter().GetResult();
                    _logger.LogDebug("Evicted cache entries matching pattern: {Pattern}", expandedPattern);
                }
            }

            if (evictAttribute.Tags != null)
            {
                foreach (var tag in evictAttribute.Tags)
                {
                    _cacheService.RemoveByPatternAsync($"*{tag}*").GetAwaiter().GetResult();
                    _logger.LogDebug("Evicted cache entries with tag: {Tag}", tag);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evicting cache");
        }
    }

    private void HandleCacheUpdate(CacheUpdateAttribute updateAttribute, IInvocation invocation)
    {
        if (invocation.ReturnValue == null) return;

        try
        {
            var cacheKey = GenerateCacheKey(updateAttribute, invocation);
            var expiration = TimeSpan.FromMinutes(updateAttribute.ExpirationMinutes);
            
            _cacheService.SetAsync(cacheKey, invocation.ReturnValue, expiration).GetAwaiter().GetResult();
            _logger.LogDebug("Updated cache for key: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cache");
        }
    }

    private string GenerateCacheKey(CacheAttribute cacheAttribute, IInvocation invocation)
    {
        var keyBuilder = new StringBuilder();
        
        // Add prefix
        if (!string.IsNullOrEmpty(cacheAttribute.KeyPrefix))
        {
            keyBuilder.Append(cacheAttribute.KeyPrefix);
        }
        else
        {
            keyBuilder.Append($"{invocation.TargetType.Name}_{invocation.Method.Name}");
        }

        // Add parameters
        if (cacheAttribute.KeyParameters != null && cacheAttribute.KeyParameters.Length > 0)
        {
            var parameters = invocation.Method.GetParameters();
            var arguments = invocation.Arguments;

            for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
            {
                if (cacheAttribute.KeyParameters.Contains(parameters[i].Name))
                {
                    keyBuilder.Append($"_{parameters[i].Name}_{GetParameterValue(arguments[i])}");
                }
            }
        }
        else
        {
            // Include all parameters if none specified
            foreach (var arg in invocation.Arguments)
            {
                keyBuilder.Append($"_{GetParameterValue(arg)}");
            }
        }

        return keyBuilder.ToString();
    }

    private string GenerateCacheKey(CacheUpdateAttribute updateAttribute, IInvocation invocation)
    {
        var keyBuilder = new StringBuilder();
        
        if (!string.IsNullOrEmpty(updateAttribute.KeyPrefix))
        {
            keyBuilder.Append(updateAttribute.KeyPrefix);
        }
        else
        {
            keyBuilder.Append($"{invocation.TargetType.Name}_{invocation.Method.Name}");
        }

        if (updateAttribute.KeyParameters != null)
        {
            var parameters = invocation.Method.GetParameters();
            var arguments = invocation.Arguments;

            for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
            {
                if (updateAttribute.KeyParameters.Contains(parameters[i].Name))
                {
                    keyBuilder.Append($"_{parameters[i].Name}_{GetParameterValue(arguments[i])}");
                }
            }
        }

        return keyBuilder.ToString();
    }

    private string ExpandPattern(string pattern, IInvocation invocation)
    {
        var result = pattern;
        var parameters = invocation.Method.GetParameters();
        var arguments = invocation.Arguments;

        for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
        {
            var placeholder = $"{{{parameters[i].Name}}}";
            if (result.Contains(placeholder))
            {
                result = result.Replace(placeholder, GetParameterValue(arguments[i]));
            }
        }

        return result;
    }

    private string GetParameterValue(object? parameter)
    {
        if (parameter == null) return "null";
        
        if (parameter is string or int or long or bool or DateTime or Guid)
        {
            return parameter.ToString() ?? "null";
        }

        try
        {
            return JsonSerializer.Serialize(parameter);
        }
        catch
        {
            return parameter.GetHashCode().ToString();
        }
    }

    private bool IsAsyncMethod(MethodInfo method)
    {
        return method.ReturnType.IsGenericType && 
               method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    private bool IsNullOrDefault<T>(T value)
    {
        return EqualityComparer<T>.Default.Equals(value, default(T));
    }
}