# Employee Integration

A C#/.NET application for receiving employee update events, validating and transforming employee data, and forwarding updates to a downstream Employee 360 REST API.

The project focuses on clear architecture, reliable event processing, maintainable code, and automated testing.

**Implementation status:** Checkpoint 1 has not yet been verified in this repository. Features described in the project documentation represent intended functionality unless confirmed by the implementation and tests.

## Solution Structure

The planned solution consists of the following projects:

- `EmployeeIntegration.Functions` — Azure Functions using the .NET isolated worker model.
- `EmployeeIntegration.Application` — application use cases and processing orchestration.
- `EmployeeIntegration.Domain` — core business rules, models, and validation.
- `EmployeeIntegration.Infrastructure` — external system communication and persistence implementations.
- `EmployeeIntegration.Tests` — automated tests.
- `Employee360.MockApi` — a separate mock Employee 360 REST API for local integration testing.

The solution uses a layered architecture without a `src/` directory. Project boundaries and dependencies are documented in [`docs/architecture.md`](docs/architecture.md).

## Prerequisites

The planned local development environment requires:

- .NET 10 SDK.
- Azure Functions Core Tools v4.
- Visual Studio Code or another compatible editor.

Additional runtime requirements will be documented when the local configuration has been implemented and verified.

## Getting Started

Setup and execution instructions will be added after the solution structure, configuration, and local startup process have been verified.

Do not assume that planned endpoints or processing capabilities are available until they have been implemented and tested.

## Verification

Build and test commands will be confirmed once the solution and test projects have been created.

Verification results will be recorded against the actual code version. A successful build or test run will not be claimed before the relevant command has been executed.

## Documentation

- [`SOURCE-OF-TRUTH.md`](SOURCE-OF-TRUTH.md) — project purpose, functional requirements, and scope.
- [`docs/architecture.md`](docs/architecture.md) — solution structure, dependency boundaries, and intended processing flow.
- [`docs/implementation-plan.md`](docs/implementation-plan.md) — development checkpoints and verification requirements.
- [`docs/ai-usage.md`](docs/ai-usage.md) — AI-assisted development practices and verified milestone records.
- [`AGENTS.md`](AGENTS.md) — instructions for AI coding assistants.

## Scope

The current scope is local employee-event processing and downstream REST integration, supported by automated tests.

A live D365 integration, user interface, Azure deployment, and full Service Bus or Event Grid infrastructure are outside the current scope unless explicitly approved.

Implementation status and future scope will be updated as development progresses.
