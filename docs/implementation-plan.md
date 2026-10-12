# Development Plan and Scope Controls

## Purpose

This document defines the development sequence, scope boundaries, and verification requirements for Employee Integration.

The objective is to build a small, reliable, testable C#/.NET application that receives employee update events, validates and transforms them, and forwards updates to a downstream Employee 360 REST API.

## Rules for AI Coding Assistants

These rules apply to Antigravity, GitHub Copilot, ChatGPT, and any other coding assistant used on this project.

1. Read `AGENTS.md`, `SOURCE-OF-TRUTH.md`, `docs/architecture.md`, `docs/ai-usage.md`, and `README.md` before making changes. Consult this plan to identify the current development checkpoint.
2. Implement only the current checkpoint. Do not start future checkpoints or add unrelated features unless explicitly approved.
3. Inspect the existing solution before creating files, projects, interfaces, services, abstractions, or dependencies. Reuse existing components where appropriate.
4. Create a new file or project only when it serves a defined requirement and cannot reasonably be handled by the existing structure.
5. Do not add a user interface, authentication system, live D365 integration, message broker, or deployment infrastructure without explicit approval.
6. Do not introduce speculative event fields, invented mappings, unnecessary third-party packages, or unsupported requirements.
7. Keep changes small, testable, and consistent with the existing architecture.
8. Do not mark a task complete until its implementation has been inspected and the applicable verification has been performed.
9. Do not invent build results, test counts, runtime behaviour, or documentation claims.
10. Do not commit, push, deploy, delete files, or perform destructive Git operations without explicit user approval.

## Standard Checkpoint Workflow

Every checkpoint follows this sequence:

Plan → Inspect → Implement → Build → Test → Review the diff → Update relevant documentation → Report results and limitations → Obtain approval → Commit.

A failed check must be reported and investigated. Do not proceed as though verification succeeded.

## Checkpoint 1 — Foundation

**Status: Complete — implemented, built, tested, and reviewed.**

Goal: Establish a buildable solution and working local development environment.

- [ ] Confirm the available .NET SDK and Azure Functions Core Tools.
- [ ] Create the .NET solution and required projects.
- [ ] Establish project references and dependency boundaries.
- [ ] Configure the Azure Functions isolated-worker project.
- [ ] Establish the mock downstream API and automated test project structure.
- [ ] Add minimal local configuration without committing secrets.
- [ ] Run a minimal HTTP-triggered Function locally.
- [ ] Build the complete solution and run the available automated tests.
- [ ] Review the actual changes and update documentation with verified results.

Checkpoint 1 is complete only after the solution builds and the minimal Function has been run locally and verified.

Git initialisation and connection to the new GitHub repository will occur after Checkpoint 1 is complete and approved.

## Checkpoint 2 — Event Validation and Transformation

**Status: Complete — implemented, built, tested, and reviewed.**
Goal: Receive an employee update event, validate it, normalize supported values, and map it into the downstream Employee 360 contract.

- [ ] Define the source event and downstream request models.
- [ ] Validate required fields and reject malformed or unsafe input.
- [ ] Normalize supported location values, including `CPT` to `Cape Town`.
- [ ] Transform the source event into the downstream contract without inventing missing values.
- [ ] Implement the application processing flow and HTTP intake function.
- [ ] Add tests for valid input, invalid input, normalization, transformation, and processing behaviour.

The supported event contract must be based on the documented requirements and actual sample payloads.

## Checkpoint 3 — Idempotency and Persistence

**Status: Complete — implemented, built, tested, and reviewed.**

Goal: Prevent duplicate event delivery from producing unintended duplicate downstream effects and retain sufficient processing state.

- [x] Identify events using a stable event identifier.
- [x] Record processing status and information needed to recognise previously processed events.
- [x] Prevent duplicate side effects when an already-successful event is delivered again.
- [x] Define how processing behaves when an earlier attempt failed or its outcome is uncertain.
- [x] Add tests for repeated delivery and relevant state transitions.

Use the simplest persistence mechanism that satisfies the requirement and fits the existing architecture. Do not introduce a database server, distributed cache, or message broker without a demonstrated need.

## Checkpoint 4 — Downstream HTTP Reliability

**Status: Complete — implemented, built, tested, and reviewed.**

Goal: Call the mock Employee 360 API and handle downstream responses safely.

- [x] Configure the downstream HTTP client through the existing architecture.
- [x] Set appropriate timeouts.
- [x] Implement bounded retries for eligible transient failures.
- [x] Distinguish transient failures from permanent failures.
- [x] Ensure validation failures are not retried as transient downstream errors.
- [x] Extend the mock API only as needed to simulate successful responses and selected failures.
- [x] Add tests for successful calls, retry limits, and permanent-failure handling.

Retries must account for ambiguous outcomes. A timeout does not prove that the downstream service failed to apply an update. Prevent duplicate effects through idempotency or another justified mechanism.

## Checkpoint 5 — Event Ordering and Failure Recovery

**Status: Complete — implemented, built, tested, and reviewed.**

Goal: Handle outdated events and isolate failures without stopping unrelated processing.

- [x] Use the optional source `sequenceNumber` per employee; equal or lower sequences are recorded as stale and suppressed.
- [x] Do not assume timestamps alone reliably establish event order; events without sequence information use existing idempotency handling.
- [x] Store employee/version and failure details in processing records.
- [x] Expose event status and failed events for inspection.
- [x] Permit resetting failed events for replay by resubmitting the original event; uncertain outcomes remain blocked.
- [x] Keep event handling isolated per HTTP request so malformed or permanently failing events do not stop independent requests.

Inspection endpoints: `GET /api/events/{eventId}` and `GET /api/events/dead-letter`. Failed-event recovery is `POST /api/events/{eventId}/recovery`; it clears only a failed record, after which the caller must resubmit the original event with the same event ID. The in-memory store is volatile, and these capabilities are process-local rather than durable dead-letter storage.

Verification: `dotnet build EmployeeIntegration.slnx` succeeded with 0 warnings and 0 errors; `dotnet test EmployeeIntegration.slnx` passed 116 tests (0 failed, 0 skipped); `git diff --check` passed.

## Checkpoint 6 — End-to-End Verification

**Status: Complete — automated verification and local HTTP flows exercised.**

Goal: Verify the complete integration locally.

- [x] Valid event submitted to the local Function, transformed, and accepted by the mock API; confirmed via repeated idempotency key returning `already_processed` from the mock.
- [x] Duplicate delivery returned `Duplicate` without a second downstream update.
- [x] Invalid and malformed HTTP payloads returned HTTP 400; a later independent valid event succeeded.
- [x] `CPT` was transformed to `Cape Town` in the Function response and mock API's duplicate response.
- [x] Bounded transient retries verified by automated `Employee360ClientTests`; not exercised through the local Function HTTP flow (see limitation below).
- [x] Permanent downstream failure exercised through a temporary local 404 base path; processing recorded `Failed` and returned HTTP 500.
- [x] Sequence 99 after sequence 100 for the same employee returned `Stale` and was not sent downstream.
- [x] Event status, dead-letter listing, and recovery HTTP endpoints exercised; failed record was listed, reset, and then returned 404 to inspection.
- [x] A separate request was processed after the permanent failure and received its own failure result, demonstrating request isolation.
- [x] Automated build and test suite run; final diff check passed.

Verification results: .NET SDK 10.0.401 and Azure Functions Core Tools 4.7.0 were available. From the repository root, the mock API was started with `dotnet run --no-build --project Employee360.MockApi/Employee360.MockApi.csproj --launch-profile http`; Azurite was started with `azurite --silent --location /tmp/employee-integration-azurite --debug /tmp/employee-integration-azurite.log`; and from `EmployeeIntegration.Functions`, the Function host was started with `func start --port 7297`. The mock API ran on port 5223, Azurite satisfied `UseDevelopmentStorage=true`, and Functions ran on port 7297. `dotnet build EmployeeIntegration.slnx` passed with 0 warnings and 0 errors. `dotnet test EmployeeIntegration.slnx` passed 116 tests (116 passed, 0 failed, 0 skipped). `git diff --check` passed.

Limitation: The Function's downstream client does not forward the mock API's `X-Mock-Fail-Times`/`X-Mock-Status` test headers, so transient 503/retry behavior was verified through automated client tests, not through the real Function-to-mock HTTP path. The permanent failure and successful recovery endpoint were exercised by temporarily pointing a separate local Function host at a non-existent mock route; `local.settings.json` was restored byte-for-byte afterward. Processing status remains in-memory and resets when the Function host restarts.

## Checkpoint 7 — Project Readiness

**Status: Complete — documentation and local OpenAPI/Swagger walkthrough verified.**

Goal: Ensure the repository is understandable, runnable, and professionally presented.

- [x] Provide a README with verified prerequisites and local run instructions.
- [x] Document the actual configuration required to run the Function and mock API.
- [x] Ensure no credentials or secrets are committed.
- [x] Ensure documentation accurately reflects implemented functionality and known limitations.
- [x] Review project structure, naming, dependency direction, error handling, and testability.
- [x] Document a reasonable Azure evolution path without implementing unnecessary infrastructure.
- [x] Ensure the AI usage record accurately reflects meaningful assistance and verified outcomes.
- [x] Serve the OpenAPI document and local Swagger UI for the existing HTTP endpoints.

Verification: With Azurite, Employee360.MockApi, and the Functions host running simultaneously, the mock API listened on port 5223 and Functions on port 7297. The live Checkpoint 7 HTTP run submitted a valid event and received HTTP 200/Success with `officeLocation: Cape Town`; inspecting it returned HTTP 200 with state Completed and sequence 100. Resubmitting the same ID returned HTTP 200/Duplicate. Malformed JSON and a structurally invalid event each returned HTTP 400, and an independent valid event then returned HTTP 200/Success. Sequence 99 for the same employee after sequence 100 returned HTTP 200/Stale. Repeating the downstream PUT directly with the same idempotency key returned `already_processed`, `isDuplicate: true`, and the expected employee payload. The empty dead-letter endpoint returned HTTP 200 with `[]`. The earlier Checkpoint 6 live failure drill also exercised a permanent local 404, failed-event inspection/listing, recovery reset, and independent request handling; its result and settings restoration are recorded above. The Function host exposed `/api/openapi.json` (HTTP 200, valid OpenAPI 3.0.3 JSON listing the actual Function routes) and `/api/swagger` (HTTP 200); the bundle, standalone preset, and CSS each returned HTTP 200. Browser UI rendering/interaction was not visually confirmed: the browser automation runtime could not start, although the page and its resources were fetched successfully and a Codex browser panel was queued. The live Function client does not expose the mock's transient-failure controls, so transient retry behavior remains verified by automated client tests only.

Final verification from the confirmed repository directory: `dotnet build EmployeeIntegration.slnx` succeeded with 0 warnings and 0 errors; `dotnet test EmployeeIntegration.slnx` passed 116 tests (116 passed, 0 failed, 0 skipped); `git diff --check` passed.

The OpenAPI document is maintained as an embedded JSON resource and can drift from code if routes/contracts change. The processing store remains in-memory, and Checkpoint 7 does not add durable storage, authentication, deployment, or production API governance.

## Azure Evolution

Azure Functions or App Service can host the integration. Azure Service Bus can provide durable queueing and dead-letter handling where appropriate. Event Grid can distribute event notifications where its delivery model fits. Managed identity, Key Vault, and Application Insights can support secure access and operational monitoring.

API Management or Logic Apps should be considered only where governance or orchestration needs justify them.

These are architectural options, not mandatory implementation tasks.

## Scope and Completion Rules

- Prioritize a working integration over unnecessary features or documentation volume.
- Preserve the agreed project structure unless a concrete requirement justifies a change.
- Keep optional improvements separate from required functionality.
- Record unresolved assumptions and limitations explicitly.
- Do not claim that an unimplemented feature exists.
- Do not begin the next checkpoint until the current checkpoint has been reviewed and its results reported.
- Do not initialize Git or connect the GitHub repository until Checkpoint 1 has been completed and approved.
