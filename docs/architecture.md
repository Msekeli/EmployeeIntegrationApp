# Architecture

## Projects

- `EmployeeIntegration.Functions` — Azure Functions .NET isolated worker for HTTP event intake and inspection.
- `EmployeeIntegration.Application` — application use cases and processing orchestration.
- `EmployeeIntegration.Domain` — core event and employee rules, models, and validation.
- `EmployeeIntegration.Infrastructure` — implementations for external systems and persistence.
- `EmployeeIntegration.Tests` — automated tests.
- `Employee360.MockApi` — a separate, minimal downstream REST API for local integration testing.

The solution does not use a `src/` directory. Keep the number of projects and abstractions proportionate to actual requirements.

## Dependencies

- Functions calls Application.
- Application depends on Domain and defines the interfaces required by its use cases.
- Infrastructure implements Application-defined interfaces for external systems and persistence.
- Domain remains independent of Azure Functions, persistence frameworks, and HTTP clients.

Dependencies should point inward toward the core business rules.

## Event Flow

1. An HTTP-triggered Function receives an employee update event.
2. Application validates the event, normalizes supported values, and transforms it into the downstream contract.
3. Duplicate detection and event-order checks protect against repeated or outdated updates where reliable identifiers or ordering information are available.
4. Infrastructure sends the transformed request to the Employee 360 API, using bounded retries for eligible transient failures.
5. Processing outcomes and relevant failure details can be recorded and exposed through inspection and recovery capabilities.

This flow describes the intended processing design. The implementation status of each capability must be verified against the code and tests. Planned functionality must not be presented as completed.

## Local API Documentation

The Functions project serves a hand-maintained OpenAPI 3.0.3 document at `/api/openapi.json` and Swagger UI at `/api/swagger`. The document is embedded in the Functions assembly, and the Swagger UI JavaScript and CSS are served from embedded package assets so local use does not depend on a CDN. Keep the document aligned with the actual HTTP routes and request/response contracts when those change. These routes support local development and do not add authentication or production API governance.

## Reliability and Data Handling

- Use stable event identifiers for duplicate detection.
- Use a trustworthy source version or sequence number for ordering when available. Timestamps alone may not establish event order reliably.
- Handle ambiguous downstream timeouts carefully. Retrying a request may duplicate its effects unless the downstream system supports idempotency.
- Do not retry permanent validation failures as though they were transient errors.
- Bound retries and define how exhausted retries and permanent failures are handled.
- Avoid logging secrets or unnecessary personal data.
- Keep processing status and failure details sufficient for operational inspection without exposing sensitive information.

## Potential Azure Deployment Architecture

The HTTP-triggered Function supports local testing and controlled event intake.

For a production deployment, Azure Service Bus could provide durable queueing, message settlement, and dead-letter handling. Azure Event Grid could distribute event notifications where its delivery model fits the integration requirements.

Managed identity and Azure Key Vault can support secure access to downstream services and secrets. Application Insights can provide telemetry and operational visibility.

Azure API Management or Logic Apps should be introduced only where API governance or additional orchestration justifies the complexity.

The deployment architecture should be selected according to operational, security, reliability, and scale requirements rather than adding Azure services without a demonstrated need.
