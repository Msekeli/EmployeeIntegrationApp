using System.Net;
using EmployeeIntegration.Application;
using EmployeeIntegration.Domain;
using EmployeeIntegration.Infrastructure;

namespace EmployeeIntegration.Tests.Infrastructure;

public class Employee360ClientTests
{
    private static readonly Employee360UpdateRequest SampleRequest = new()
    {
        EmployeeId = "EMP-777",
        FirstName = "Ada",
        LastName = "Lovelace",
        FullName = "Ada Lovelace",
        Email = "ada@computing.org",
        Department = "Engineering",
        JobTitle = "Visionary",
        OfficeLocation = "Cape Town",
        OfficeCode = "CPT",
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static readonly Employee360ClientOptions FastOptions = new()
    {
        BaseUrl = "http://localhost:5223",
        Timeout = TimeSpan.FromSeconds(2),
        MaxRetries = 3,
        InitialRetryDelay = TimeSpan.Zero,
        MaxRetryDelay = TimeSpan.Zero
    };

    [Fact]
    public async Task UpdateEmployeeAsync_SuccessfulResponse_ReturnsSuccessWithOneAttempt()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":"updated"}""")
        });

        var client = new Employee360Client(new HttpClient(handler), FastOptions);

        var result = await client.UpdateEmployeeAsync("EMP-777", SampleRequest, "evt-123");

        Assert.True(result.IsSuccess);
        Assert.Equal(DownstreamStatus.Success, result.Status);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(1, result.AttemptCount);
        Assert.Single(handler.CapturedRequests);

        var sentRequest = handler.CapturedRequests[0];
        Assert.Equal(HttpMethod.Put, sentRequest.Method);
        Assert.Equal("http://localhost:5223/api/employees/EMP-777", sentRequest.RequestUri?.ToString());
        Assert.True(sentRequest.Headers.Contains("Idempotency-Key"));
        Assert.Equal("evt-123", sentRequest.Headers.GetValues("Idempotency-Key").First());
    }

    [Fact]
    public async Task UpdateEmployeeAsync_TransientFailureThenSuccess_RetriesAndSucceeds()
    {
        var callCount = 0;
        var handler = new TestHttpMessageHandler(_ =>
        {
            callCount++;
            if (callCount < 3)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("Server temporarily unavailable")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"updated"}""")
            };
        });

        var client = new Employee360Client(new HttpClient(handler), FastOptions);

        var result = await client.UpdateEmployeeAsync("EMP-777", SampleRequest, "evt-retry");

        Assert.True(result.IsSuccess);
        Assert.Equal(DownstreamStatus.Success, result.Status);
        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(3, handler.CapturedRequests.Count);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_TooManyRequests429_RetriesAndSucceeds()
    {
        var callCount = 0;
        var handler = new TestHttpMessageHandler(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                var msg = new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("Rate limited")
                };
                msg.Headers.TryAddWithoutValidation("Retry-After", "0");
                return msg;
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new Employee360Client(new HttpClient(handler), FastOptions);

        var result = await client.UpdateEmployeeAsync("EMP-777", SampleRequest, "evt-429");

        Assert.True(result.IsSuccess);
        Assert.Equal(DownstreamStatus.Success, result.Status);
        Assert.Equal(2, result.AttemptCount);
        Assert.Equal(2, handler.CapturedRequests.Count);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_Permanent400BadRequest_DoesNotRetry()
    {
        var callCount = 0;
        var handler = new TestHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Invalid employee schema")
            };
        });

        var client = new Employee360Client(new HttpClient(handler), FastOptions);

        var result = await client.UpdateEmployeeAsync("EMP-777", SampleRequest, "evt-400");

        Assert.False(result.IsSuccess);
        Assert.Equal(DownstreamStatus.PermanentFailure, result.Status);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(1, result.AttemptCount);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_Permanent404NotFound_DoesNotRetry()
    {
        var callCount = 0;
        var handler = new TestHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Employee record does not exist")
            };
        });

        var client = new Employee360Client(new HttpClient(handler), FastOptions);

        var result = await client.UpdateEmployeeAsync("EMP-777", SampleRequest, "evt-404");

        Assert.False(result.IsSuccess);
        Assert.Equal(DownstreamStatus.PermanentFailure, result.Status);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(1, result.AttemptCount);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_TransientFailuresExhausted_ReturnsTransientFailure()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Persistent 503")
        });

        var client = new Employee360Client(new HttpClient(handler), FastOptions);

        var result = await client.UpdateEmployeeAsync("EMP-777", SampleRequest, "evt-503-exhausted");

        Assert.False(result.IsSuccess);
        Assert.Equal(DownstreamStatus.TransientFailure, result.Status);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(3, handler.CapturedRequests.Count);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_TimeoutOnAllAttempts_ReturnsUncertainWithoutClaimingSuccess()
    {
        var handler = new TimeoutHttpMessageHandler();
        var client = new Employee360Client(new HttpClient(handler), FastOptions);

        var result = await client.UpdateEmployeeAsync("EMP-777", SampleRequest, "evt-timeout");

        Assert.False(result.IsSuccess);
        Assert.Equal(DownstreamStatus.Uncertain, result.Status);
        Assert.Equal(3, result.AttemptCount);
        Assert.Contains("timed out", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public List<HttpRequestMessage> CapturedRequests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedRequests.Add(request);
            return Task.FromResult(_handler(request));
        }
    }

    private sealed class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        public int AttemptCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AttemptCount++;
            throw new OperationCanceledException("The request timed out.");
        }
    }
}
