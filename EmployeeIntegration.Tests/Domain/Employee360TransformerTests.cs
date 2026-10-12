using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Tests.Domain;

public class Employee360TransformerTests
{
    [Fact]
    public void Transform_ValidEvent_TransformsCorrectly()
    {
        var timestamp = DateTimeOffset.Parse("2026-10-12T10:15:30Z");
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-123",
            EventType = "EmployeeUpdated",
            Timestamp = timestamp,
            SequenceNumber = 5,
            Data = new EmployeePayload
            {
                EmployeeId = " EMP-001 ",
                FirstName = " John ",
                LastName = " Doe ",
                Email = " John.Doe@Example.COM ",
                Department = " Engineering ",
                JobTitle = " Principal Architect ",
                OfficeCode = " CPT "
            }
        };

        var transformed = Employee360Transformer.Transform(@event);

        Assert.Equal("EMP-001", transformed.EmployeeId);
        Assert.Equal("John", transformed.FirstName);
        Assert.Equal("Doe", transformed.LastName);
        Assert.Equal("John Doe", transformed.FullName);
        Assert.Equal("john.doe@example.com", transformed.Email);
        Assert.Equal("Engineering", transformed.Department);
        Assert.Equal("Principal Architect", transformed.JobTitle);
        Assert.Equal("Cape Town", transformed.OfficeLocation);
        Assert.Equal("CPT", transformed.OfficeCode);
        Assert.Equal(timestamp, transformed.UpdatedAt);
    }

    [Fact]
    public void Transform_UnknownOfficeCode_PreservesCodeWithoutInventingMapping()
    {
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-124",
            EventType = "EmployeeUpdated",
            Timestamp = DateTimeOffset.Parse("2026-10-12T10:15:30Z"),
            EmployeeId = "EMP-002",
            FirstName = "Jane",
            LastName = "Roe",
            Email = "jane.roe@example.com",
            Department = "Finance",
            JobTitle = "Financial Analyst",
            OfficeCode = "  xyz  "
        };

        var transformed = Employee360Transformer.Transform(@event);

        Assert.Equal("XYZ", transformed.OfficeLocation);
        Assert.Equal("XYZ", transformed.OfficeCode);
    }

    [Fact]
    public void Transform_NullOfficeCode_ResultsInNullLocationAndCode()
    {
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-125",
            EventType = "EmployeeUpdated",
            Timestamp = DateTimeOffset.Parse("2026-10-12T10:15:30Z"),
            EmployeeId = "EMP-003",
            FirstName = "Sam",
            LastName = "Taylor",
            Email = "sam.taylor@example.com",
            Department = "People",
            JobTitle = "HR Specialist",
            OfficeCode = null
        };

        var transformed = Employee360Transformer.Transform(@event);

        Assert.Null(transformed.OfficeLocation);
        Assert.Null(transformed.OfficeCode);
    }

    [Fact]
    public void Transform_DeterministicOutput_ProducesIdenticalResultAcrossMultipleCalls()
    {
        var @event = new EmployeeUpdatedEvent
        {
            EventId = "evt-999",
            EventType = "EmployeeUpdated",
            Timestamp = DateTimeOffset.Parse("2026-10-12T12:00:00Z"),
            SequenceNumber = 10,
            Data = new EmployeePayload
            {
                EmployeeId = "EMP-999",
                FirstName = "Chris",
                LastName = "Evans",
                Email = "chris.evans@example.com",
                Department = "Operations",
                JobTitle = "Director",
                OfficeCode = "cpt"
            }
        };

        var firstRun = Employee360Transformer.Transform(@event);
        var secondRun = Employee360Transformer.Transform(@event);

        Assert.Equal(firstRun, secondRun);
        Assert.Equal(firstRun.FullName, secondRun.FullName);
        Assert.Equal(firstRun.Email, secondRun.Email);
        Assert.Equal(firstRun.OfficeLocation, secondRun.OfficeLocation);
        Assert.Equal(firstRun.UpdatedAt, secondRun.UpdatedAt);
    }
}
