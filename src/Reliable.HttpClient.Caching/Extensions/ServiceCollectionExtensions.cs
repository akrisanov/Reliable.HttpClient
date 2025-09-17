using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Providers;

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
    /// Adds HTTP client caching for a specific HTTP client and response type
    /// </summary>
    /// <typeparam name="TClient">HTTP client type</typeparam>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configure cache options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddHttpClientCaching<TClient, TResponse>(
        this IServiceCollection services,
        Action<HttpCacheOptions>? configureOptions = null)
        where TClient : class
    {
        // Check if IMemoryCache is registered
        var hasMemoryCache = services.Any(static x => x.ServiceType == typeof(IMemoryCache));
        if (!hasMemoryCache)
        {
            throw new ArgumentException(
                "IMemoryCache is not registered. Please call services.AddMemoryCache() or services.AddHttpCaching() first.",
                nameof(services));
        }

        // Register default cache options
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // Register cache key generator as singleton
        services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

        // Register cache provider as scoped (one per request/scope)
        services.TryAddScoped<IHttpResponseCache<TResponse>, MemoryCacheProvider<TResponse>>();

        // Register cached HTTP client as scoped
        services.TryAddScoped<CachedHttpClient<TResponse>>();

        return services;
    }
}
