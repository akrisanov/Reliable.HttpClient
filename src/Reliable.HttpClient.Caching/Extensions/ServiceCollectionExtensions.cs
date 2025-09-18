using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching.Extensions;

/// <summary>
/// Extension methods for ServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds HTTP caching services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configure default cache options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddHttpCaching(
        this IServiceCollection services,
        Action<HttpCacheOptions>? configureOptions = null)
    {
        // Register default cache options
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // Register memory cache if not already registered
        services.TryAddSingleton<IMemoryCache, MemoryCache>();

        return services;
    }

    /// <summary>
    /// Adds HttpClientWithCache as both IHttpClientWithCache and IHttpClientAdapter
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configure default cache options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddHttpClientWithCache(
        this IServiceCollection services,
        Action<HttpCacheOptions>? configureOptions = null)
    {
        // Ensure memory cache is available
        var hasMemoryCache = services.Any(static x => x.ServiceType == typeof(IMemoryCache));
        if (!hasMemoryCache)
        {
            services.AddMemoryCache();
        }

        // Ensure HttpClient is registered
        services.AddHttpClient();

        // Register core dependencies
        services.TryAddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();
        services.TryAddSingleton<ISimpleCacheKeyGenerator, DefaultSimpleCacheKeyGenerator>();

        // Configure cache options
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // Register HttpClientWithCache for both interfaces
        services.TryAddScoped<HttpClientWithCache>();
        services.TryAddScoped<IHttpClientWithCache>(provider => provider.GetRequiredService<HttpClientWithCache>());
        services.TryAddScoped<IHttpClientAdapter>(provider => provider.GetRequiredService<HttpClientWithCache>());

        return services;
    }
}
