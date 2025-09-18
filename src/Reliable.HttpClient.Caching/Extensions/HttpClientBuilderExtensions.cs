using System.Net;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Generic.Extensions;

namespace Reliable.HttpClient.Caching.Extensions;

/// <summary>
/// Extension methods for adding universal HTTP caching to HttpClient.
/// For generic/type-safe caching, use extensions from Reliable.HttpClient.Caching.Generic.Extensions namespace.
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds memory caching with configurable options using a builder pattern
    /// </summary>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="configureOptions">Action to configure cache options using builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddMemoryCache(
        this IHttpClientBuilder builder,
        Action<HttpCacheOptionsBuilder>? configureOptions = null)
    {
        var optionsBuilder = new HttpCacheOptionsBuilder();
        configureOptions?.Invoke(optionsBuilder);
        HttpCacheOptions options = optionsBuilder.Build();

        return builder.AddMemoryCache(options);
    }

    /// <summary>
    /// Adds memory caching with pre-built options
    /// </summary>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="options">Pre-configured cache options</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddMemoryCache(
        this IHttpClientBuilder builder,
        HttpCacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        builder.Services.AddMemoryCache();
        builder.Services.TryAddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();
        builder.Services.TryAddSingleton<ISimpleCacheKeyGenerator, DefaultSimpleCacheKeyGenerator>();

        builder.Services.AddScoped(serviceProvider =>
        {
            System.Net.Http.HttpClient httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(builder.Name);
            IMemoryCache cache = serviceProvider.GetRequiredService<IMemoryCache>();
            IHttpResponseHandler responseHandler = serviceProvider.GetRequiredService<IHttpResponseHandler>();
            ISimpleCacheKeyGenerator cacheKeyGenerator = serviceProvider.GetRequiredService<ISimpleCacheKeyGenerator>();
            ILogger<HttpClientWithCache>? logger = serviceProvider.GetService<ILogger<HttpClientWithCache>>();

            return new HttpClientWithCache(httpClient, cache, responseHandler, options, cacheKeyGenerator, logger);
        });

        return builder;
    }

    /// <summary>
    /// Adds memory caching with a predefined preset using generic caching
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <param name="preset">Predefined cache configuration</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddMemoryCache<TResponse>(
        this IHttpClientBuilder builder,
        HttpCacheOptions preset)
    {
        return builder.AddGenericMemoryCache<TResponse>(options => CopyPresetToOptions(preset, options));
    }

    /// <summary>
    /// Copies preset settings to options
    /// </summary>
    private static void CopyPresetToOptions(HttpCacheOptions preset, HttpCacheOptions options)
    {
        options.DefaultExpiry = preset.DefaultExpiry;
        options.DefaultHeaders = new Dictionary<string, string>(preset.DefaultHeaders, StringComparer.OrdinalIgnoreCase);
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
    /// Adds short-term memory caching (1 minute expiry) using generic caching
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddShortTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.ShortTerm);

    /// <summary>
    /// Adds medium-term memory caching (10 minutes expiry) using generic caching
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddMediumTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.MediumTerm);

    /// <summary>
    /// Adds long-term memory caching (1 hour expiry) using generic caching
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddLongTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.LongTerm);

    /// <summary>
    /// Adds high-performance memory caching (5 minutes expiry, larger cache) using generic caching
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddHighPerformanceCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.HighPerformance);

    /// <summary>
    /// Adds configuration data caching (30 minutes expiry) using generic caching
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddConfigurationCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddMemoryCache<TResponse>(CachePresets.Configuration);

    /// <summary>
    /// Adds both resilience policies and memory caching in one call using generic caching
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
            .AddGenericMemoryCache<TResponse>(configureCache);
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
        => builder.AddResilienceWithCaching<TResponse>(
            configureResilience: null, options => CopyPresetToOptions(CachePresets.ShortTerm, options));

    /// <summary>
    /// Adds resilience with medium-term caching (10 minutes)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddResilienceWithMediumTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddResilienceWithCaching<TResponse>(
            configureResilience: null, options => CopyPresetToOptions(CachePresets.MediumTerm, options));

    /// <summary>
    /// Adds resilience with long-term caching (1 hour)
    /// </summary>
    /// <typeparam name="TResponse">Response type to cache</typeparam>
    /// <param name="builder">HttpClient builder</param>
    /// <returns>HttpClient builder for chaining</returns>
    public static IHttpClientBuilder AddResilienceWithLongTermCache<TResponse>(this IHttpClientBuilder builder)
        => builder.AddResilienceWithCaching<TResponse>(
            configureResilience: null, options => CopyPresetToOptions(CachePresets.LongTerm, options));
}
