using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Application;

/// <summary>
/// Application contract for forwarding employee updates to the downstream Employee 360 REST API.
/// </summary>
public interface IEmployee360Client
{
    /// <summary>
    /// Sends an employee update request to the downstream system with bounded retries and idempotency.
    /// </summary>
    Task<DownstreamResult> UpdateEmployeeAsync(
        string employeeId,
        Employee360UpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
