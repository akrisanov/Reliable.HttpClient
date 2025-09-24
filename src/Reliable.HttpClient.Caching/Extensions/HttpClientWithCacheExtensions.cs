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

        // Register the universal HTTP client with cache as scoped to avoid captive dependency
        services.AddScoped<IHttpClientWithCache>(sp => CreateHttpClientWithCache(sp, httpClientName, cacheOptions));
        services.AddScoped(sp => (HttpClientWithCache)sp.GetRequiredService<IHttpClientWithCache>());

        return services;
    }

    /// <summary>
    /// Adds universal HTTP client with caching to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="httpClientName">HTTP client name (optional)</param>
    /// <param name="configureCacheOptions">Action to configure cache options
    /// which then will be registered as <see cref="IOptions{TOptions}"/> named after <paramref name="httpClientName"/></param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddHttpClientWithCache(
        this IServiceCollection services,
        string? httpClientName,
        Action<HttpCacheOptions> configureCacheOptions)
    {
        var options = new HttpCacheOptions();
        configureCacheOptions(options);
        services.Configure(httpClientName, configureCacheOptions);

        return services.AddHttpClientWithCache(httpClientName, options);
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
        Action<HttpCacheOptions> configureCacheOptions,
        Action<HttpClientOptions>? configureResilience = null)
    {
        var cacheOptions = new HttpCacheOptions();
        configureCacheOptions(cacheOptions);
        services.Configure(httpClientName, configureCacheOptions);

        return services.AddResilientHttpClientWithCache(httpClientName, configureResilience, cacheOptions);
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
        Action<HttpCacheOptions> configureCacheOptions,
        Action<HttpClientOptions>? customizeOptions = null)
    {
        var cacheOptions = new HttpCacheOptions();
        configureCacheOptions(cacheOptions);
        services.Configure(httpClientName, configureCacheOptions);

        return services.AddResilientHttpClientWithCache(httpClientName, preset, customizeOptions, cacheOptions);
    }

    /// <summary>
    /// Creates an instance of <see cref="HttpClientWithCache"/> using the provided service provider and configuration.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve required dependencies.</param>
    /// <param name="httpClientName">The name of the HTTP client to retrieve from the <see cref="IHttpClientFactory"/>.
    /// <param name="cacheOptions">Cache options including default headers and settings (optional)</param>
    /// If null or empty, a default client is created.</param>
    /// <returns>A configured instance of <see cref="HttpClientWithCache"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a required service (e.g., <see cref="IHttpClientFactory"/>, <see cref="IMemoryCache"/>, 
    /// <see cref="IHttpResponseHandler"/>, or <see cref="ISimpleCacheKeyGenerator"/>) is not registered in the service provider.</exception>
    private static HttpClientWithCache CreateHttpClientWithCache(
        IServiceProvider serviceProvider,
        string? httpClientName,
        HttpCacheOptions? cacheOptions)
    {
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        System.Net.Http.HttpClient httpClient = httpClientName is null or ""
            ? httpClientFactory.CreateClient()
            : httpClientFactory.CreateClient(httpClientName);

        IMemoryCache cache = serviceProvider.GetRequiredService<IMemoryCache>();
        IHttpResponseHandler responseHandler = serviceProvider.GetRequiredService<IHttpResponseHandler>();
        HttpCacheOptions? cacheOptionsToInject = cacheOptions is null
            ? serviceProvider.GetService<IOptionsSnapshot<HttpCacheOptions>>()?.Get(httpClientName)
            : cacheOptions;
        IOptionsSnapshot<HttpCacheOptions>? cacheOptionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HttpCacheOptions>>();
        ISimpleCacheKeyGenerator cacheKeyGenerator = serviceProvider.GetRequiredService<ISimpleCacheKeyGenerator>();
        ILogger<HttpClientWithCache>? logger = serviceProvider.GetService<ILogger<HttpClientWithCache>>();

        return new HttpClientWithCache(
            httpClient,
            cache,
            responseHandler,
            cacheOptionsToInject,
            cacheKeyGenerator,
            logger);
    }
}
