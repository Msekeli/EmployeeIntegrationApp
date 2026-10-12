using Employee360.MockApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeIntegration.Tests.MockApi;

public class EmployeesControllerTests
{
    public EmployeesControllerTests()
    {
        EmployeesController.ResetState();
    }

    [Fact]
    public void UpdateEmployee_FirstCall_ReturnsUpdatedWithIsDuplicateFalse()
    {
        var controller = new EmployeesController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var request = new EmployeeUpdateRequest(
            EmployeeId: "EMP-100",
            FirstName: "Ada",
            LastName: "Lovelace",
            FullName: "Ada Lovelace",
            Email: "ada@computing.org",
            Department: "Engineering",
            JobTitle: "Lead",
            OfficeLocation: "Cape Town",
            OfficeCode: "CPT",
            UpdatedAt: DateTimeOffset.UtcNow);

        var result = controller.UpdateEmployee("EMP-100", request, "evt-first-call");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var val = Assert.IsType<EmployeeUpdateResponse>(okResult.Value);
        Assert.Equal("updated", val.Status);
        Assert.False(val.IsDuplicate);
    }

    [Fact]
    public void UpdateEmployee_RepeatedCallWithSameIdempotencyKey_ReturnsAlreadyProcessedWithIsDuplicateTrue()
    {
        var controller = new EmployeesController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var request = new EmployeeUpdateRequest(
            EmployeeId: "EMP-200",
            FirstName: "Grace",
            LastName: "Hopper",
            FullName: "Grace Hopper",
            Email: "grace@navy.mil",
            Department: "Systems",
            JobTitle: "Admiral",
            OfficeLocation: "Cape Town",
            OfficeCode: "CPT",
            UpdatedAt: DateTimeOffset.UtcNow);

        var firstResult = controller.UpdateEmployee("EMP-200", request, "evt-dedup-key-1");
        var okFirst = Assert.IsType<OkObjectResult>(firstResult);
        var valFirst = Assert.IsType<EmployeeUpdateResponse>(okFirst.Value);
        Assert.Equal("updated", valFirst.Status);
        Assert.False(valFirst.IsDuplicate);

        // Second delivery with the same idempotency key
        var secondResult = controller.UpdateEmployee("EMP-200", request, "evt-dedup-key-1");
        var okSecond = Assert.IsType<OkObjectResult>(secondResult);
        var valSecond = Assert.IsType<EmployeeUpdateResponse>(okSecond.Value);
        Assert.Equal("already_processed", valSecond.Status);
        Assert.True(valSecond.IsDuplicate);
    }

    [Fact]
    public void UpdateEmployee_SimulateRateLimit429_Returns429WithRetryAfter()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Mock-Status"] = "429";

        var controller = new EmployeesController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new EmployeeUpdateRequest(
            EmployeeId: "EMP-300",
            FirstName: "Alan",
            LastName: "Turing",
            FullName: "Alan Turing",
            Email: "alan@bletchley.org",
            Department: "Cryptography",
            JobTitle: "Mathematician",
            OfficeLocation: "Cape Town",
            OfficeCode: "CPT",
            UpdatedAt: DateTimeOffset.UtcNow);

        var result = controller.UpdateEmployee("EMP-300", request, "evt-429-test");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(429, objectResult.StatusCode);
        Assert.True(httpContext.Response.Headers.ContainsKey("Retry-After"));
    }

    [Fact]
    public void UpdateEmployee_SimulateTransientFailTimes_FailsRequestedTimesThenSucceeds()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Mock-Fail-Times"] = "2";

        var controller = new EmployeesController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new EmployeeUpdateRequest(
            EmployeeId: "EMP-400",
            FirstName: "Margaret",
            LastName: "Hamilton",
            FullName: "Margaret Hamilton",
            Email: "margaret@nasa.gov",
            Department: "Software",
            JobTitle: "Director",
            OfficeLocation: "Cape Town",
            OfficeCode: "CPT",
            UpdatedAt: DateTimeOffset.UtcNow);

        // Attempt 1 -> 503
        var res1 = controller.UpdateEmployee("EMP-400", request, "evt-fail-times-key");
        var objRes1 = Assert.IsType<ObjectResult>(res1);
        Assert.Equal(503, objRes1.StatusCode);

        // Attempt 2 -> 503
        var res2 = controller.UpdateEmployee("EMP-400", request, "evt-fail-times-key");
        var objRes2 = Assert.IsType<ObjectResult>(res2);
        Assert.Equal(503, objRes2.StatusCode);

        // Attempt 3 -> 200 OK
        var res3 = controller.UpdateEmployee("EMP-400", request, "evt-fail-times-key");
        var okRes3 = Assert.IsType<OkObjectResult>(res3);
        var val3 = Assert.IsType<EmployeeUpdateResponse>(okRes3.Value);
        Assert.Equal("updated", val3.Status);
    }
}
