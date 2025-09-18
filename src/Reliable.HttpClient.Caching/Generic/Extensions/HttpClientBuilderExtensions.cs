using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Providers;

namespace Reliable.HttpClient.Caching.Generic.Extensions;

/// <summary>
/// Extension methods for adding generic HTTP caching to HttpClient builders
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds memory caching to HttpClient with automatic dependency registration for generic responses.
    /// This method registers the generic CachedHttpClient&lt;TResponse&gt; for type-safe caching.
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="configureOptions">Configure cache options</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddGenericMemoryCache<TResponse>(
        this IHttpClientBuilder builder,
        Action<HttpCacheOptions>? configureOptions = null)
    {
        // Register cache options using Options pattern with client name
        if (configureOptions is not null)
        {
            builder.Services.Configure(builder.Name, configureOptions);
        }

        // Register memory cache if not already registered (automatic dependency registration)
        builder.Services.TryAddSingleton<IMemoryCache, MemoryCache>();

        // Register cache key generator if not already registered
        builder.Services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

        // Register cache provider as scoped (one per request/scope)
        builder.Services.TryAddScoped<IHttpResponseCache<TResponse>, MemoryCacheProvider<TResponse>>();

        // Register cached HTTP client as scoped
        builder.Services.TryAddScoped<CachedHttpClient<TResponse>>();

        return builder;
    }

    /// <summary>
    /// Adds custom cache provider to HttpClient for generic responses.
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <typeparam name="TCacheProvider">Cache provider type</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="configureOptions">Configure cache options</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddGenericCache<TResponse, TCacheProvider>(
        this IHttpClientBuilder builder,
        Action<HttpCacheOptions>? configureOptions = null)
        where TCacheProvider : class, IHttpResponseCache<TResponse>
    {
        // Register cache options using Options pattern with client name
        if (configureOptions is not null)
        {
            builder.Services.Configure(builder.Name, configureOptions);
        }

        // Register cache key generator if not already registered
        builder.Services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

        // Register custom cache provider as scoped
        builder.Services.TryAddScoped<IHttpResponseCache<TResponse>, TCacheProvider>();

        // Register cached HTTP client as scoped
        builder.Services.TryAddScoped<CachedHttpClient<TResponse>>();

        return builder;
    }

    /// <summary>
    /// Adds distributed caching to HttpClient for generic responses using Redis or SQL Server.
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="configureOptions">Configure cache options</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddGenericDistributedCache<TResponse>(
        this IHttpClientBuilder builder,
        Action<HttpCacheOptions>? configureOptions = null)
    {
        // Register cache options using Options pattern with client name
        if (configureOptions is not null)
        {
            builder.Services.Configure(builder.Name, configureOptions);
        }

        // Register cache key generator if not already registered
        builder.Services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

        // Register distributed cache provider as scoped
        builder.Services.TryAddScoped<IHttpResponseCache<TResponse>, MemoryCacheProvider<TResponse>>();

        // Register cached HTTP client as scoped
        builder.Services.TryAddScoped<CachedHttpClient<TResponse>>();

        return builder;
    }
}
