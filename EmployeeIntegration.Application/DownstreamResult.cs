namespace EmployeeIntegration.Application;

public enum DownstreamStatus
{
    /// <summary>
    /// Downstream update was successfully accepted.
    /// </summary>
    Success,

    /// <summary>
    /// Downstream rejected the update permanently (e.g. 4xx validation or client error).
    /// </summary>
    PermanentFailure,

    /// <summary>
    /// Outcome is ambiguous or uncertain (e.g. timeout, network connection drop, 504 Gateway Timeout).
    /// </summary>
    Uncertain,

    /// <summary>
    /// Transient failure retries were exhausted without confirmation.
    /// </summary>
    TransientFailure
}

/// <summary>
/// Result of sending an update to the downstream Employee 360 API.
/// </summary>
public sealed record DownstreamResult
{
    public required DownstreamStatus Status { get; init; }
    public int StatusCode { get; init; }
    public string? Message { get; init; }
    public int AttemptCount { get; init; } = 1;

    public bool IsSuccess => Status == DownstreamStatus.Success;

    public static DownstreamResult Succeeded(int statusCode = 200, string? message = null, int attempts = 1) =>
        new()
        {
            Status = DownstreamStatus.Success,
            StatusCode = statusCode,
            Message = message ?? "Downstream update succeeded.",
            AttemptCount = attempts
        };

    public static DownstreamResult Permanent(int statusCode, string message, int attempts = 1) =>
        new()
        {
            Status = DownstreamStatus.PermanentFailure,
            StatusCode = statusCode,
            Message = message,
            AttemptCount = attempts
        };

    public static DownstreamResult Uncertain(string message, int attempts = 1, int statusCode = 0) =>
        new()
        {
            Status = DownstreamStatus.Uncertain,
            StatusCode = statusCode,
            Message = message,
            AttemptCount = attempts
        };

    public static DownstreamResult Transient(int statusCode, string message, int attempts = 1) =>
        new()
        {
            Status = DownstreamStatus.TransientFailure,
            StatusCode = statusCode,
            Message = message,
            AttemptCount = attempts
        };
}
