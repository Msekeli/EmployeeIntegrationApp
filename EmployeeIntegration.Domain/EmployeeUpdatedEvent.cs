using System.Text.Json.Serialization;

namespace EmployeeIntegration.Domain;

public sealed record EmployeeUpdatedEvent
{
    [JsonPropertyName("eventId")]
    public string? EventId { get; init; }

    [JsonPropertyName("eventType")]
    public string? EventType { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    [JsonPropertyName("effectiveDate")]
    public DateTimeOffset? EffectiveDate { get; init; }

    [JsonPropertyName("sequenceNumber")]
    public long? SequenceNumber { get; init; }

    [JsonPropertyName("data")]
    public EmployeePayload? Data { get; init; }

    // Direct flat properties for callers supplying flat events
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

    /// <summary>
    /// Resolves the effective employee payload, prioritizing nested Data if provided,
    /// and falling back to flat properties.
    /// </summary>
    public EmployeePayload GetEffectivePayload()
    {
        return new EmployeePayload
        {
            EmployeeId = Data?.EmployeeId ?? EmployeeId,
            FirstName = Data?.FirstName ?? FirstName,
            LastName = Data?.LastName ?? LastName,
            Email = Data?.Email ?? Email,
            Department = Data?.Department ?? Department,
            JobTitle = Data?.JobTitle ?? JobTitle,
            OfficeCode = Data?.OfficeCode ?? OfficeCode,
            Location = Data?.Location ?? Location
        };
    }

    public DateTimeOffset? GetEffectiveTimestamp() => Timestamp ?? EffectiveDate;
}
