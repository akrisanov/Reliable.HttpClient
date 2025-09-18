namespace Reliable.HttpClient;

/// <summary>
/// Builder for circuit breaker options
/// </summary>
public class CircuitBreakerOptionsBuilder
{
    private readonly CircuitBreakerOptions _options;

    internal CircuitBreakerOptionsBuilder(CircuitBreakerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets failure threshold before opening circuit
    /// </summary>
    /// <param name="failures">Number of failures</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerOptionsBuilder WithFailureThreshold(int failures)
    {
        _options.FailuresBeforeOpen = failures;
        return this;
    }

    /// <summary>
    /// Sets duration to keep circuit open
    /// </summary>
    /// <param name="duration">Open duration</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerOptionsBuilder WithOpenDuration(TimeSpan duration)
    {
        _options.OpenDuration = duration;
        return this;
    }
}
