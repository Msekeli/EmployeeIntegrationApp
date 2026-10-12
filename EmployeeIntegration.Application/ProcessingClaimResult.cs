namespace EmployeeIntegration.Application;

public enum ClaimStatus
{
    /// <summary>
    /// The event is new; processing rights have been granted to this caller.
    /// </summary>
    Granted,

    /// <summary>
    /// The event was previously processed and completed successfully.
    /// </summary>
    AlreadyCompleted,

    /// <summary>
    /// Another concurrent execution is currently processing this event.
    /// </summary>
    InProgress,

    /// <summary>
    /// An earlier attempt ended in an uncertain state and cannot be safely completed or re-executed without resolution.
    /// </summary>
    Uncertain,

    /// <summary>
    /// An earlier attempt permanently failed.
    /// </summary>
    Failed
}

/// <summary>
/// Result of attempting to claim processing rights for an event.
/// </summary>
public sealed record ProcessingClaimResult
{
    public required ClaimStatus Status { get; init; }
    public EventProcessingRecord? ExistingRecord { get; init; }

    public bool IsGranted => Status == ClaimStatus.Granted;

    public static ProcessingClaimResult Granted(EventProcessingRecord record) =>
        new() { Status = ClaimStatus.Granted, ExistingRecord = record };

    public static ProcessingClaimResult AlreadyCompleted(EventProcessingRecord record) =>
        new() { Status = ClaimStatus.AlreadyCompleted, ExistingRecord = record };

    public static ProcessingClaimResult InProgress(EventProcessingRecord record) =>
        new() { Status = ClaimStatus.InProgress, ExistingRecord = record };

    public static ProcessingClaimResult Uncertain(EventProcessingRecord record) =>
        new() { Status = ClaimStatus.Uncertain, ExistingRecord = record };

    public static ProcessingClaimResult Failed(EventProcessingRecord record) =>
        new() { Status = ClaimStatus.Failed, ExistingRecord = record };
}
