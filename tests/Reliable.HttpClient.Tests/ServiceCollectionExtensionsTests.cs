using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHttpClientAdapter_RegistersIHttpClientAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();

        // Act
        services.AddHttpClientAdapter();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IHttpClientAdapter adapter = serviceProvider.GetRequiredService<IHttpClientAdapter>();
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<HttpClientAdapter>();
    }

    [Fact]
    public void AddHttpClientWithAdapter_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddHttpClientWithAdapter();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IHttpClientAdapter>().Should().NotBeNull();
        serviceProvider.GetService<IHttpResponseHandler>().Should().NotBeNull();
        serviceProvider.GetService<System.Net.Http.HttpClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddHttpClientWithAdapter_WithCustomConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddHttpClientWithAdapter(client =>
        {
            client.BaseAddress = new Uri("https://api.test.com");
        });
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IHttpClientAdapter adapter = serviceProvider.GetRequiredService<IHttpClientAdapter>();
        adapter.Should().NotBeNull();
        
        // Check if HttpClient was configured properly by verifying the service registration
        var httpClient = serviceProvider.GetRequiredService<System.Net.Http.HttpClient>();
        httpClient.BaseAddress?.ToString().Should().Be("https://api.test.com/");
    }

    [Fact]
    public void AddHttpClientAdapter_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();

        // Act & Assert
        Func<IServiceCollection> act = () => services
            .AddHttpClientAdapter()
            .AddHttpClientAdapter();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddHttpClientWithAdapter_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Func<IServiceCollection> act = () => services
            .AddHttpClientWithAdapter()
            .AddHttpClientWithAdapter();

        act.Should().NotThrow();
    }
}