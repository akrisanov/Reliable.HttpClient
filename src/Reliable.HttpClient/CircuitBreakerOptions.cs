namespace Reliable.HttpClient;

/// <summary>
/// Circuit breaker policy configuration options
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Enable Circuit Breaker policy
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Number of failures before opening Circuit Breaker
    /// </summary>
    public int FailuresBeforeOpen { get; set; } = 5;

    /// <summary>
    /// Circuit Breaker open duration
    /// </summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromMilliseconds(60_000);

    /// <summary>
    /// Validates the circuit breaker configuration options
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
    public void Validate()
    {
#pragma warning disable MA0015 // Specify the parameter name in ArgumentException
        if (FailuresBeforeOpen <= 0)
            throw new ArgumentException("FailuresBeforeOpen must be greater than 0", nameof(FailuresBeforeOpen));

        if (OpenDuration <= TimeSpan.Zero)
            throw new ArgumentException("OpenDuration must be greater than zero", nameof(OpenDuration));
#pragma warning restore MA0015 // Specify the parameter name in ArgumentException
    }
}
