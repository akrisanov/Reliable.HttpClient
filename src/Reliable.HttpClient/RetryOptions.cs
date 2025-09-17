namespace Reliable.HttpClient;

/// <summary>
/// Retry policy configuration options
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts on error
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay before retry (exponential backoff: 1s, 2s, 4s, 8s...)
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(1_000);

    /// <summary>
    /// Maximum delay before retry
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMilliseconds(30_000);

    /// <summary>
    /// Jitter factor for randomizing retry delays (0.0 to 1.0)
    /// </summary>
    public double JitterFactor { get; set; } = 0.25;

    /// <summary>
    /// Validates the retry configuration options
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
    public void Validate()
    {
#pragma warning disable MA0015 // Specify the parameter name in ArgumentException
        if (MaxRetries < 0)
            throw new ArgumentException("MaxRetries cannot be negative", nameof(MaxRetries));

        if (BaseDelay <= TimeSpan.Zero)
            throw new ArgumentException("BaseDelay must be greater than zero", nameof(BaseDelay));

        if (MaxDelay <= TimeSpan.Zero)
            throw new ArgumentException("MaxDelay must be greater than zero", nameof(MaxDelay));

        if (BaseDelay > MaxDelay)
            throw new ArgumentException("BaseDelay cannot be greater than MaxDelay", nameof(BaseDelay));

        if (JitterFactor < 0.0 || JitterFactor > 1.0)
            throw new ArgumentException("JitterFactor must be between 0.0 and 1.0", nameof(JitterFactor));
#pragma warning restore MA0015 // Specify the parameter name in ArgumentException
    }
}
