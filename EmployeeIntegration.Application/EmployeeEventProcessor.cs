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

    public EmployeeEventProcessor(IEventProcessingStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public EventProcessingResult ProcessJson(string? json)
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

        return Process(@event);
    }

    public EventProcessingResult Process(EmployeeUpdatedEvent? @event)
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

        try
        {
            var transformed = Employee360Transformer.Transform(@event);
            _store.RecordState(eventId, ProcessingState.Completed, "Event transformed successfully.");
            return EventProcessingResult.Succeeded(transformed, eventId);
        }
        catch (Exception ex)
        {
            _store.RecordState(eventId, ProcessingState.Uncertain, $"Transformation error: {ex.Message}");
            return EventProcessingResult.Uncertain(
                eventId,
                $"Processing encountered an error resulting in an uncertain outcome: {ex.Message}");
        }
    }
}
