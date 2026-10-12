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
    private readonly ConcurrentDictionary<string, long> _latestSequenceByEmployee = new(StringComparer.Ordinal);
    private readonly object _claimLock = new();

    public ProcessingClaimResult TryClaim(string eventId)
        => TryClaim(eventId, string.Empty, null);

    public ProcessingClaimResult TryClaim(string eventId, string employeeId, long? sequenceNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        lock (_claimLock)
        {
            if (_records.TryGetValue(eventId, out var known))
            {
                return known.State switch
                {
                    ProcessingState.Completed => ProcessingClaimResult.AlreadyCompleted(known),
                    ProcessingState.Received => ProcessingClaimResult.InProgress(known),
                    ProcessingState.Uncertain => ProcessingClaimResult.Uncertain(known),
                    ProcessingState.Failed => ProcessingClaimResult.Failed(known),
                    ProcessingState.Stale => ProcessingClaimResult.Stale(known),
                    _ => ProcessingClaimResult.InProgress(known)
                };
            }

            if (sequenceNumber is not null && !string.IsNullOrWhiteSpace(employeeId) &&
                _latestSequenceByEmployee.TryGetValue(employeeId, out var latest) && sequenceNumber <= latest)
            {
                var stale = NewRecord(eventId, ProcessingState.Stale, employeeId, sequenceNumber,
                    $"Stale event suppressed: sequence {sequenceNumber} is not newer than the latest observed sequence {latest}.");
                _records[eventId] = stale;
                return ProcessingClaimResult.Stale(stale);
            }

            if (sequenceNumber is not null && !string.IsNullOrWhiteSpace(employeeId))
                _latestSequenceByEmployee[employeeId] = sequenceNumber.Value;

            var record = NewRecord(eventId, ProcessingState.Received, employeeId, sequenceNumber, "Initial event intake received.");
            _records[eventId] = record;
            return ProcessingClaimResult.Granted(record);
        }
    }

    private static EventProcessingRecord NewRecord(string id, ProcessingState state, string employeeId, long? sequence, string details)
    {
        var now = DateTimeOffset.UtcNow;
        return new EventProcessingRecord { EventId = id, State = state, FirstSeenAtUtc = now, LastUpdatedAtUtc = now,
            AttemptCount = 1, Details = details, EmployeeId = string.IsNullOrEmpty(employeeId) ? null : employeeId, SequenceNumber = sequence };
    }

    public EventProcessingRecord? Get(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        return _records.TryGetValue(eventId, out var record) ? record : null;
    }

    public IReadOnlyList<EventProcessingRecord> GetAll() => _records.Values.OrderByDescending(x => x.LastUpdatedAtUtc).ToArray();

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

        lock (_claimLock)
        {
            if (!_records.TryGetValue(eventId, out var existing) || existing.State != ProcessingState.Failed ||
                !_records.TryRemove(new KeyValuePair<string, EventProcessingRecord>(eventId, existing)))
                return false;

            if (existing.EmployeeId is not null)
            {
                var newestRemaining = _records.Values
                    .Where(record => record.EmployeeId == existing.EmployeeId && record.SequenceNumber is not null && record.State != ProcessingState.Stale)
                    .Select(record => record.SequenceNumber!.Value)
                    .DefaultIfEmpty()
                    .Max();
                if (_records.Values.Any(record => record.EmployeeId == existing.EmployeeId && record.SequenceNumber is not null && record.State != ProcessingState.Stale))
                    _latestSequenceByEmployee[existing.EmployeeId] = newestRemaining;
                else
                    _latestSequenceByEmployee.TryRemove(existing.EmployeeId, out _);
            }

            return true;
        }
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
                or ProcessingState.Uncertain
                or ProcessingState.Stale;

        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Invalid event processing state transition from {current} to {requested}.");
        }

        return requested;
    }
}
