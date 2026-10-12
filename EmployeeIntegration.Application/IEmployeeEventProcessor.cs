using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Application;

public interface IEmployeeEventProcessor
{
    Task<EventProcessingResult> ProcessAsync(EmployeeUpdatedEvent? @event, CancellationToken cancellationToken = default);
    Task<EventProcessingResult> ProcessJsonAsync(string? json, CancellationToken cancellationToken = default);

    EventProcessingResult Process(EmployeeUpdatedEvent? @event);
    EventProcessingResult ProcessJson(string? json);
}
