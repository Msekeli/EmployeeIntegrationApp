using System.Net;
using EmployeeIntegration.Application;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace EmployeeIntegration.Functions;

public sealed class EventInspectionFunction(IEventProcessingStore store)
{
    [Function("GetEventStatus")]
    public async Task<HttpResponseData> GetEventStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{eventId}")] HttpRequestData request,
        string eventId)
    {
        var record = store.Get(eventId);
        var response = request.CreateResponse(record is null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
        await response.WriteAsJsonAsync(record);
        return response;
    }

    [Function("GetDeadLetterEvents")]
    public async Task<HttpResponseData> GetDeadLetterEvents(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/dead-letter")] HttpRequestData request)
    {
        var records = store.GetAll().Where(record => record.State == ProcessingState.Failed).ToArray();
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(records);
        return response;
    }

    [Function("RecoverFailedEvent")]
    public async Task<HttpResponseData> RecoverFailedEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/{eventId}/recovery")] HttpRequestData request,
        string eventId)
    {
        var reset = store.TryReset(eventId);
        var response = request.CreateResponse(reset ? HttpStatusCode.OK : HttpStatusCode.Conflict);
        await response.WriteAsJsonAsync(new
        {
            eventId,
            reset,
            message = reset
                ? "Failed event state cleared. Resubmit the original event with the same eventId to replay it."
                : "Only an event in Failed state can be recovered. Uncertain outcomes require investigation."
        });
        return response;
    }
}
