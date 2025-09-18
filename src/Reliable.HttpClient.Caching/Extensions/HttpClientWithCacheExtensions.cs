using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching.Extensions;

/// <summary>
/// Extension methods for registering universal HTTP client with caching
/// </summary>
public static class HttpClientWithCacheExtensions
{
    /// <summary>
    /// Adds universal HTTP client with caching to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="httpClientName">HTTP client name (optional)</param>
    /// <param name="cacheOptions">Cache options including default headers and settings (optional)</param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddHttpClientWithCache(
        this IServiceCollection services,
        string? httpClientName = null,
        HttpCacheOptions? cacheOptions = null)
    {
        // Register dependencies
        services.AddMemoryCache();
        services.AddSingleton<ISimpleCacheKeyGenerator, DefaultSimpleCacheKeyGenerator>();

        // Register the universal HTTP client with cache
        services.AddSingleton<IHttpClientWithCache>(serviceProvider =>
        {
            IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            System.Net.Http.HttpClient httpClient = httpClientName is null or ""
                ? httpClientFactory.CreateClient()
                : httpClientFactory.CreateClient(httpClientName);

            IMemoryCache cache = serviceProvider.GetRequiredService<IMemoryCache>();
            IHttpResponseHandler responseHandler = serviceProvider.GetRequiredService<IHttpResponseHandler>(); // Use universal handler
            ISimpleCacheKeyGenerator cacheKeyGenerator = serviceProvider.GetRequiredService<ISimpleCacheKeyGenerator>();
            ILogger<HttpClientWithCache>? logger = serviceProvider.GetService<ILogger<HttpClientWithCache>>();

            return new HttpClientWithCache(
                httpClient,
                cache,
                responseHandler,
                cacheOptions,
                cacheKeyGenerator,
                logger);
        });

        return services;
    }

    /// <summary>
    /// Adds universal HTTP client with caching and resilience to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="httpClientName">HTTP client name</param>
    /// <param name="configureResilience">Action to configure resilience options</param>
    /// <param name="cacheOptions">Cache options including default headers and settings (optional)</param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddResilientHttpClientWithCache(
        this IServiceCollection services,
        string httpClientName,
        Action<HttpClientOptions>? configureResilience = null,
        HttpCacheOptions? cacheOptions = null)
    {
        // Add HTTP client with resilience
        services.AddHttpClient(httpClientName)
                .AddResilience(configureResilience);

        // Add universal HTTP client with cache
        return services.AddHttpClientWithCache(httpClientName, cacheOptions);
    }

    /// <summary>
    /// Adds universal HTTP client with caching using preset resilience configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="httpClientName">HTTP client name</param>
    /// <param name="preset">Predefined resilience preset</param>
    /// <param name="customizeOptions">Optional action to customize preset options</param>
    /// <param name="cacheOptions">Cache options including default headers and settings (optional)</param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddResilientHttpClientWithCache(
        this IServiceCollection services,
        string httpClientName,
        HttpClientOptions preset,
        Action<HttpClientOptions>? customizeOptions = null,
        HttpCacheOptions? cacheOptions = null)
    {
        // Add HTTP client with resilience preset
        services.AddHttpClient(httpClientName)
                .AddResilience(preset, customizeOptions);

        // Add universal HTTP client with cache
        return services.AddHttpClientWithCache(httpClientName, cacheOptions);
    }
}
