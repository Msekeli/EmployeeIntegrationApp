using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.MockApi.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, EmployeeUpdateRequest> ProcessedKeys =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly ConcurrentDictionary<string, int> AttemptCounters =
        new(StringComparer.OrdinalIgnoreCase);

    public static void ResetState()
    {
        ProcessedKeys.Clear();
        AttemptCounters.Clear();
    }

    [HttpPut("{employeeId}")]
    public IActionResult UpdateEmployee(
        string employeeId,
        [FromBody] EmployeeUpdateRequest employee,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        // 1. Check for simulation header: X-Mock-Fail-Times
        if (Request.Headers.TryGetValue("X-Mock-Fail-Times", out var failTimesHeader) &&
            int.TryParse(failTimesHeader, out var failTimes) &&
            !string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var attempts = AttemptCounters.AddOrUpdate(idempotencyKey, 1, (_, count) => count + 1);
            if (attempts <= failTimes)
            {
                return StatusCode(503, new
                {
                    error = "Service Unavailable",
                    attempt = attempts,
                    maxFailures = failTimes
                });
            }
        }

        // 2. Check for simulation header: X-Mock-Status
        if (Request.Headers.TryGetValue("X-Mock-Status", out var mockStatusHeader) &&
            int.TryParse(mockStatusHeader, out var mockStatusCode))
        {
            if (mockStatusCode == 429)
            {
                Response.Headers.Append("Retry-After", "0");
                return StatusCode(429, new { error = "Too Many Requests (Rate limit exceeded)" });
            }

            return StatusCode(mockStatusCode, new { error = $"Simulated HTTP {mockStatusCode}" });
        }

        // 3. Deduplication via Idempotency-Key header
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (!ProcessedKeys.TryAdd(idempotencyKey, employee))
            {
                // Key was already processed: do not re-apply side-effects
                return Ok(new EmployeeUpdateResponse(
                    EmployeeId: employeeId,
                    Status: "already_processed",
                    IsDuplicate: true,
                    IdempotencyKey: idempotencyKey,
                    Employee: employee));
            }
        }

        return Ok(new EmployeeUpdateResponse(
            EmployeeId: employeeId,
            Status: "updated",
            IsDuplicate: false,
            IdempotencyKey: idempotencyKey,
            Employee: employee));
    }
}

public sealed record EmployeeUpdateResponse(
    string EmployeeId,
    string Status,
    bool IsDuplicate,
    string? IdempotencyKey,
    EmployeeUpdateRequest Employee);

public sealed record EmployeeUpdateRequest(
    string? EmployeeId,
    string? FirstName,
    string? LastName,
    string? FullName,
    string? Email,
    string? Department,
    string? JobTitle,
    string? OfficeLocation,
    string? OfficeCode,
    DateTimeOffset? UpdatedAt);
