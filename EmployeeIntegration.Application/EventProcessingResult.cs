using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Application;

public enum EventProcessingStatus
{
    Success,
    Duplicate,
    Conflict,
    Invalid,
    Malformed,
    Uncertain,
    Failed
}

public sealed record EventProcessingResult
{
    public EventProcessingStatus Status { get; init; }
    public bool IsSuccess => Status == EventProcessingStatus.Success;
    public bool IsDuplicate => Status == EventProcessingStatus.Duplicate;
    public string? EventId { get; init; }
    public Employee360UpdateRequest? TransformedRequest { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    public string? Message { get; init; }

    public static EventProcessingResult Succeeded(Employee360UpdateRequest request, string? eventId = null) =>
        new()
        {
            Status = EventProcessingStatus.Success,
            EventId = eventId,
            TransformedRequest = request,
            Message = "Event processed and transformed successfully."
        };

    public static EventProcessingResult Duplicate(string eventId, string? message = null) =>
        new()
        {
            Status = EventProcessingStatus.Duplicate,
            EventId = eventId,
            Message = message ?? $"Event '{eventId}' was already processed successfully."
        };

    public static EventProcessingResult Conflict(string eventId, string? message = null) =>
        new()
        {
            Status = EventProcessingStatus.Conflict,
            EventId = eventId,
            Message = message ?? $"Event '{eventId}' is currently being processed by another execution."
        };

    public static EventProcessingResult Uncertain(string eventId, string message) =>
        new()
        {
            Status = EventProcessingStatus.Uncertain,
            EventId = eventId,
            Message = message
        };

    public static EventProcessingResult Failed(string eventId, string message) =>
        new()
        {
            Status = EventProcessingStatus.Failed,
            EventId = eventId,
            Message = message
        };

    public static EventProcessingResult Invalid(IReadOnlyList<ValidationError> errors, string? eventId = null) =>
        new()
        {
            Status = EventProcessingStatus.Invalid,
            EventId = eventId,
            Errors = errors,
            Message = "Event validation failed."
        };

    public static EventProcessingResult Malformed(string message) =>
        new()
        {
            Status = EventProcessingStatus.Malformed,
            Errors = new[] { new ValidationError("Payload", message) },
            Message = message
        };
}
