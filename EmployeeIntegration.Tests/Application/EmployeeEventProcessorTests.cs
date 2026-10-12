
using EmployeeIntegration.Application;
using EmployeeIntegration.Domain;
using EmployeeIntegration.Infrastructure;

namespace EmployeeIntegration.Tests.Application;

public class EmployeeEventProcessorTests
{
    private readonly IEventProcessingStore _store = new InMemoryEventProcessingStore();
    private readonly EmployeeEventProcessor _processor;

    public EmployeeEventProcessorTests()
    {
        _processor = new EmployeeEventProcessor(_store);
    }

    [Fact]
    public void ProcessJson_ValidJson_ReturnsSuccessWithTransformedRequest()
    {
        var json = """
        {
            "eventId": "evt-501",
            "eventType": "EmployeeUpdated",
            "timestamp": "2026-10-12T14:30:00Z",
            "sequenceNumber": 1,
            "data": {
                "employeeId": "EMP-501",
                "firstName": "Grace",
                "lastName": "Hopper",
                "email": "grace.hopper@navy.mil",
                "department": "Systems",
                "jobTitle": "Admiral",
                "officeCode": "CPT"
            }
        }
        """;

        var result = _processor.ProcessJson(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Success, result.Status);
        Assert.NotNull(result.TransformedRequest);
        Assert.Equal("EMP-501", result.TransformedRequest.EmployeeId);
        Assert.Equal("Grace Hopper", result.TransformedRequest.FullName);
        Assert.Equal("Cape Town", result.TransformedRequest.OfficeLocation);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ProcessJson_ValidFlatJson_ReturnsSuccessWithTransformedRequest()
    {
        var json = """
        {
            "eventId": "evt-502",
            "eventType": "EmployeeUpdated",
            "timestamp": "2026-10-12T14:30:00Z",
            "employeeId": "EMP-502",
            "firstName": "Ada",
            "lastName": "Lovelace",
            "email": "ada.lovelace@computing.org",
            "department": "Mathematics",
            "jobTitle": "Visionary",
            "officeCode": "XYZ"
        }
        """;

        var result = _processor.ProcessJson(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Success, result.Status);
        Assert.NotNull(result.TransformedRequest);
        Assert.Equal("Ada Lovelace", result.TransformedRequest.FullName);
        Assert.Equal("XYZ", result.TransformedRequest.OfficeLocation);
    }

    [Fact]
    public void ProcessJson_AssessmentExample_ReturnsSuccessWithCorrectMappings()
    {
        var json = """
        {
            "eventId": "EVT-10452-001",
            "eventType": "EmployeeUpdated",
            "employeeId": "EMP10452",
            "firstName": "Amira",
            "lastName": "Khan",
            "department": "Data & AI",
            "location": "Cape Town",
            "effectiveDate": "2026-10-01T08:30:00Z"
        }
        """;

        var result = _processor.ProcessJson(json);

        Assert.True(
            result.IsSuccess,
            string.Join("; ", result.Errors.Select(
                error => $"{error.Field}: {error.Message}")));

        Assert.Equal(EventProcessingStatus.Success, result.Status);
        Assert.NotNull(result.TransformedRequest);
        Assert.Equal("EMP10452", result.TransformedRequest.EmployeeId);
        Assert.Equal("Amira", result.TransformedRequest.FirstName);
        Assert.Equal("Khan", result.TransformedRequest.LastName);
        Assert.Equal("Amira Khan", result.TransformedRequest.FullName);
        Assert.Equal("Data & AI", result.TransformedRequest.Department);
        Assert.Equal("Cape Town", result.TransformedRequest.OfficeLocation);
        Assert.Null(result.TransformedRequest.Email);
        Assert.Null(result.TransformedRequest.JobTitle);
        Assert.Equal(
            DateTimeOffset.Parse("2026-10-01T08:30:00Z"),
            result.TransformedRequest.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProcessJson_EmptyOrWhitespacePayload_ReturnsMalformedSafely(
        string? payload)
    {
        var result = _processor.ProcessJson(payload);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Malformed, result.Status);
        Assert.Null(result.TransformedRequest);
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"eventId\": ")]
    [InlineData("not a json string")]
    [InlineData("<xml>invalid</xml>")]
    public void ProcessJson_MalformedSyntax_ReturnsMalformedSafely(
        string malformedJson)
    {
        var result = _processor.ProcessJson(malformedJson);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Malformed, result.Status);
        Assert.Null(result.TransformedRequest);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, error => error.Field == "Payload");
    }

    [Fact]
    public void ProcessJson_MissingRequiredFields_ReturnsInvalidWithValidationErrors()
    {
        var invalidJson = """
        {
            "eventId": "evt-503",
            "eventType": "EmployeeUpdated",
            "timestamp": "2026-10-12T14:30:00Z",
            "data": {
                "firstName": "Alan",
                "email": "invalid-email"
            }
        }
        """;

        var result = _processor.ProcessJson(invalidJson);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Invalid, result.Status);
        Assert.Null(result.TransformedRequest);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, error => error.Field == "EmployeeId");
        Assert.Contains(result.Errors, error => error.Field == "LastName");
        Assert.Contains(result.Errors, error => error.Field == "Email");
        Assert.Contains(result.Errors, error => error.Field == "Department");
        Assert.DoesNotContain(result.Errors, error => error.Field == "JobTitle");
    }

    [Fact]
    public void Process_DirectValidEvent_ReturnsSuccess()
    {
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-777",
            EventType = "EmployeeUpdated",
            Timestamp = DateTimeOffset.UtcNow,
            EmployeeId = "EMP-777",
            FirstName = "Margaret",
            LastName = "Hamilton",
            Email = "margaret.hamilton@apollo.nasa.gov",
            Department = "Software",
            JobTitle = "Director",
            OfficeCode = "CPT"
        };

        var result = _processor.Process(@event);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.TransformedRequest);
        Assert.Equal("Cape Town", result.TransformedRequest.OfficeLocation);
    }

    [Fact]
    public void Process_DirectInvalidEvent_ReturnsInvalid()
    {
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-888"
        };

        var result = _processor.Process(@event);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Invalid, result.Status);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Process_FirstDelivery_SucceedsAndRecordsCompletedState()
    {
        var eventId = "evt-dedup-001";
        var @event = CreateValidSampleEvent(eventId, "EMP-100");

        var result = _processor.Process(@event);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsDuplicate);
        Assert.Equal(EventProcessingStatus.Success, result.Status);
        Assert.Equal(eventId, result.EventId);
        Assert.NotNull(result.TransformedRequest);

        var stored = _store.Get(eventId);
        Assert.NotNull(stored);
        Assert.Equal(ProcessingState.Completed, stored.State);
        Assert.Equal(1, stored.AttemptCount);
    }

    [Fact]
    public void Process_RepeatedDelivery_ReturnsExplicitDuplicateResultWithoutReexecuting()
    {
        var eventId = "evt-dedup-002";
        var @event = CreateValidSampleEvent(eventId, "EMP-101");

        var firstResult = _processor.Process(@event);
        Assert.True(firstResult.IsSuccess);
        Assert.Equal(EventProcessingStatus.Success, firstResult.Status);

        // Second delivery of the identical event
        var secondResult = _processor.Process(@event);

        Assert.False(secondResult.IsSuccess);
        Assert.True(secondResult.IsDuplicate);
        Assert.Equal(EventProcessingStatus.Duplicate, secondResult.Status);
        Assert.Equal(eventId, secondResult.EventId);
        Assert.Null(secondResult.TransformedRequest);
        Assert.Contains(eventId, secondResult.Message);

        var stored = _store.Get(eventId);
        Assert.NotNull(stored);
        Assert.Equal(ProcessingState.Completed, stored.State);
    }

    [Fact]
    public void Process_IndependentEventIds_ProcessIndependently()
    {
        var event1 = CreateValidSampleEvent("evt-indep-1", "EMP-201");
        var event2 = CreateValidSampleEvent("evt-indep-2", "EMP-202");

        var result1 = _processor.Process(event1);
        var result2 = _processor.Process(event2);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(ProcessingState.Completed, _store.Get("evt-indep-1")?.State);
        Assert.Equal(ProcessingState.Completed, _store.Get("evt-indep-2")?.State);
    }

    [Fact]
    public async Task Process_ConcurrentDeliveries_CannotBothClaimFirstProcessing()
    {
        var eventId = "evt-concurrent-001";
        var json = $$"""
        {
            "eventId": "{{eventId}}",
            "eventType": "EmployeeUpdated",
            "timestamp": "2026-10-12T14:30:00Z",
            "employeeId": "EMP-999",
            "firstName": "Ada",
            "lastName": "Lovelace",
            "email": "ada@computing.org",
            "department": "Engineering",
            "location": "Cape Town"
        }
        """;

        const int concurrencyLevel = 20;
        var startSignal = new TaskCompletionSource();

        var tasks = Enumerable.Range(0, concurrencyLevel).Select(async _ =>
        {
            await startSignal.Task;
            return _processor.ProcessJson(json);
        }).ToArray();

        // Release all threads simultaneously
        startSignal.SetResult();
        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.Status == EventProcessingStatus.Success);
        var duplicateOrConflictCount = results.Count(r =>
            r.Status == EventProcessingStatus.Duplicate || r.Status == EventProcessingStatus.Conflict);

        // Exactly ONE concurrent execution must claim first processing
        Assert.Equal(1, successCount);
        Assert.Equal(concurrencyLevel - 1, duplicateOrConflictCount);

        var stored = _store.Get(eventId);
        Assert.NotNull(stored);
        Assert.Equal(ProcessingState.Completed, stored.State);
    }

    [Fact]
    public void Process_EarlierAttemptUncertain_ReturnsUncertainAndNeverClaimsSuccess()
    {
        var eventId = "evt-uncertain-001";
        _store.RecordState(eventId, ProcessingState.Uncertain, "Downstream call timed out with unknown outcome.");

        var @event = CreateValidSampleEvent(eventId, "EMP-301");
        var result = _processor.Process(@event);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsDuplicate);
        Assert.Equal(EventProcessingStatus.Uncertain, result.Status);
        Assert.Equal(eventId, result.EventId);
        Assert.Contains("uncertain outcome", result.Message, StringComparison.OrdinalIgnoreCase);

        var stored = _store.Get(eventId);
        Assert.NotNull(stored);
        Assert.Equal(ProcessingState.Uncertain, stored.State);
    }

    [Fact]
    public void Process_EarlierAttemptFailed_ReturnsFailedResult()
    {
        var eventId = "evt-failed-001";
        _store.RecordState(eventId, ProcessingState.Failed, "Downstream rejected update permanently (400 Bad Request).");

        var @event = CreateValidSampleEvent(eventId, "EMP-302");
        var result = _processor.Process(@event);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsDuplicate);
        Assert.Equal(EventProcessingStatus.Failed, result.Status);
        Assert.Equal(eventId, result.EventId);
        Assert.Contains("previously failed", result.Message, StringComparison.OrdinalIgnoreCase);

        var stored = _store.Get(eventId);
        Assert.NotNull(stored);
        Assert.Equal(ProcessingState.Failed, stored.State);
    }

    [Fact]
    public void Process_RecoveryReset_DoesNotReplayUncertainOutcome()
    {
        var eventId = "evt-reset-001";
        _store.RecordState(eventId, ProcessingState.Uncertain, "Downstream timeout.");

        var @event = CreateValidSampleEvent(eventId, "EMP-303");

        // Before reset, processing returns Uncertain
        var blockedResult = _processor.Process(@event);
        Assert.Equal(EventProcessingStatus.Uncertain, blockedResult.Status);

        // An uncertain outcome may already have applied the side effect, so it
        // remains blocked until a durable reconciliation mechanism exists.
        var resetSuccess = _store.TryReset(eventId);
        Assert.False(resetSuccess);

        var replayedResult = _processor.Process(@event);
        Assert.Equal(EventProcessingStatus.Uncertain, replayedResult.Status);
        Assert.Equal(ProcessingState.Uncertain, _store.Get(eventId)?.State);
    }

    [Fact]
    public async Task ProcessAsync_WhenDownstreamSucceeds_MarksEventCompleted()
    {
        var store = new InMemoryEventProcessingStore();
        var mockClient = new StubEmployee360Client(DownstreamResult.Succeeded(200));
        var processor = new EmployeeEventProcessor(store, mockClient);

        var eventId = "evt-downstream-ok";
        var @event = CreateValidSampleEvent(eventId, "EMP-401");

        var result = await processor.ProcessAsync(@event);

        Assert.True(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Success, result.Status);
        Assert.Equal(ProcessingState.Completed, store.Get(eventId)?.State);
        Assert.Equal(eventId, mockClient.LastIdempotencyKey);
    }

    [Fact]
    public async Task ProcessAsync_WhenDownstreamFailsPermanently_MarksEventFailed()
    {
        var store = new InMemoryEventProcessingStore();
        var mockClient = new StubEmployee360Client(DownstreamResult.Permanent(400, "Invalid schema"));
        var processor = new EmployeeEventProcessor(store, mockClient);

        var eventId = "evt-downstream-perm-fail";
        var @event = CreateValidSampleEvent(eventId, "EMP-402");

        var result = await processor.ProcessAsync(@event);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Failed, result.Status);
        Assert.Equal(ProcessingState.Failed, store.Get(eventId)?.State);
    }

    [Fact]
    public async Task ProcessAsync_WhenDownstreamTimesOut_MarksEventUncertain()
    {
        var store = new InMemoryEventProcessingStore();
        var mockClient = new StubEmployee360Client(DownstreamResult.Uncertain("Timeout occurred"));
        var processor = new EmployeeEventProcessor(store, mockClient);

        var eventId = "evt-downstream-timeout";
        var @event = CreateValidSampleEvent(eventId, "EMP-403");

        var result = await processor.ProcessAsync(@event);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Uncertain, result.Status);
        Assert.Equal(ProcessingState.Uncertain, store.Get(eventId)?.State);
    }

    [Fact]
    public async Task ProcessAsync_WhenDownstreamTransientRetriesExhausted_MarksEventFailed()
    {
        var store = new InMemoryEventProcessingStore();
        var mockClient = new StubEmployee360Client(DownstreamResult.Transient(503, "503 retries exhausted"));
        var processor = new EmployeeEventProcessor(store, mockClient);

        var eventId = "evt-downstream-exhausted";
        var @event = CreateValidSampleEvent(eventId, "EMP-404");

        var result = await processor.ProcessAsync(@event);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Failed, result.Status);
        Assert.Equal(ProcessingState.Failed, store.Get(eventId)?.State);
    }

    [Fact]
    public async Task ProcessAsync_OutOfOrderSequence_SuppressesStaleEventBeforeDownstreamCall()
    {
        var store = new InMemoryEventProcessingStore();
        var client = new CountingClient();
        var processor = new EmployeeEventProcessor(store, client);

        var newest = CreateValidSampleEvent("evt-seq-10", "EMP-SEQ") with { SequenceNumber = 10 };
        var stale = CreateValidSampleEvent("evt-seq-9", "EMP-SEQ") with { SequenceNumber = 9 };

        Assert.Equal(EventProcessingStatus.Success, (await processor.ProcessAsync(newest)).Status);
        var result = await processor.ProcessAsync(stale);

        Assert.Equal(EventProcessingStatus.Stale, result.Status);
        Assert.Equal(1, client.CallCount);
        Assert.Equal(ProcessingState.Stale, store.Get("evt-seq-9")?.State);
        Assert.Contains("sequence 9", store.Get("evt-seq-9")?.Details);
    }

    private sealed class CountingClient : IEmployee360Client
    {
        public int CallCount { get; private set; }

        public Task<DownstreamResult> UpdateEmployeeAsync(string employeeId, Employee360UpdateRequest request,
            string idempotencyKey, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(DownstreamResult.Succeeded(200));
        }
    }

    private sealed class StubEmployee360Client : IEmployee360Client
    {
        private readonly DownstreamResult _result;

        public StubEmployee360Client(DownstreamResult result)
        {
            _result = result;
        }

        public string? LastIdempotencyKey { get; private set; }

        public Task<DownstreamResult> UpdateEmployeeAsync(
            string employeeId,
            Employee360UpdateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(_result);
        }
    }

    private static EmployeeUpdatedEvent CreateValidSampleEvent(string eventId, string employeeId)
    {
        return new EmployeeUpdatedEvent
        {
            EventId = eventId,
            EventType = "EmployeeUpdated",
            Timestamp = DateTimeOffset.UtcNow,
            EmployeeId = employeeId,
            FirstName = "Test",
            LastName = "User",
            Email = $"{employeeId.ToLowerInvariant()}@example.com",
            Department = "Engineering",
            OfficeCode = "CPT"
        };
    }
}
