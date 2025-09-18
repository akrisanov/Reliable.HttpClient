using Microsoft.Extensions.DependencyInjection;

namespace Reliable.HttpClient;

/// <summary>
/// Extension methods for registering HTTP client adapters in DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds HttpClientAdapter as implementation of IHttpClientAdapter
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddHttpClientAdapter(this IServiceCollection services)
    {
        services.AddScoped<IHttpClientAdapter, HttpClientAdapter>();
        return services;
    }

    /// <summary>
    /// Adds HttpClient and related services for non-cached scenarios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureHttpClient">Optional HttpClient configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddHttpClientWithAdapter(
        this IServiceCollection services,
        Action<System.Net.Http.HttpClient>? configureHttpClient = null)
    {
        services.AddHttpClient();

        if (configureHttpClient is not null)
        {
            services.AddHttpClient<HttpClientAdapter>(configureHttpClient);
        }

        services.AddSingleton<IHttpResponseHandler, DefaultHttpResponseHandler>();
        services.AddScoped<IHttpClientAdapter, HttpClientAdapter>();

        return services;
    }
}
