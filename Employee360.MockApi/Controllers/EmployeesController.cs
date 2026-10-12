using Microsoft.AspNetCore.Mvc;

namespace Employee360.MockApi.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController : ControllerBase
{
    [HttpPut("{employeeId}")]
    public IActionResult UpdateEmployee(
        string employeeId,
        [FromBody] EmployeeUpdateRequest employee)
    {
        return Ok(new
        {
            employeeId,
            status = "updated",
            employee
        });
    }
}

public sealed record EmployeeUpdateRequest(
    string FirstName,
    string LastName,
    string Department,
    string Location,
    DateTimeOffset EffectiveDate);
