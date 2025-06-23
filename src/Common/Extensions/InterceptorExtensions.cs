using Castle.DynamicProxy;
using Common.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Extensions;

public static class InterceptorExtensions
{
    public static IServiceCollection AddInterceptors(this IServiceCollection services)
    {
        services.AddSingleton<ProxyGenerator>();
        services.AddScoped<CacheInterceptor>();
        services.AddScoped<LoggingInterceptor>();
        
        return services;
    }

    public static IServiceCollection AddCacheInterception<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TImplementation>();
        services.AddScoped<TInterface>(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
            var interceptor = provider.GetRequiredService<CacheInterceptor>();
            var target = provider.GetRequiredService<TImplementation>();
            
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(target, interceptor);
        });
        
        return services;
    }

    public static IServiceCollection AddLoggingInterception<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TImplementation>();
        services.AddScoped<TInterface>(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
            var interceptor = provider.GetRequiredService<LoggingInterceptor>();
            var target = provider.GetRequiredService<TImplementation>();
            
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(target, interceptor);
        });
        
        return services;
    }

    public static IServiceCollection AddFullInterception<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TImplementation>();
        services.AddScoped<TInterface>(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
            var cacheInterceptor = provider.GetRequiredService<CacheInterceptor>();
            var loggingInterceptor = provider.GetRequiredService<LoggingInterceptor>();
            var target = provider.GetRequiredService<TImplementation>();
            
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(
                target, 
                loggingInterceptor, // Execute first (wraps around everything)
                cacheInterceptor    // Execute second (handles caching logic)
            );
        });
        
        return services;
    }
}