
using EmployeeIntegration.Application;
using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Tests.Application;

public class EmployeeEventProcessorTests
{
    private readonly EmployeeEventProcessor _processor = new();

    [Fact]
    public void ProcessJson_ValidJson_ReturnsSuccessWithTransformedRequest()
    {
        var json = """
        {
            "eventId": "evt-501",
            "eventType": "EmployeeUpdated",
            "timestamp": "2026-10-12T14:30:00Z",
            "sequenceNumber": 1,
            "data": {
                "employeeId": "EMP-501",
                "firstName": "Grace",
                "lastName": "Hopper",
                "email": "grace.hopper@navy.mil",
                "department": "Systems",
                "jobTitle": "Admiral",
                "officeCode": "CPT"
            }
        }
        """;

        var result = _processor.ProcessJson(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Success, result.Status);
        Assert.NotNull(result.TransformedRequest);
        Assert.Equal("EMP-501", result.TransformedRequest.EmployeeId);
        Assert.Equal("Grace Hopper", result.TransformedRequest.FullName);
        Assert.Equal("Cape Town", result.TransformedRequest.OfficeLocation);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ProcessJson_ValidFlatJson_ReturnsSuccessWithTransformedRequest()
    {
        var json = """
        {
            "eventId": "evt-502",
            "eventType": "EmployeeUpdated",
            "timestamp": "2026-10-12T14:30:00Z",
            "employeeId": "EMP-502",
            "firstName": "Ada",
            "lastName": "Lovelace",
            "email": "ada.lovelace@computing.org",
            "department": "Mathematics",
            "jobTitle": "Visionary",
            "officeCode": "XYZ"
        }
        """;

        var result = _processor.ProcessJson(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Success, result.Status);
        Assert.NotNull(result.TransformedRequest);
        Assert.Equal("Ada Lovelace", result.TransformedRequest.FullName);
        Assert.Equal("XYZ", result.TransformedRequest.OfficeLocation);
    }

    [Fact]
    public void ProcessJson_AssessmentExample_ReturnsSuccessWithCorrectMappings()
    {
        var json = """
        {
            "eventId": "EVT-10452-001",
            "eventType": "EmployeeUpdated",
            "employeeId": "EMP10452",
            "firstName": "Amira",
            "lastName": "Khan",
            "department": "Data & AI",
            "location": "Cape Town",
            "effectiveDate": "2026-10-01T08:30:00Z"
        }
        """;

        var result = _processor.ProcessJson(json);

        Assert.True(
            result.IsSuccess,
            string.Join("; ", result.Errors.Select(
                error => $"{error.Field}: {error.Message}")));

        Assert.Equal(EventProcessingStatus.Success, result.Status);
        Assert.NotNull(result.TransformedRequest);
        Assert.Equal("EMP10452", result.TransformedRequest.EmployeeId);
        Assert.Equal("Amira", result.TransformedRequest.FirstName);
        Assert.Equal("Khan", result.TransformedRequest.LastName);
        Assert.Equal("Amira Khan", result.TransformedRequest.FullName);
        Assert.Equal("Data & AI", result.TransformedRequest.Department);
        Assert.Equal("Cape Town", result.TransformedRequest.OfficeLocation);
        Assert.Null(result.TransformedRequest.Email);
        Assert.Null(result.TransformedRequest.JobTitle);
        Assert.Equal(
            DateTimeOffset.Parse("2026-10-01T08:30:00Z"),
            result.TransformedRequest.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProcessJson_EmptyOrWhitespacePayload_ReturnsMalformedSafely(
        string? payload)
    {
        var result = _processor.ProcessJson(payload);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Malformed, result.Status);
        Assert.Null(result.TransformedRequest);
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"eventId\": ")]
    [InlineData("not a json string")]
    [InlineData("<xml>invalid</xml>")]
    public void ProcessJson_MalformedSyntax_ReturnsMalformedSafely(
        string malformedJson)
    {
        var result = _processor.ProcessJson(malformedJson);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Malformed, result.Status);
        Assert.Null(result.TransformedRequest);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, error => error.Field == "Payload");
    }

    [Fact]
    public void ProcessJson_MissingRequiredFields_ReturnsInvalidWithValidationErrors()
    {
        var invalidJson = """
        {
            "eventId": "evt-503",
            "eventType": "EmployeeUpdated",
            "timestamp": "2026-10-12T14:30:00Z",
            "data": {
                "firstName": "Alan",
                "email": "invalid-email"
            }
        }
        """;

        var result = _processor.ProcessJson(invalidJson);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Invalid, result.Status);
        Assert.Null(result.TransformedRequest);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, error => error.Field == "EmployeeId");
        Assert.Contains(result.Errors, error => error.Field == "LastName");
        Assert.Contains(result.Errors, error => error.Field == "Email");
        Assert.Contains(result.Errors, error => error.Field == "Department");
        Assert.DoesNotContain(result.Errors, error => error.Field == "JobTitle");
    }

    [Fact]
    public void Process_DirectValidEvent_ReturnsSuccess()
    {
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-777",
            EventType = "EmployeeUpdated",
            Timestamp = DateTimeOffset.UtcNow,
            EmployeeId = "EMP-777",
            FirstName = "Margaret",
            LastName = "Hamilton",
            Email = "margaret.hamilton@apollo.nasa.gov",
            Department = "Software",
            JobTitle = "Director",
            OfficeCode = "CPT"
        };

        var result = _processor.Process(@event);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.TransformedRequest);
        Assert.Equal("Cape Town", result.TransformedRequest.OfficeLocation);
    }

    [Fact]
    public void Process_DirectInvalidEvent_ReturnsInvalid()
    {
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-888"
        };

        var result = _processor.Process(@event);

        Assert.False(result.IsSuccess);
        Assert.Equal(EventProcessingStatus.Invalid, result.Status);
        Assert.NotEmpty(result.Errors);
    }
}
