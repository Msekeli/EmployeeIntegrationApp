using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Tests.Domain;

public class EmployeeUpdatedEventValidationTests
{
    private static EmployeeUpdatedEvent CreateValidEvent() => new()
    {
        EventId = "evt-001",
        EventType = "EmployeeUpdated",
        Timestamp = DateTimeOffset.Parse("2026-10-12T08:00:00Z"),
        SequenceNumber = 1,
        Data = new EmployeePayload
        {
            EmployeeId = "EMP-100",
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice.smith@example.com",
            Department = "Engineering",
            JobTitle = "Staff Engineer",
            OfficeCode = "CPT"
        }
    };

    [Fact]
    public void Validate_ValidEvent_PassesValidation()
    {
        var @event = CreateValidEvent();

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidFlatEvent_PassesValidation()
    {
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-002",
            EventType = "EmployeeUpdated",
            Timestamp = DateTimeOffset.Parse("2026-10-12T08:00:00Z"),
            EmployeeId = "EMP-101",
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob.jones@example.com",
            Department = "Marketing",
            JobTitle = "Marketing Manager",
            OfficeCode = "CPT"
        };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NullEvent_FailsSafely()
    {
        var result = EmployeeUpdatedEventValidator.Validate(null);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Event", result.Errors[0].Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingEventId_FailsValidation(string? eventId)
    {
        var @event = CreateValidEvent() with { EventId = eventId };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "EventId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingEventType_FailsValidation(string? eventType)
    {
        var @event = CreateValidEvent() with { EventType = eventType };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "EventType");
    }

    [Theory]
    [InlineData("EmployeeCreated")]
    [InlineData("EmployeeDeleted")]
    [InlineData("UnknownEvent")]
    public void Validate_IncorrectEventType_FailsValidation(string eventType)
    {
        var @event = CreateValidEvent() with { EventType = eventType };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "EventType" && e.Message.Contains("EmployeeUpdated"));
    }

    [Fact]
    public void Validate_MissingTimestampAndEffectiveDate_FailsValidation()
    {
        var @event = CreateValidEvent() with { Timestamp = null };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "EffectiveDate");
    }

    [Fact]
    public void Validate_DefaultTimestampAndEffectiveDate_FailsValidation()
    {
        var @event = CreateValidEvent() with { Timestamp = default(DateTimeOffset) };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "EffectiveDate");
    }

    [Fact]
    public void Validate_NegativeSequenceNumber_FailsValidation()
    {
        var @event = CreateValidEvent() with { SequenceNumber = -1 };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "SequenceNumber");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingEmployeeId_FailsValidation(string? employeeId)
    {
        var @event = CreateValidEvent() with
        {
            Data = CreateValidEvent().Data! with { EmployeeId = employeeId }
        };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "EmployeeId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingFirstName_FailsValidation(string? firstName)
    {
        var @event = CreateValidEvent() with
        {
            Data = CreateValidEvent().Data! with { FirstName = firstName }
        };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "FirstName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingLastName_FailsValidation(string? lastName)
    {
        var @event = CreateValidEvent() with
        {
            Data = CreateValidEvent().Data! with { LastName = lastName }
        };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "LastName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingEmail_IsAllowed(string? email)
    {
        var @event = CreateValidEvent() with
        {
            Data = CreateValidEvent().Data! with { Email = email }
        };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("alice@")]
    [InlineData("alice example.com")]
    [InlineData("alice@domain")]
    public void Validate_MalformedEmail_FailsValidation(string invalidEmail)
    {
        var @event = CreateValidEvent() with
        {
            Data = CreateValidEvent().Data! with { Email = invalidEmail }
        };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "Email" && e.Message.Contains("format"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingDepartment_FailsValidation(string? department)
    {
        var @event = CreateValidEvent() with
        {
            Data = CreateValidEvent().Data! with { Department = department }
        };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "Department");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingJobTitle_IsAllowed(string? jobTitle)
    {
        var @event = CreateValidEvent() with
        {
            Data = CreateValidEvent().Data! with { JobTitle = jobTitle }
        };

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MultipleMissingFields_ReportsAllErrors()
    {
        var @event = new EmployeeUpdatedEvent();

        var result = EmployeeUpdatedEventValidator.Validate(@event);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 7);
    }
}
