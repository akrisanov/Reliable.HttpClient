using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Extensions;

namespace Reliable.HttpClient.Caching.Tests;

public class HttpClientWithCacheExtensionsTests
{
    [Fact]
    public void AddHttpClientWithCache_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();

        // Act
        services.AddHttpClientWithCache("CachedClient");
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IHttpClientWithCache>().Should().NotBeNull().And.BeOfType<HttpClientWithCache>();
        serviceProvider.GetService<IMemoryCache>().Should().NotBeNull();
        serviceProvider.GetService<ISimpleCacheKeyGenerator>().Should().NotBeNull();
        serviceProvider.GetService<System.Net.Http.HttpClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddHttpClientWithCache_WithNamedClient_RegistersNamedHttpClient()
    {
        // Arrange
        const string clientName = "CachedClient";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();

        // Act
        services.AddHttpClientWithCache(clientName);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient(clientName);
        httpClient.Should().NotBeNull();

        IHttpClientWithCache httpClientWithCache = serviceProvider.GetRequiredService<IHttpClientWithCache>();
        httpClientWithCache.Should().NotBeNull().And.BeOfType<HttpClientWithCache>();
    }

    [Fact]
    public void AddHttpClientWithCache_WithCacheOptions_AppliesCacheOptions()
    {
        // Arrange
        const string clientName = "CachedClient";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();

        // Act
        services.AddHttpClientWithCache(clientName, options => options.DefaultExpiry = TimeSpan.FromMinutes(10));
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        var httpClientWithCache = serviceProvider.GetRequiredService<IHttpClientWithCache>() as HttpClientWithCache;
        httpClientWithCache.Should().NotBeNull();

        // Проверяем, что HttpCacheOptions правильно настроены для MediumTerm preset
        // Используем IOptionsSnapshot для получения настроек именованного клиента "KodySuCached"
        IOptionsSnapshot<HttpCacheOptions> optionsSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<HttpCacheOptions>>();
        HttpCacheOptions registeredOptions = optionsSnapshot.Get(clientName);

        registeredOptions.DefaultExpiry.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void AddResilientHttpClientWithCache_RegistersAllRequiredServices()
    {
        // Arrange
        const string clientName = "ResilientClient";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();

        // Act
        services.AddResilientHttpClientWithCache(clientName);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient(clientName);
        httpClient.Should().NotBeNull();

        serviceProvider.GetService<IHttpClientWithCache>().Should().NotBeNull().And.BeOfType<HttpClientWithCache>();
        serviceProvider.GetService<IMemoryCache>().Should().NotBeNull();
        serviceProvider.GetService<ISimpleCacheKeyGenerator>().Should().NotBeNull();
    }

    [Fact]
    public void AddHttpClientWithCache_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        const string clientName = "CachedClient";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();

        // Act & Assert
        Func<IServiceCollection> act = () => services
            .AddHttpClientWithCache(clientName)
            .AddHttpClientWithCache(clientName);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddResilientHttpClientWithCache_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        const string clientName = "ResilientClient";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();

        // Act & Assert
        Func<IServiceCollection> act = () => services
            .AddResilientHttpClientWithCache(clientName)
            .AddResilientHttpClientWithCache(clientName);

        act.Should().NotThrow();
    }
}
