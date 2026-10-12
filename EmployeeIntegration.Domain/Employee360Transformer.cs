namespace EmployeeIntegration.Domain;

/// <summary>
/// Deterministically transforms an EmployeeUpdatedEvent into an Employee360UpdateRequest.
/// </summary>
public static class Employee360Transformer
{
    /// <summary>
    /// Deterministically transforms an EmployeeUpdatedEvent into an Employee360UpdateRequest.
    /// Normalizes whitespace, formats full name, converts email to lowercase,
    /// normalizes office location (mapping CPT to Cape Town and preserving supplied locations),
    /// and uses the source timestamp/effective date for deterministic UpdatedAt.
    /// </summary>
    public static Employee360UpdateRequest Transform(EmployeeUpdatedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var payload = @event.GetEffectivePayload();

        var firstName = (payload.FirstName ?? string.Empty).Trim();
        var lastName = (payload.LastName ?? string.Empty).Trim();
        var fullName = string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)
            ? string.Empty
            : $"{firstName} {lastName}".Trim();

        var email = string.IsNullOrWhiteSpace(payload.Email)
            ? null
            : payload.Email.Trim().ToLowerInvariant();
        var department = (payload.Department ?? string.Empty).Trim();
        var jobTitle = string.IsNullOrWhiteSpace(payload.JobTitle)
            ? null
            : payload.JobTitle.Trim();
        var location = payload.Location ?? payload.OfficeCode;

        return new Employee360UpdateRequest
        {
            EmployeeId = (payload.EmployeeId ?? string.Empty).Trim(),
            FirstName = firstName,
            LastName = lastName,
            FullName = fullName,
            Email = email,
            Department = department,
            JobTitle = jobTitle,
            OfficeLocation = OfficeLocationNormalizer.NormalizeOfficeLocation(location),
            OfficeCode = OfficeLocationNormalizer.NormalizeOfficeCode(payload.OfficeCode),
            UpdatedAt = @event.GetEffectiveTimestamp() ?? DateTimeOffset.MinValue
        };
    }
}
