using System.Net;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Providers;

namespace Reliable.HttpClient.Caching.Extensions;

/// <summary>
/// Extension methods for adding HTTP caching to HttpClient
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds memory caching to HttpClient with automatic dependency registration
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="configureOptions">Configure cache options</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddMemoryCache<TResponse>(
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
    /// Adds custom cache provider to HttpClient
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <typeparam name="TCacheProvider">Cache provider type</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="configureOptions">Configure cache options</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddCache<TResponse, TCacheProvider>(
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
    /// Adds memory caching with a predefined preset
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="preset">Predefined cache configuration</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddMemoryCache<TResponse>(
        this IHttpClientBuilder builder,
        HttpCacheOptions preset)
    {
        return builder.AddMemoryCache<TResponse>(options => CopyPresetToOptions(preset, options));
    }

    /// <summary>
    /// Copies preset settings to options
    /// </summary>
    private static void CopyPresetToOptions(HttpCacheOptions preset, HttpCacheOptions options)
    {
        options.DefaultExpiry = preset.DefaultExpiry;
        options.MaxCacheSize = preset.MaxCacheSize;
        options.KeyGenerator = preset.KeyGenerator;
        options.CacheableStatusCodes = new HashSet<HttpStatusCode>(preset.CacheableStatusCodes);
        options.CacheableMethods = new HashSet<HttpMethod>(preset.CacheableMethods);
        options.ShouldCache = preset.ShouldCache;

        // Create a new GetExpiry function that uses the correct DefaultExpiry
        options.GetExpiry = (request, response) =>
        {
            // Check Cache-Control max-age directive
            if (response.Headers.CacheControl?.MaxAge is not null)
            {
                return response.Headers.CacheControl.MaxAge.Value;
            }

            // Check Cache-Control no-cache or no-store directives
            if (response.Headers.CacheControl is not null)
            {
                if (response.Headers.CacheControl.NoCache || response.Headers.CacheControl.NoStore)
                    return TimeSpan.Zero;
            }

            // Fall back to the configured default expiry
            return options.DefaultExpiry;
        };
    }

    /// <summary>
    /// Adds short-term memory caching (1 minute expiry)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddShortTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.ShortTerm);

    /// <summary>
    /// Adds medium-term memory caching (10 minutes expiry)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddMediumTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.MediumTerm);

    /// <summary>
    /// Adds long-term memory caching (1 hour expiry)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddLongTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.LongTerm);

    /// <summary>
    /// Adds high-performance memory caching (5 minutes expiry, larger cache)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddHighPerformanceCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.HighPerformance);

    /// <summary>
    /// Adds configuration data caching (30 minutes expiry)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddConfigurationCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.Configuration);

    /// <summary>
    /// Adds both resilience policies and memory caching in one call
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="configureResilience">Configure resilience options</param>
    /// <param name="configureCache">Configure cache options</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddResilienceWithCaching<TResponse>(
        this IHttpClientBuilder builder,
        Action<HttpClientOptions>? configureResilience = null,
        Action<HttpCacheOptions>? configureCache = null)
    {
        return builder
            .AddResilience(configureResilience)
            .AddMemoryCache<TResponse>(configureCache);
    }

    /// <summary>
    /// Adds resilience policies with preset-based caching
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="resiliencePreset">Predefined resilience configuration</param>
    /// <param name="cachePreset">Predefined cache configuration</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddResilienceWithCaching<TResponse>(
        this IHttpClientBuilder builder,
        HttpClientOptions resiliencePreset,
        HttpCacheOptions cachePreset)
    {
        return builder
            .AddResilience(resiliencePreset)
            .AddMemoryCache<TResponse>(cachePreset);
    }

    /// <summary>
    /// Adds resilience with short-term caching (1 minute)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddResilienceWithShortTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddResilienceWithCaching<TResponse>(configureResilience: null, options => CopyPresetToOptions(CachePresets.ShortTerm, options));

    /// <summary>
    /// Adds resilience with medium-term caching (10 minutes)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddResilienceWithMediumTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddResilienceWithCaching<TResponse>(configureResilience: null, options => CopyPresetToOptions(CachePresets.MediumTerm, options));

    /// <summary>
    /// Adds resilience with long-term caching (1 hour)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddResilienceWithLongTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddResilienceWithCaching<TResponse>(configureResilience: null, options => CopyPresetToOptions(CachePresets.LongTerm, options));
}
