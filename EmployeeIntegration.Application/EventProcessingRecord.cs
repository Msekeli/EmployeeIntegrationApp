namespace EmployeeIntegration.Application;

/// <summary>
/// Retains the state and audit history for a processed event.
/// </summary>
public sealed record EventProcessingRecord
{
    public required string EventId { get; init; }
    public required ProcessingState State { get; init; }
    public DateTimeOffset FirstSeenAtUtc { get; init; }
    public DateTimeOffset LastUpdatedAtUtc { get; init; }
    public int AttemptCount { get; init; } = 1;
    public string? Details { get; init; }
}
