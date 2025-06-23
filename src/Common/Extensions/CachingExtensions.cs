using Common.Interfaces;
using Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Common.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        CacheProvider provider = CacheProvider.InMemory)
    {
        switch (provider)
        {
            case CacheProvider.InMemory:
                services.AddMemoryCache();
                services.AddScoped<ICacheService, MemoryCacheService>();
                break;

            case CacheProvider.Redis:
                var connectionString = configuration.GetConnectionString("Redis") ?? 
                                     configuration["Redis:ConnectionString"];
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Redis connection string is required when using Redis cache provider");
                }

                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = connectionString;
                    options.InstanceName = configuration["Redis:InstanceName"] ?? "CleanArchTemplate";
                });

                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    return ConnectionMultiplexer.Connect(connectionString);
                });

                services.AddScoped<ICacheService, RedisCacheService>();
                break;

            case CacheProvider.Hybrid:
                // Add both providers
                services.AddMemoryCache();
                services.AddStackExchangeRedisCache(options =>
                {
                    var redisConnectionString = configuration.GetConnectionString("Redis") ?? 
                                              configuration["Redis:ConnectionString"];
                    options.Configuration = redisConnectionString;
                    options.InstanceName = configuration["Redis:InstanceName"] ?? "CleanArchTemplate";
                });

                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var redisConnectionString = configuration.GetConnectionString("Redis") ?? 
                                              configuration["Redis:ConnectionString"];
                    return ConnectionMultiplexer.Connect(redisConnectionString!);
                });

                // Register both services
                services.AddScoped<MemoryCacheService>();
                services.AddScoped<RedisCacheService>();
                
                // Use a factory to decide which one to use
                services.AddScoped<ICacheService>(sp =>
                {
                    var useRedis = configuration.GetValue<bool>("Cache:UseRedisForDistributed", true);
                    return useRedis ? sp.GetRequiredService<RedisCacheService>() : sp.GetRequiredService<MemoryCacheService>();
                });
                break;

            default:
                throw new ArgumentException($"Unsupported cache provider: {provider}");
        }

        return services;
    }

    public static IServiceCollection AddDistributedCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddCaching(configuration, CacheProvider.Redis);
    }

    public static IServiceCollection AddInMemoryCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ICacheService, MemoryCacheService>();
        return services;
    }
}

public enum CacheProvider
{
    InMemory,
    Redis,
    Hybrid
}