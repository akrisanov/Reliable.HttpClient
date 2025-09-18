using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Providers;

namespace Reliable.HttpClient.Caching.Generic.Extensions;

/// <summary>
/// Extension methods for ServiceCollection to register generic cached HTTP clients
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds HTTP client caching for a specific HTTP client and response type.
    /// This method registers the generic CachedHttpClient&lt;TResponse&gt; for type-safe caching.
    /// </summary>
    /// <typeparam name="TClient">HTTP client type</typeparam>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configure cache options</param>
    /// <returns>Service collection for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when IMemoryCache is not registered</exception>
    public static IServiceCollection AddGenericHttpClientCaching<TClient, TResponse>(
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

    /// <summary>
    /// Adds generic HTTP client caching with default memory cache provider.
    /// This is a convenience method that includes memory cache registration.
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configure cache options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGenericHttpClientCaching<TResponse>(
        this IServiceCollection services,
        Action<HttpCacheOptions>? configureOptions = null)
    {
        // Ensure memory cache is registered
        services.TryAddSingleton<IMemoryCache, MemoryCache>();

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
