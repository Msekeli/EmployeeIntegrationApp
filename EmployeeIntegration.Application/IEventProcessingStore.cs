namespace EmployeeIntegration.Application;

/// <summary>
/// Contract for storing and managing event processing lifecycle state and idempotency.
/// </summary>
public interface IEventProcessingStore
{
    /// <summary>
    /// Atomically claims execution rights for the given event ID.
    /// If the event is new, transitions to Received and grants the claim.
    /// If the event already exists, returns the corresponding claim status without duplicate grant.
    /// </summary>
    ProcessingClaimResult TryClaim(string eventId);

    /// <summary>
    /// Retrieves the current processing record for an event ID, or null if not found.
    /// </summary>
    EventProcessingRecord? Get(string eventId);

    /// <summary>
    /// Records or updates the state and diagnostic details for an event.
    /// </summary>
    void RecordState(string eventId, ProcessingState state, string? details = null);

    /// <summary>
    /// Clears or resets the processing record for an event to support recovery or replay.
    /// Returns true if an existing record was removed.
    /// </summary>
    bool TryReset(string eventId);
}
