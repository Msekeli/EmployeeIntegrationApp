using System.Net;
using System.Text.Json;
using EmployeeIntegration.Application;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace EmployeeIntegration.Functions;

public sealed class EmployeeUpdatedFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IEmployeeEventProcessor _processor;

    public EmployeeUpdatedFunction(IEmployeeEventProcessor processor)
    {
        _processor = processor;
    }

    [Function("EmployeeUpdated")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/employee-updated")]
        HttpRequestData request)
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        var result = await _processor.ProcessJsonAsync(body);

        var statusCode = result.Status switch
        {
            EventProcessingStatus.Success => HttpStatusCode.OK,
            EventProcessingStatus.Duplicate => HttpStatusCode.OK,
            EventProcessingStatus.Conflict => HttpStatusCode.Conflict,
            EventProcessingStatus.Uncertain => HttpStatusCode.InternalServerError,
            EventProcessingStatus.Failed => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.BadRequest
        };

        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");

        var responseJson = JsonSerializer.Serialize(result, JsonOptions);
        await response.WriteStringAsync(responseJson);

        return response;
    }
}
