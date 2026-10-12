# Employee Integration

A .NET 10 Azure Functions isolated-worker application that validates and transforms employee update events, then forwards them to the local Employee360 mock API. Event processing state is held in memory and resets when the Function host restarts.

## Projects

- `EmployeeIntegration.Functions` — HTTP event intake, health, inspection, recovery, OpenAPI, and Swagger UI.
- `EmployeeIntegration.Application` — event processing and downstream client contracts.
- `EmployeeIntegration.Domain` — event validation and Employee360 transformation.
- `EmployeeIntegration.Infrastructure` — downstream HTTP client and in-memory processing store.
- `EmployeeIntegration.Tests` — automated tests.
- `Employee360.MockApi` — local mock downstream REST API.

Project boundaries and dependencies are described in [docs/architecture.md](docs/architecture.md). No real D365, Azure deployment, queue, or durable store is included.

## Prerequisites

- .NET SDK 10 (verified with 10.0.401).
- Azure Functions Core Tools v4 (verified with 4.7.0).
- Azurite, because `EmployeeIntegration.Functions/local.settings.json` uses `AzureWebJobsStorage=UseDevelopmentStorage=true`.

The checked-in local settings file contains only local runtime and mock API values; no credentials are required. It configures the Functions worker, the mock base URL, a 5-second HTTP timeout, and 3 total downstream attempts (initial request plus retries).

## Run locally

Keep three terminals open from the repository root unless a directory change is shown.

From the repository root, build the solution once before starting the processes:

```bash
dotnet build EmployeeIntegration.slnx
```

**Terminal 1 — Azurite:**

```bash
azurite --silent --location /tmp/employee-integration-azurite --debug /tmp/employee-integration-azurite.log
```

**Terminal 2 — Employee360 mock API:**

```bash
dotnet run --no-build --project Employee360.MockApi/Employee360.MockApi.csproj --launch-profile http
```

This launch profile listens on `http://localhost:5223`.

**Terminal 3 — Azure Functions:**

```bash
cd EmployeeIntegration.Functions
func start --port 7297
```

The verified Functions host listens on `http://localhost:7297`. Core Tools builds the Functions project during startup. Start Azurite and the mock API before the Function host.

## Local URLs

- Swagger UI: [http://localhost:7297/api/swagger](http://localhost:7297/api/swagger)
- OpenAPI 3 document: [http://localhost:7297/api/openapi.json](http://localhost:7297/api/openapi.json)
- Health: [http://localhost:7297/api/health](http://localhost:7297/api/health)
- Event intake: `POST http://localhost:7297/api/events/employee-updated`
- Event status: `GET http://localhost:7297/api/events/{eventId}`
- Failed events: `GET http://localhost:7297/api/events/dead-letter`
- Failed-event recovery: `POST http://localhost:7297/api/events/{eventId}/recovery`
- Mock API update route: `PUT http://localhost:5223/api/employees/{employeeId}`

The Swagger UI and its CSS/JavaScript assets are served locally from the Functions app; they do not depend on a CDN.

## Submit an event

The source event requires `eventId`, `eventType: "EmployeeUpdated"`, an `effectiveDate` or `timestamp`, and employee ID, first name, last name, and department. Email is optional but must be valid if supplied. Employee fields may be nested under `data` or supplied flat. `sequenceNumber` is optional and supports per-employee ordering.

```bash
curl -i http://localhost:7297/api/events/employee-updated \
  -H 'Content-Type: application/json' \
  -d '{
    "eventId": "evt-demo-100",
    "eventType": "EmployeeUpdated",
    "timestamp": "2026-10-12T07:00:00Z",
    "sequenceNumber": 100,
    "data": {
      "employeeId": "EMP-DEMO-100",
      "firstName": "Ada",
      "lastName": "Lovelace",
      "email": "ada@example.test",
      "department": "Engineering",
      "jobTitle": "Engineer",
      "officeCode": "CPT"
    }
  }'
```

A successful response includes the transformed Employee360 request; `officeLocation` becomes `Cape Town` for `CPT`. Duplicate and stale events return HTTP 200 with their respective status. Invalid or malformed requests return HTTP 400. Downstream failures return HTTP 500; an in-progress conflict returns HTTP 409.

Inspect the event and failed-event list:

```bash
curl -i http://localhost:7297/api/events/evt-demo-100
curl -i http://localhost:7297/api/events/dead-letter
```

Only `Failed` records can be reset for caller-driven replay. After recovery, resubmit the original event with the same event ID. `Uncertain` outcomes cannot be reset because the downstream side effect may already have happened. The store is volatile, so restarting the Function host clears all records.

## Suggested technical walkthrough

1. Open Swagger UI and show the documented paths and example event.
2. Submit the sample event; show the success response and `Cape Town` normalization.
3. Submit it again with the same event ID; show `Duplicate`.
4. Submit malformed JSON and an event missing required fields; show HTTP 400. Then submit a separate valid event.
5. Submit sequence 100 and then 99 for the same employee; show the second event is `Stale`.
6. For a permanent-failure demonstration, temporarily change `Employee360:BaseUrl` in the ignored local settings file to `http://localhost:5223/no-such-prefix/` before starting Functions. Submit a valid event, inspect it and the dead-letter list, then POST to its `/recovery` route. Restore the original base URL before the next normal run. This local 404 simulation is not a real Employee360 failure.
7. Explain that transient retry limits are covered by automated client tests; the Function client does not forward the mock API's failure-simulation headers.

Run automated verification from the repository root:

```bash
dotnet build EmployeeIntegration.slnx
dotnet test EmployeeIntegration.slnx
```

For current known limitations and verified checkpoint results, see [docs/implementation-plan.md](docs/implementation-plan.md) and [docs/ai-usage.md](docs/ai-usage.md).

Implementation status and future scope will be updated as development progresses.
