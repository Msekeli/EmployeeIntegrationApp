namespace EmployeeIntegration.Domain;

/// <summary>
/// Handles normalization of office codes and supplied location values.
/// </summary>
public static class OfficeLocationNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> KnownOfficeLocations =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CPT"] = "Cape Town"
        };

    /// <summary>
    /// Normalizes a source office code or location to a downstream office location name.
    /// Maps "CPT" (case-insensitive, trimmed) to "Cape Town" and preserves supplied
    /// "Cape Town" values. Unknown codes are normalized consistently without inventing
    /// fictitious mappings. Returns null if the value is null or whitespace.
    /// </summary>
    public static string? NormalizeOfficeLocation(string? officeCode)
    {
        if (string.IsNullOrWhiteSpace(officeCode))
        {
            return null;
        }

        var trimmed = officeCode.Trim();

        if (string.Equals(trimmed, "Cape Town", StringComparison.OrdinalIgnoreCase))
        {
            return "Cape Town";
        }

        if (KnownOfficeLocations.TryGetValue(trimmed, out var mappedLocation))
        {
            return mappedLocation;
        }

        // Unknown code: preserve normalized code without inventing a fictitious mapping.
        return trimmed.ToUpperInvariant();
    }

    /// <summary>
    /// Returns the normalized office code (trimmed, uppercase), or null if null/whitespace.
    /// </summary>
    public static string? NormalizeOfficeCode(string? officeCode)
    {
        if (string.IsNullOrWhiteSpace(officeCode))
        {
            return null;
        }

        return officeCode.Trim().ToUpperInvariant();
    }
}
