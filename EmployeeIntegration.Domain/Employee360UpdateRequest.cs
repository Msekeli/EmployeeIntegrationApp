using System.Text.Json.Serialization;

namespace EmployeeIntegration.Domain;

public sealed record Employee360UpdateRequest
{
    [JsonPropertyName("employeeId")]
    public required string EmployeeId { get; init; }

    [JsonPropertyName("firstName")]
    public required string FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public required string LastName { get; init; }

    [JsonPropertyName("fullName")]
    public required string FullName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("department")]
    public required string Department { get; init; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; init; }

    [JsonPropertyName("officeLocation")]
    public string? OfficeLocation { get; init; }

    [JsonPropertyName("officeCode")]
    public string? OfficeCode { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}
