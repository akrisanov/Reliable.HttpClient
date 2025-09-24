using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// <param name="configureCacheOptions">Action to configure cache options</param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddHttpClientWithCache(
        this IServiceCollection services,
        string? httpClientName = null,
        Action<HttpCacheOptions>? configureCacheOptions = null)
    {
        // Register dependencies
        services.AddMemoryCache();
        services.AddSingleton<ISimpleCacheKeyGenerator, DefaultSimpleCacheKeyGenerator>();

        if (configureCacheOptions is not null)
        {
            services.Configure(httpClientName, configureCacheOptions);
        }

        // Register the universal HTTP client with cache as scoped to avoid captive dependency
        services.AddScoped<IHttpClientWithCache>(sp => CreateHttpClientWithCache(sp, httpClientName));
        services.AddScoped(sp => (HttpClientWithCache)sp.GetRequiredService<IHttpClientWithCache>());

        return services;
    }

    /// <summary>
    /// Adds universal HTTP client with caching and resilience to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="httpClientName">HTTP client name</param>
    /// <param name="configureResilience">Action to configure resilience options</param>
    /// <param name="configureCacheOptions">Action to configure cache options</param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddResilientHttpClientWithCache(
        this IServiceCollection services,
        string httpClientName,
        Action<HttpClientOptions>? configureResilience = null,
        Action<HttpCacheOptions>? configureCacheOptions = null)
    {
        // Add HTTP client with resilience
        services.AddHttpClient(httpClientName)
                .AddResilience(configureResilience);

        // Add universal HTTP client with cache
        return services.AddHttpClientWithCache(httpClientName, configureCacheOptions);
    }

    /// <summary>
    /// Adds universal HTTP client with caching using preset resilience configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="httpClientName">HTTP client name</param>
    /// <param name="preset">Predefined resilience preset</param>
    /// <param name="customizeOptions">Optional action to customize preset options</param>
    /// <param name="configureCacheOptions">Action to configure cache options</param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddResilientHttpClientWithCache(
        this IServiceCollection services,
        string httpClientName,
        HttpClientOptions preset,
        Action<HttpClientOptions>? customizeOptions = null,
        Action<HttpCacheOptions>? configureCacheOptions = null)
    {
        // Add HTTP client with resilience preset
        services.AddHttpClient(httpClientName)
                .AddResilience(preset, customizeOptions);

        // Add universal HTTP client with cache
        return services.AddHttpClientWithCache(httpClientName, configureCacheOptions);
    }

    /// <summary>
    /// Creates an instance of <see cref="HttpClientWithCache"/> using the provided service provider and configuration.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve required dependencies.</param>
    /// <param name="httpClientName">The name of the HTTP client to retrieve from the <see cref="IHttpClientFactory"/>.
    /// If null or empty, a default client is created.</param>
    /// <returns>A configured instance of <see cref="HttpClientWithCache"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a required service (e.g., <see cref="IHttpClientFactory"/>, <see cref="IMemoryCache"/>, 
    /// <see cref="IHttpResponseHandler"/>, or <see cref="ISimpleCacheKeyGenerator"/>) is not registered in the service provider.</exception>
    private static HttpClientWithCache CreateHttpClientWithCache(
        IServiceProvider serviceProvider,
        string? httpClientName)
    {
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        System.Net.Http.HttpClient httpClient = httpClientName is null or ""
            ? httpClientFactory.CreateClient()
            : httpClientFactory.CreateClient(httpClientName);

        IMemoryCache cache = serviceProvider.GetRequiredService<IMemoryCache>();
        IHttpResponseHandler responseHandler = serviceProvider.GetRequiredService<IHttpResponseHandler>();
        IOptionsSnapshot<HttpCacheOptions>? cacheOptionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HttpCacheOptions>>();
        ISimpleCacheKeyGenerator cacheKeyGenerator = serviceProvider.GetRequiredService<ISimpleCacheKeyGenerator>();
        ILogger<HttpClientWithCache>? logger = serviceProvider.GetService<ILogger<HttpClientWithCache>>();

        return new HttpClientWithCache(
            httpClient,
            cache,
            responseHandler,
            cacheOptionsSnapshot?.Get(httpClientName),
            cacheKeyGenerator,
            logger);
    }
}
