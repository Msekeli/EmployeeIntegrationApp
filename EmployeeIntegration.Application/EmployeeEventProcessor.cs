using System.Text.Json;
using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Application;

public sealed class EmployeeEventProcessor : IEmployeeEventProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IEventProcessingStore _store;
    private readonly IEmployee360Client _downstreamClient;

    public EmployeeEventProcessor(IEventProcessingStore store)
        : this(store, new DefaultSuccessfulEmployee360Client())
    {
    }

    public EmployeeEventProcessor(IEventProcessingStore store, IEmployee360Client downstreamClient)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _downstreamClient = downstreamClient ?? throw new ArgumentNullException(nameof(downstreamClient));
    }

    public EventProcessingResult ProcessJson(string? json) =>
        ProcessJsonAsync(json).GetAwaiter().GetResult();

    public EventProcessingResult Process(EmployeeUpdatedEvent? @event) =>
        ProcessAsync(@event).GetAwaiter().GetResult();

    public async Task<EventProcessingResult> ProcessJsonAsync(string? json, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return EventProcessingResult.Malformed("Payload cannot be empty or whitespace.");
        }

        EmployeeUpdatedEvent? @event;
        try
        {
            @event = JsonSerializer.Deserialize<EmployeeUpdatedEvent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return EventProcessingResult.Malformed($"Malformed JSON payload: {ex.Message}");
        }
        catch (Exception ex)
        {
            return EventProcessingResult.Malformed($"Failed to parse payload: {ex.Message}");
        }

        if (@event == null)
        {
            return EventProcessingResult.Malformed("Payload deserialized to null.");
        }

        return await ProcessAsync(@event, cancellationToken);
    }

    public async Task<EventProcessingResult> ProcessAsync(EmployeeUpdatedEvent? @event, CancellationToken cancellationToken = default)
    {
        var validation = EmployeeUpdatedEventValidator.Validate(@event);
        if (!validation.IsValid)
        {
            return EventProcessingResult.Invalid(validation.Errors, @event?.EventId);
        }

        var eventId = @event!.EventId!;
        var claim = _store.TryClaim(eventId);

        switch (claim.Status)
        {
            case ClaimStatus.AlreadyCompleted:
                return EventProcessingResult.Duplicate(eventId);

            case ClaimStatus.InProgress:
                return EventProcessingResult.Conflict(eventId);

            case ClaimStatus.Uncertain:
                return EventProcessingResult.Uncertain(
                    eventId,
                    $"Event '{eventId}' has an uncertain outcome from an earlier attempt ({claim.ExistingRecord?.Details ?? "unconfirmed"}). It cannot be confirmed as completed or safely reprocessed without investigation.");

            case ClaimStatus.Failed:
                return EventProcessingResult.Failed(
                    eventId,
                    $"Event '{eventId}' previously failed processing: {claim.ExistingRecord?.Details ?? "unspecified error"}.");

            case ClaimStatus.Granted:
                break;

            default:
                return EventProcessingResult.Conflict(eventId);
        }

        Employee360UpdateRequest transformed;
        try
        {
            transformed = Employee360Transformer.Transform(@event);
        }
        catch (Exception ex)
        {
            _store.RecordState(eventId, ProcessingState.Failed, $"Transformation error: {ex.Message}");
            return EventProcessingResult.Failed(eventId, $"Transformation failed: {ex.Message}");
        }

        DownstreamResult downstreamResult;
        try
        {
            downstreamResult = await _downstreamClient.UpdateEmployeeAsync(
                transformed.EmployeeId,
                transformed,
                idempotencyKey: eventId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _store.RecordState(eventId, ProcessingState.Uncertain, $"Downstream client invocation error: {ex.Message}");
            return EventProcessingResult.Uncertain(eventId, $"Downstream invocation resulted in uncertain state: {ex.Message}");
        }

        switch (downstreamResult.Status)
        {
            case DownstreamStatus.Success:
                _store.RecordState(eventId, ProcessingState.Completed, $"Successfully forwarded to downstream Employee 360 (HTTP {downstreamResult.StatusCode}).");
                return EventProcessingResult.Succeeded(transformed, eventId);

            case DownstreamStatus.PermanentFailure:
                _store.RecordState(eventId, ProcessingState.Failed, $"Downstream rejected update permanently (HTTP {downstreamResult.StatusCode}): {downstreamResult.Message}");
                return EventProcessingResult.Failed(eventId, $"Downstream rejected update (HTTP {downstreamResult.StatusCode}): {downstreamResult.Message}");

            case DownstreamStatus.Uncertain:
                _store.RecordState(eventId, ProcessingState.Uncertain, $"Downstream update outcome is uncertain: {downstreamResult.Message}");
                return EventProcessingResult.Uncertain(eventId, $"Downstream update outcome is uncertain: {downstreamResult.Message}");

            case DownstreamStatus.TransientFailure:
            default:
                _store.RecordState(eventId, ProcessingState.Failed, $"Downstream transient retries exhausted (HTTP {downstreamResult.StatusCode}): {downstreamResult.Message}");
                return EventProcessingResult.Failed(eventId, $"Downstream transient retries exhausted (HTTP {downstreamResult.StatusCode}): {downstreamResult.Message}");
        }
    }
}

internal sealed class DefaultSuccessfulEmployee360Client : IEmployee360Client
{
    public Task<DownstreamResult> UpdateEmployeeAsync(
        string employeeId,
        Employee360UpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DownstreamResult.Succeeded(200));
    }
}
