using EmployeeIntegration.Application;
using EmployeeIntegration.Infrastructure;

namespace EmployeeIntegration.Tests.Infrastructure;

public class InMemoryEventProcessingStoreTests
{
    private readonly InMemoryEventProcessingStore _store = new();

    [Fact]
    public void TryClaim_NewEvent_GrantsClaimAndSetsReceivedState()
    {
        var result = _store.TryClaim("evt-new-1");

        Assert.True(result.IsGranted);
        Assert.Equal(ClaimStatus.Granted, result.Status);
        Assert.NotNull(result.ExistingRecord);
        Assert.Equal(ProcessingState.Received, result.ExistingRecord.State);

        var record = _store.Get("evt-new-1");
        Assert.NotNull(record);
        Assert.Equal(ProcessingState.Received, record.State);
    }

    [Fact]
    public void TryClaim_CompletedEvent_DeniesWithAlreadyCompleted()
    {
        _store.RecordState("evt-done-1", ProcessingState.Completed);

        var result = _store.TryClaim("evt-done-1");

        Assert.False(result.IsGranted);
        Assert.Equal(ClaimStatus.AlreadyCompleted, result.Status);
    }

    [Fact]
    public void TryClaim_ReceivedEvent_DeniesWithInProgress()
    {
        _store.TryClaim("evt-progress-1");

        var secondClaim = _store.TryClaim("evt-progress-1");

        Assert.False(secondClaim.IsGranted);
        Assert.Equal(ClaimStatus.InProgress, secondClaim.Status);
    }

    [Fact]
    public void TryClaim_UncertainEvent_DeniesWithUncertain()
    {
        _store.RecordState("evt-uncertain-1", ProcessingState.Uncertain, "Ambiguous timeout");

        var result = _store.TryClaim("evt-uncertain-1");

        Assert.False(result.IsGranted);
        Assert.Equal(ClaimStatus.Uncertain, result.Status);
    }

    [Fact]
    public void TryClaim_FailedEvent_DeniesWithFailed()
    {
        _store.RecordState("evt-failed-1", ProcessingState.Failed, "Validation or terminal error");

        var result = _store.TryClaim("evt-failed-1");

        Assert.False(result.IsGranted);
        Assert.Equal(ClaimStatus.Failed, result.Status);
    }

    [Fact]
    public void TryClaim_SequenceNumbers_AcceptsHigherAndRecordsLowerAsStale()
    {
        Assert.Equal(ClaimStatus.Granted, _store.TryClaim("evt-v5", "EMP-V", 5).Status);
        var stale = _store.TryClaim("evt-v4", "EMP-V", 4);

        Assert.Equal(ClaimStatus.Stale, stale.Status);
        Assert.Equal(ProcessingState.Stale, _store.Get("evt-v4")?.State);
        Assert.Equal(ClaimStatus.InProgress, _store.TryClaim("evt-v5", "EMP-V", 5).Status);
    }

    [Fact]
    public void GetAll_AndReset_ExposeFailedRecoveryRecords()
    {
        _store.RecordState("evt-dead", ProcessingState.Failed, "Permanent downstream failure.");

        var record = Assert.Single(_store.GetAll());
        Assert.Equal("evt-dead", record.EventId);
        Assert.Equal("Permanent downstream failure.", record.Details);
        Assert.True(_store.TryReset("evt-dead"));
        Assert.Empty(_store.GetAll());
    }

    [Fact]
    public void TryReset_FailedVersion_AllowsSameVersionReplay()
    {
        _store.TryClaim("evt-replay-version", "EMP-R", 12);
        _store.RecordState("evt-replay-version", ProcessingState.Failed, "Retry after correction.");

        Assert.True(_store.TryReset("evt-replay-version"));
        Assert.Equal(ClaimStatus.Granted, _store.TryClaim("evt-replay-version", "EMP-R", 12).Status);
    }

    [Fact]
    public void TryClaim_EventIdsAreComparedOrdinally()
    {
        _store.RecordState("evt-case-sensitive", ProcessingState.Completed);

        var result = _store.TryClaim("EVT-CASE-SENSITIVE");

        Assert.True(result.IsGranted);
    }

    [Fact]
    public async Task TryClaim_ConcurrentClaims_OnlyOneIsGranted()
    {
        var eventId = "evt-concurrent-claim-1";
        const int concurrentTasks = 30;
        var startGate = new TaskCompletionSource();

        var tasks = Enumerable.Range(0, concurrentTasks).Select(async _ =>
        {
            await startGate.Task;
            return _store.TryClaim(eventId);
        }).ToArray();

        startGate.SetResult();
        var results = await Task.WhenAll(tasks);

        var grantedCount = results.Count(r => r.IsGranted);
        var deniedCount = results.Count(r => !r.IsGranted);

        Assert.Equal(1, grantedCount);
        Assert.Equal(concurrentTasks - 1, deniedCount);
    }

    [Fact]
    public void RecordState_UpdatesStateAndAttemptCount()
    {
        _store.TryClaim("evt-state-1");
        _store.RecordState("evt-state-1", ProcessingState.Completed, "All good");

        var record = _store.Get("evt-state-1");
        Assert.NotNull(record);
        Assert.Equal(ProcessingState.Completed, record.State);
        Assert.Equal(1, record.AttemptCount);
        Assert.Equal("All good", record.Details);
    }

    [Fact]
    public void TryReset_FailedEvent_RemovesExistingEvent_AllowsSubsequentClaim()
    {
        _store.RecordState("evt-reset-test", ProcessingState.Failed);

        var resetResult = _store.TryReset("evt-reset-test");
        Assert.True(resetResult);
        Assert.Null(_store.Get("evt-reset-test"));

        var reClaim = _store.TryClaim("evt-reset-test");
        Assert.True(reClaim.IsGranted);
    }

    [Theory]
    [InlineData(ProcessingState.Received)]
    [InlineData(ProcessingState.Completed)]
    [InlineData(ProcessingState.Uncertain)]
    public void TryReset_DoesNotRemoveNonFailedEvent(ProcessingState state)
    {
        _store.RecordState($"evt-no-reset-{state}", state);

        Assert.False(_store.TryReset($"evt-no-reset-{state}"));
        Assert.Equal(state, _store.Get($"evt-no-reset-{state}")?.State);
    }

    [Fact]
    public void RecordState_DoesNotAllowTerminalStateToChange()
    {
        _store.RecordState("evt-terminal", ProcessingState.Completed);

        Assert.Throws<InvalidOperationException>(() =>
            _store.RecordState("evt-terminal", ProcessingState.Uncertain));
    }
}
