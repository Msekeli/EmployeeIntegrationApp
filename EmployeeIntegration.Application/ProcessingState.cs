namespace EmployeeIntegration.Application;

/// <summary>
/// Represents the processing lifecycle state of an employee event.
/// </summary>
public enum ProcessingState
{
    /// <summary>
    /// The event has been received and processing is currently in progress.
    /// </summary>
    Received,

    /// <summary>
    /// The event has been fully and successfully processed.
    /// </summary>
    Completed,

    /// <summary>
    /// The event processing permanently failed (e.g. unrecoverable error).
    /// </summary>
    Failed,

    /// <summary>
    /// The outcome of the event processing is ambiguous or uncertain
    /// (e.g. downstream timeout or error where success/failure cannot be proven).
    /// </summary>
    Uncertain
}
