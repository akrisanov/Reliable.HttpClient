namespace Reliable.HttpClient;

/// <summary>
/// Builder for retry options
/// </summary>
public class RetryOptionsBuilder
{
    private readonly RetryOptions _options;

    internal RetryOptionsBuilder(RetryOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets maximum number of retries
    /// </summary>
    /// <param name="maxRetries">Maximum retry count</param>
    /// <returns>Builder for method chaining</returns>
    public RetryOptionsBuilder WithMaxRetries(int maxRetries)
    {
        _options.MaxRetries = maxRetries;
        return this;
    }

    /// <summary>
    /// Sets base delay between retries
    /// </summary>
    /// <param name="delay">Base delay</param>
    /// <returns>Builder for method chaining</returns>
    public RetryOptionsBuilder WithBaseDelay(TimeSpan delay)
    {
        _options.BaseDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets maximum delay between retries
    /// </summary>
    /// <param name="delay">Maximum delay</param>
    /// <returns>Builder for method chaining</returns>
    public RetryOptionsBuilder WithMaxDelay(TimeSpan delay)
    {
        _options.MaxDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets jitter factor for randomizing delays
    /// </summary>
    /// <param name="factor">Jitter factor (0.0 to 1.0)</param>
    /// <returns>Builder for method chaining</returns>
    public RetryOptionsBuilder WithJitter(double factor)
    {
        _options.JitterFactor = factor;
        return this;
    }

    /// <summary>
    /// Disables jitter (sets factor to 0)
    /// </summary>
    /// <returns>Builder for method chaining</returns>
    public RetryOptionsBuilder WithoutJitter()
    {
        _options.JitterFactor = 0.0;
        return this;
    }
}
