using System.Collections.Concurrent;
using EmployeeIntegration.Application;

namespace EmployeeIntegration.Infrastructure;

/// <summary>
/// Thread-safe in-memory implementation of IEventProcessingStore for tracking
/// event processing lifecycle states, duplicate detection, and concurrency control.
/// </summary>
public sealed class InMemoryEventProcessingStore : IEventProcessingStore
{
    private readonly ConcurrentDictionary<string, EventProcessingRecord> _records =
        new(StringComparer.Ordinal);

    public ProcessingClaimResult TryClaim(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        var now = DateTimeOffset.UtcNow;
        var initialRecord = new EventProcessingRecord
        {
            EventId = eventId,
            State = ProcessingState.Received,
            FirstSeenAtUtc = now,
            LastUpdatedAtUtc = now,
            AttemptCount = 1,
            Details = "Initial event intake received."
        };

        // TryAdd is atomic: only one caller succeeds when multiple concurrent requests attempt the same EventId
        if (_records.TryAdd(eventId, initialRecord))
        {
            return ProcessingClaimResult.Granted(initialRecord);
        }

        // Event already exists; inspect current lifecycle state
        if (_records.TryGetValue(eventId, out var existing))
        {
            return existing.State switch
            {
                ProcessingState.Completed => ProcessingClaimResult.AlreadyCompleted(existing),
                ProcessingState.Received => ProcessingClaimResult.InProgress(existing),
                ProcessingState.Uncertain => ProcessingClaimResult.Uncertain(existing),
                ProcessingState.Failed => ProcessingClaimResult.Failed(existing),
                _ => ProcessingClaimResult.InProgress(existing)
            };
        }

        // Fallback in the rare race where an entry was removed between TryAdd and TryGetValue
        return _records.TryAdd(eventId, initialRecord)
            ? ProcessingClaimResult.Granted(initialRecord)
            : ProcessingClaimResult.InProgress(initialRecord);
    }

    public EventProcessingRecord? Get(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        return _records.TryGetValue(eventId, out var record) ? record : null;
    }

    public void RecordState(string eventId, ProcessingState state, string? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        var now = DateTimeOffset.UtcNow;
        _records.AddOrUpdate(
            eventId,
            id => new EventProcessingRecord
            {
                EventId = id,
                State = state,
                FirstSeenAtUtc = now,
                LastUpdatedAtUtc = now,
                AttemptCount = 1,
                Details = details
            },
            (id, existing) => existing with
            {
                State = ValidateTransition(existing.State, state),
                LastUpdatedAtUtc = now,
                AttemptCount = existing.AttemptCount,
                Details = details ?? existing.Details
            });
    }

    public bool TryReset(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        return _records.TryGetValue(eventId, out var existing)
            && existing.State == ProcessingState.Failed
            && _records.TryRemove(new KeyValuePair<string, EventProcessingRecord>(eventId, existing));
    }

    private static ProcessingState ValidateTransition(
        ProcessingState current,
        ProcessingState requested)
    {
        if (current == requested)
        {
            return requested;
        }

        var isValid = current == ProcessingState.Received &&
            requested is ProcessingState.Completed
                or ProcessingState.Failed
                or ProcessingState.Uncertain;

        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Invalid event processing state transition from {current} to {requested}.");
        }

        return requested;
    }
}
