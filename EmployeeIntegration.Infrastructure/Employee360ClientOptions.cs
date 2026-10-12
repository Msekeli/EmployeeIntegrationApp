namespace EmployeeIntegration.Infrastructure;

/// <summary>
/// Configuration options for the downstream Employee 360 REST client.
/// </summary>
public sealed class Employee360ClientOptions
{
    public const string ConfigurationSection = "Employee360";

    /// <summary>
    /// Base URL of the downstream Employee 360 REST service.
    /// Default local development URL corresponds to Employee360.MockApi.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5223";

    /// <summary>
    /// Explicit request timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum number of total attempts (initial attempt + retries).
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay before retrying an eligible transient failure.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Maximum backoff delay cap.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}
