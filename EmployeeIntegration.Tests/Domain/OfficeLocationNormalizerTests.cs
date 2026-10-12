using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Tests.Domain;

public class OfficeLocationNormalizerTests
{
    [Theory]
    [InlineData("CPT", "Cape Town")]
    [InlineData("cpt", "Cape Town")]
    [InlineData("  cpt  ", "Cape Town")]
    [InlineData("  CPT  ", "Cape Town")]
    public void NormalizeOfficeLocation_CptOfficeCode_MapsToCapeTown(string inputCode, string expectedLocation)
    {
        var result = OfficeLocationNormalizer.NormalizeOfficeLocation(inputCode);

        Assert.Equal(expectedLocation, result);
    }

    [Theory]
    [InlineData("XYZ", "XYZ")]
    [InlineData("xyz", "XYZ")]
    [InlineData("  xyz  ", "XYZ")]
    [InlineData("JHB", "JHB")]
    [InlineData("DUR", "DUR")]
    [InlineData("UNKNOWN_CODE", "UNKNOWN_CODE")]
    public void NormalizeOfficeLocation_UnknownOfficeCode_PreservesCodeWithoutInventingMapping(string inputCode, string expectedLocation)
    {
        var result = OfficeLocationNormalizer.NormalizeOfficeLocation(inputCode);

        Assert.Equal(expectedLocation, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeOfficeLocation_NullOrWhitespace_ReturnsNull(string? inputCode)
    {
        var result = OfficeLocationNormalizer.NormalizeOfficeLocation(inputCode);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("CPT", "CPT")]
    [InlineData("cpt", "CPT")]
    [InlineData("  cpt  ", "CPT")]
    [InlineData("xyz", "XYZ")]
    [InlineData("  jhb  ", "JHB")]
    public void NormalizeOfficeCode_ValidInput_NormalizesTrimmedUppercase(string inputCode, string expectedCode)
    {
        var result = OfficeLocationNormalizer.NormalizeOfficeCode(inputCode);

        Assert.Equal(expectedCode, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeOfficeCode_NullOrWhitespace_ReturnsNull(string? inputCode)
    {
        var result = OfficeLocationNormalizer.NormalizeOfficeCode(inputCode);

        Assert.Null(result);
    }
}
