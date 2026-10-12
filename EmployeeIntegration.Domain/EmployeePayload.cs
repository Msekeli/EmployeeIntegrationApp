using System.Text.Json.Serialization;

namespace EmployeeIntegration.Domain;

public sealed record EmployeePayload
{
    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("department")]
    public string? Department { get; init; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; init; }

    [JsonPropertyName("officeCode")]
    public string? OfficeCode { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }
}
