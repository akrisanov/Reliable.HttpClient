using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NetHttpClient = System.Net.Http.HttpClient;
using Xunit;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Extensions;
using Reliable.HttpClient.Caching.Generic;
using Reliable.HttpClient.Caching.Generic.Extensions;

namespace Reliable.HttpClient.Caching.Tests.Integration;

public class DependencyInjectionTests
{
    [Fact]
    public void AddHttpClientCaching_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpClient<TestApiClient>();

        // Act
        services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IHttpResponseCache<ApiResponse>>().Should().NotBeNull();
        serviceProvider.GetService<ICacheKeyGenerator>().Should().NotBeNull();
        serviceProvider.GetService<CachedHttpClient<ApiResponse>>().Should().NotBeNull();
    }

    [Fact]
    public void AddHttpClientCaching_WithCustomOptions_UsesCustomConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpClient<TestApiClient>();

        // Act
        services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>(options =>
        {
            options.DefaultExpiry = TimeSpan.FromMinutes(10);
            options.CacheableMethods.Add(HttpMethod.Post);
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        CachedHttpClient<ApiResponse> cachedClient = serviceProvider.GetRequiredService<CachedHttpClient<ApiResponse>>();
        cachedClient.Should().NotBeNull();
    }

    [Fact]
    public void GetRequiredService_CachedHttpClient_ReturnsValidInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpClient<TestApiClient>();
        services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        CachedHttpClient<ApiResponse> cachedClient = serviceProvider.GetRequiredService<CachedHttpClient<ApiResponse>>();

        // Assert
        cachedClient.Should().NotBeNull();
        cachedClient.Should().BeOfType<CachedHttpClient<ApiResponse>>();
    }

    [Fact]
    public void GetRequiredService_IHttpResponseCache_ReturnsMemoryCacheProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpClient<TestApiClient>();
        services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        IHttpResponseCache<ApiResponse> cache = serviceProvider.GetRequiredService<IHttpResponseCache<ApiResponse>>();

        // Assert
        cache.Should().NotBeNull();
        cache.Should().BeAssignableTo<IHttpResponseCache<ApiResponse>>();
    }

    [Fact]
    public void GetRequiredService_ICacheKeyGenerator_ReturnsDefaultGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpClient<TestApiClient>();
        services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        ICacheKeyGenerator keyGenerator = serviceProvider.GetRequiredService<ICacheKeyGenerator>();

        // Assert
        keyGenerator.Should().NotBeNull();
        keyGenerator.Should().BeAssignableTo<ICacheKeyGenerator>();
    }

    [Fact]
    public void MultipleClients_CanBeRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpClient<TestApiClient>();
        services.AddHttpClient<AnotherApiClient>();

        // Act
        services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>();
        services.AddGenericHttpClientCaching<AnotherApiClient, AnotherResponse>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        CachedHttpClient<ApiResponse> cachedClient1 = serviceProvider.GetRequiredService<CachedHttpClient<ApiResponse>>();
        CachedHttpClient<AnotherResponse> cachedClient2 = serviceProvider.GetRequiredService<CachedHttpClient<AnotherResponse>>();

        cachedClient1.Should().NotBeNull();
        cachedClient2.Should().NotBeNull();
        cachedClient1.Should().NotBe(cachedClient2);
    }

    [Fact]
    public void Services_AreRegisteredWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpClient<TestApiClient>();
        services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Test that we get the same cache instance (singleton)
        IHttpResponseCache<ApiResponse> cache1 = serviceProvider.GetRequiredService<IHttpResponseCache<ApiResponse>>();
        IHttpResponseCache<ApiResponse> cache2 = serviceProvider.GetRequiredService<IHttpResponseCache<ApiResponse>>();
        cache1.Should().BeSameAs(cache2);

        // Test that we get the same key generator instance (singleton)
        ICacheKeyGenerator keyGen1 = serviceProvider.GetRequiredService<ICacheKeyGenerator>();
        ICacheKeyGenerator keyGen2 = serviceProvider.GetRequiredService<ICacheKeyGenerator>();
        keyGen1.Should().BeSameAs(keyGen2);

        // Test that we get different CachedHttpClient instances (scoped/transient)
        using IServiceScope scope1 = serviceProvider.CreateScope();
        using IServiceScope scope2 = serviceProvider.CreateScope();

        CachedHttpClient<ApiResponse> client1 = scope1.ServiceProvider.GetRequiredService<CachedHttpClient<ApiResponse>>();
        CachedHttpClient<ApiResponse> client2 = scope2.ServiceProvider.GetRequiredService<CachedHttpClient<ApiResponse>>();

        // CachedHttpClient should be scoped, so different scopes should get different instances
        client1.Should().NotBeSameAs(client2);
    }

    [Fact]
    public void AddHttpClientCaching_WithoutHttpClient_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        // Note: Not adding HttpClient

        // Act
        services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        Func<CachedHttpClient<ApiResponse>> action = () => serviceProvider.GetRequiredService<CachedHttpClient<ApiResponse>>();
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddHttpClientCaching_WithoutMemoryCache_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient<TestApiClient>();
        // Note: Not adding MemoryCache

        // Act & Assert
        Func<IServiceCollection> action = () => services.AddGenericHttpClientCaching<TestApiClient, ApiResponse>();
        action.Should().Throw<ArgumentException>()
            .WithMessage("*IMemoryCache is not registered*");
    }

    private class TestApiClient
    {
        public TestApiClient(NetHttpClient httpClient)
        {
            _ = httpClient; // Parameter required for DI but not used in tests
        }
    }

    private class AnotherApiClient
    {
        public AnotherApiClient(NetHttpClient httpClient)
        {
            _ = httpClient; // Parameter required for DI but not used in tests
        }
    }

    private class ApiResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class AnotherResponse
    {
        public string Value { get; set; } = string.Empty;
    }
}
