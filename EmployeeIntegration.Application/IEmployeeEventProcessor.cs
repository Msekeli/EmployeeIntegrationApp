using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Application;

public interface IEmployeeEventProcessor
{
    EventProcessingResult Process(EmployeeUpdatedEvent? @event);
    EventProcessingResult ProcessJson(string? json);
}
