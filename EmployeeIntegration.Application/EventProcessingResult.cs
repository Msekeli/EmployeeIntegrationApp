using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Application;

public enum EventProcessingStatus
{
    Success,
    Invalid,
    Malformed
}

public sealed record EventProcessingResult
{
    public EventProcessingStatus Status { get; init; }
    public bool IsSuccess => Status == EventProcessingStatus.Success;
    public Employee360UpdateRequest? TransformedRequest { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    public string? Message { get; init; }

    public static EventProcessingResult Succeeded(Employee360UpdateRequest request) =>
        new()
        {
            Status = EventProcessingStatus.Success,
            TransformedRequest = request,
            Message = "Event processed and transformed successfully."
        };

    public static EventProcessingResult Invalid(IReadOnlyList<ValidationError> errors) =>
        new()
        {
            Status = EventProcessingStatus.Invalid,
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
