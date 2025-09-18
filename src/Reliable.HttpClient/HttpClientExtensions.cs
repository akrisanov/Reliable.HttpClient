using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;

namespace Reliable.HttpClient;

/// <summary>
/// Extension methods for registering HTTP clients with resilience policies
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Configures HTTP client with resilience policies
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="configureOptions">Action to configure resilience options</param>
    /// <returns>HTTP client builder for method chaining</returns>
    public static IHttpClientBuilder AddResilience(
        this IHttpClientBuilder builder,
        Action<HttpClientOptions>? configureOptions = null)
    {
        var options = new HttpClientOptions();
        configureOptions?.Invoke(options);

        // Validate configuration
        options.Validate();

        return builder
            .AddPolicyHandler(CreateRetryPolicy(options))
            .AddPolicyHandler(CreateCircuitBreakerPolicy(options));
    }

    /// <summary>
    /// Configures HTTP client with resilience policies using fluent builder
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="configureOptions">Action to configure resilience options using builder</param>
    /// <returns>HTTP client builder for method chaining</returns>
    public static IHttpClientBuilder AddResilience(
        this IHttpClientBuilder builder,
        Action<HttpClientOptionsBuilder> configureOptions)
    {
        var optionsBuilder = new HttpClientOptionsBuilder();
        configureOptions(optionsBuilder);
        HttpClientOptions options = optionsBuilder.Build();

        return builder
            .AddPolicyHandler(CreateRetryPolicy(options))
            .AddPolicyHandler(CreateCircuitBreakerPolicy(options));
    }

    /// <summary>
    /// Configures HTTP client with predefined preset
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="preset">Predefined configuration preset</param>
    /// <param name="customizeOptions">Optional action to customize preset options</param>
    /// <returns>HTTP client builder for method chaining</returns>
    public static IHttpClientBuilder AddResilience(
        this IHttpClientBuilder builder,
        HttpClientOptions preset,
        Action<HttpClientOptions>? customizeOptions = null)
    {
        customizeOptions?.Invoke(preset);
        preset.Validate();

        return builder
            .AddPolicyHandler(CreateRetryPolicy(preset))
            .AddPolicyHandler(CreateCircuitBreakerPolicy(preset));
    }

    /// <summary>
    /// Configures HTTP client with basic configuration and resilience policies
    /// </summary>
    /// <typeparam name="TClient">HTTP client type</typeparam>
    /// <typeparam name="TOptions">Configuration options type</typeparam>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="configureClient">Additional HTTP client configuration</param>
    /// <returns>HTTP client builder for method chaining</returns>
    public static IHttpClientBuilder ConfigureResilientClient<TClient, TOptions>(
        this IHttpClientBuilder builder,
        Action<TOptions, System.Net.Http.HttpClient>? configureClient = null)
        where TClient : class
        where TOptions : HttpClientOptions
    {
        return builder
            .ConfigureHttpClient((serviceProvider, client) => ConfigureHttpClientCore(serviceProvider, client, configureClient))
            .AddPolicyHandler((serviceProvider, request) => CreateRetryPolicy<TClient, TOptions>(serviceProvider))
            .AddPolicyHandler((serviceProvider, request) => CreateCircuitBreakerPolicy<TClient, TOptions>(serviceProvider));
    }

    /// <summary>
    /// Configures HTTP client with basic configuration and resilience policies (with named logger)
    /// </summary>
    /// <typeparam name="TOptions">Configuration options type</typeparam>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="loggerName">Logger name</param>
    /// <param name="configureClient">Additional HTTP client configuration</param>
    /// <returns>HTTP client builder for method chaining</returns>
    public static IHttpClientBuilder ConfigureResilientClient<TOptions>(
        this IHttpClientBuilder builder,
        string loggerName,
        Action<TOptions, System.Net.Http.HttpClient>? configureClient = null)
        where TOptions : HttpClientOptions
    {
        return builder
            .ConfigureHttpClient((serviceProvider, client) => ConfigureHttpClientCore(serviceProvider, client, configureClient))
            .AddPolicyHandler((serviceProvider, request) => CreateRetryPolicyNamed<TOptions>(serviceProvider, loggerName))
            .AddPolicyHandler((serviceProvider, request) => CreateCircuitBreakerPolicyNamed<TOptions>(serviceProvider, loggerName));
    }

    /// <summary>
    /// Basic HTTP client configuration based on options
    /// </summary>
    private static void ConfigureHttpClientCore<TOptions>(
        IServiceProvider serviceProvider,
        System.Net.Http.HttpClient client,
        Action<TOptions, System.Net.Http.HttpClient>? configureClient)
        where TOptions : HttpClientOptions
    {
        TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;

        // Validate configuration
        options.Validate();

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            client.BaseAddress = new Uri(options.BaseUrl);
        }

        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(options.UserAgent))
        {
            client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        }

        configureClient?.Invoke(options, client);
    }

    private static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy(HttpClientOptions options)
    {
        var random = new Random();

        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(msg => msg.StatusCode is
                >= HttpStatusCode.InternalServerError or
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: options.Retry.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromMilliseconds(
                        options.Retry.BaseDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1));

                    var finalDelay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, options.Retry.MaxDelay.TotalMilliseconds));

                    // Add jitter (random deviation) to avoid thundering herd
                    var jitterRange = finalDelay.TotalMilliseconds * options.Retry.JitterFactor;
                    var jitter = random.NextDouble() * jitterRange * 2 - jitterRange; // ±jitterRange
                    var finalDelayMs = Math.Max(0, finalDelay.TotalMilliseconds + jitter);

                    return TimeSpan.FromMilliseconds(finalDelayMs);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(HttpClientOptions options)
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(msg => msg.StatusCode is
                >= HttpStatusCode.InternalServerError or
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreaker.FailuresBeforeOpen,
                durationOfBreak: options.CircuitBreaker.OpenDuration);
    }

    private static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy<TClient, TOptions>(IServiceProvider serviceProvider)
        where TClient : class
        where TOptions : HttpClientOptions
    {
        TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;

        // Validate configuration
        options.Validate();

        ILogger<TClient> logger = serviceProvider.GetRequiredService<ILogger<TClient>>();

        return CreateRetryPolicyCore(options, logger, typeof(TClient).Name);
    }

    private static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicyNamed<TOptions>(
        IServiceProvider serviceProvider, string loggerName)
        where TOptions : HttpClientOptions
    {
        TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;

        // Validate configuration
        options.Validate();

        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger(loggerName);

        return CreateRetryPolicyCore(options, logger, loggerName);
    }

    private static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicyCore(
        HttpClientOptions options,
        ILogger logger,
        string clientName)
    {
        var random = new Random();

        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(msg => msg.StatusCode is
                >= HttpStatusCode.InternalServerError or
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: options.Retry.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromMilliseconds(
                        options.Retry.BaseDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1));

                    var finalDelay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, options.Retry.MaxDelay.TotalMilliseconds));

                    // Add jitter (random deviation) to avoid thundering herd
                    var jitterRange = finalDelay.TotalMilliseconds * options.Retry.JitterFactor;
                    var jitter = random.NextDouble() * jitterRange * 2 - jitterRange;
                    var finalDelayMs = Math.Max(0, finalDelay.TotalMilliseconds + jitter);

                    return TimeSpan.FromMilliseconds(finalDelayMs);
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var message = outcome.Exception is not null
                        ? $"Retry {retryCount} after exception: {outcome.Exception.Message}"
                        : $"Retry {retryCount} after HTTP {(int)outcome.Result.StatusCode}: {outcome.Result.ReasonPhrase}";

                    logger.LogWarning("{ClientName} HTTP retry. {Message}. Delay: {Delay}ms",
                        clientName, message, timespan.TotalMilliseconds);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy<TClient, TOptions>(
        IServiceProvider serviceProvider)
        where TClient : class
        where TOptions : HttpClientOptions
    {
        TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;

        // Validate configuration
        options.Validate();

        ILogger<TClient> logger = serviceProvider.GetRequiredService<ILogger<TClient>>();

        return CreateCircuitBreakerPolicyCore(options, logger, typeof(TClient).Name);
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicyNamed<TOptions>(
        IServiceProvider serviceProvider, string loggerName)
        where TOptions : HttpClientOptions
    {
        TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;

        // Validate configuration
        options.Validate();

        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger(loggerName);

        return CreateCircuitBreakerPolicyCore(options, logger, loggerName);
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicyCore(
        HttpClientOptions options,
        ILogger logger,
        string clientName)
    {
        if (!options.CircuitBreaker.Enabled)
        {
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(msg => msg.StatusCode is
                >= HttpStatusCode.InternalServerError or
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreaker.FailuresBeforeOpen,
                durationOfBreak: options.CircuitBreaker.OpenDuration,
                onBreak: (result, timespan) =>
                {
                    var errorMessage = result.Exception?.Message ??
                        (result.Result is not null ? $"HTTP {(int)result.Result.StatusCode}: {result.Result.ReasonPhrase}" : "Unknown error");

                    logger.LogError("{ClientName} HTTP circuit breaker opened for {Duration}ms due to: {Error}",
                        clientName, timespan.TotalMilliseconds, errorMessage);
                },
                onReset: () =>
                {
                    logger.LogInformation("{ClientName} HTTP circuit breaker reset", clientName);
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("{ClientName} HTTP circuit breaker in half-open state", clientName);
                });
    }

    // Universal response handler methods (RFC #1)

    /// <summary>
    /// Performs GET request with universal response handler
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="httpClient">HttpClient instance</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="responseHandler">Universal response handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    public static async Task<TResponse> GetAsync<TResponse>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        IHttpResponseHandler responseHandler,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs GET request with universal response handler
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="httpClient">HttpClient instance</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="responseHandler">Universal response handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    public static async Task<TResponse> GetAsync<TResponse>(
        this System.Net.Http.HttpClient httpClient,
        Uri requestUri,
        IHttpResponseHandler responseHandler,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs POST request with universal response handler
    /// </summary>
    /// <typeparam name="TRequest">Request content type</typeparam>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="httpClient">HttpClient instance</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="responseHandler">Universal response handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    public static async Task<TResponse> PostAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        TRequest content,
        IHttpResponseHandler responseHandler,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        return await responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs POST request with universal response handler
    /// </summary>
    /// <typeparam name="TRequest">Request content type</typeparam>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="httpClient">HttpClient instance</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="responseHandler">Universal response handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    public static async Task<TResponse> PostAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient httpClient,
        Uri requestUri,
        TRequest content,
        IHttpResponseHandler responseHandler,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        return await responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs PUT request with universal response handler
    /// </summary>
    /// <typeparam name="TRequest">Request content type</typeparam>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="httpClient">HttpClient instance</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="responseHandler">Universal response handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    public static async Task<TResponse> PutAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        TRequest content,
        IHttpResponseHandler responseHandler,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PutAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        return await responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs PUT request with universal response handler
    /// </summary>
    /// <typeparam name="TRequest">Request content type</typeparam>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="httpClient">HttpClient instance</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="responseHandler">Universal response handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    public static async Task<TResponse> PutAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient httpClient,
        Uri requestUri,
        TRequest content,
        IHttpResponseHandler responseHandler,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PutAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        return await responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs DELETE request with universal response handler
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="httpClient">HttpClient instance</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="responseHandler">Universal response handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    public static async Task<TResponse> DeleteAsync<TResponse>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        IHttpResponseHandler responseHandler,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs DELETE request with universal response handler
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="httpClient">HttpClient instance</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="responseHandler">Universal response handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    public static async Task<TResponse> DeleteAsync<TResponse>(
        this System.Net.Http.HttpClient httpClient,
        Uri requestUri,
        IHttpResponseHandler responseHandler,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }
}
