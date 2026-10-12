using System.Text.Json;
using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Application;

public sealed class EmployeeEventProcessor : IEmployeeEventProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            return EventProcessingResult.Invalid(validation.Errors);
        }

        var transformed = Employee360Transformer.Transform(@event!);
        return EventProcessingResult.Succeeded(transformed);
    }
}
