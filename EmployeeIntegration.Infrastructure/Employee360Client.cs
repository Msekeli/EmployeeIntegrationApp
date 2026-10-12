using System.Net;
using System.Net.Http.Json;
using EmployeeIntegration.Application;
using EmployeeIntegration.Domain;

namespace EmployeeIntegration.Infrastructure;

/// <summary>
/// Downstream HTTP client for Employee 360 REST API with bounded retries,
/// explicit timeouts, transient failure classification, and honest uncertainty handling.
/// </summary>
public sealed class Employee360Client : IEmployee360Client
{
    private readonly HttpClient _httpClient;
    private readonly Employee360ClientOptions _options;

    public Employee360Client(HttpClient httpClient, Employee360ClientOptions? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? new Employee360ClientOptions();

        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        if (_httpClient.Timeout == TimeSpan.FromSeconds(100))
        {
            _httpClient.Timeout = _options.Timeout;
        }
    }

    public async Task<DownstreamResult> UpdateEmployeeAsync(
        string employeeId,
        Employee360UpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var requestUri = $"api/employees/{Uri.EscapeDataString(employeeId)}";
        var maxAttempts = Math.Max(1, _options.MaxRetries);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri)
                {
                    Content = JsonContent.Create(request)
                };
                httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return DownstreamResult.Succeeded((int)response.StatusCode, attempts: attempt);
                }

                var statusCode = (int)response.StatusCode;

                // Permanent client failures (4xx except 429) must NOT be retried
                if (statusCode >= 400 && statusCode < 500 && statusCode != (int)HttpStatusCode.TooManyRequests)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    return DownstreamResult.Permanent(statusCode, $"Downstream client error (HTTP {statusCode}): {content}", attempts: attempt);
                }

                // If transient failure retries are exhausted, report outcome
                if (attempt == maxAttempts)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (statusCode == (int)HttpStatusCode.GatewayTimeout)
                    {
                        // 504 Gateway Timeout: ambiguous whether downstream completed before timeout
                        return DownstreamResult.Uncertain($"Gateway timeout (HTTP 504) after {attempt} attempts: {content}", attempts: attempt, statusCode: statusCode);
                    }

                    return DownstreamResult.Transient(statusCode, $"Transient failure (HTTP {statusCode}) retries exhausted after {attempt} attempts: {content}", attempts: attempt);
                }

                // Wait before retrying transient failure
                var delay = GetRetryDelay(response, attempt);
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Internal timeout occurred
                if (attempt == maxAttempts)
                {
                    // Honest handling: timeout means state is uncertain, never assume unapplied or completed
                    return DownstreamResult.Uncertain(
                        $"Downstream request timed out after {attempt} attempts without response confirmation: {ex.Message}",
                        attempts: attempt);
                }

                var delay = GetRetryDelay(null, attempt);
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                // Network or connection failure
                if (attempt == maxAttempts)
                {
                    return DownstreamResult.Uncertain(
                        $"Network or transport failure during downstream communication after {attempt} attempts: {ex.Message}",
                        attempts: attempt);
                }

                var delay = GetRetryDelay(null, attempt);
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        return DownstreamResult.Uncertain($"Downstream call ended without definitive status after {maxAttempts} attempts.", attempts: maxAttempts);
    }

    private TimeSpan GetRetryDelay(HttpResponseMessage? response, int attempt)
    {
        if (response?.StatusCode == (HttpStatusCode)429 && response.Headers.RetryAfter != null)
        {
            if (response.Headers.RetryAfter.Delta.HasValue)
            {
                var delta = response.Headers.RetryAfter.Delta.Value;
                if (delta > TimeSpan.Zero && delta <= _options.MaxRetryDelay)
                {
                    return delta;
                }
            }
        }

        var delayMs = _options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var delay = TimeSpan.FromMilliseconds(delayMs);
        return delay <= _options.MaxRetryDelay ? delay : _options.MaxRetryDelay;
    }
}
