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

**Status: Not started.**

Goal: Call the mock Employee 360 API and handle downstream responses safely.

- [ ] Configure the downstream HTTP client through the existing architecture.
- [ ] Set appropriate timeouts.
- [ ] Implement bounded retries for eligible transient failures.
- [ ] Distinguish transient failures from permanent failures.
- [ ] Ensure validation failures are not retried as transient downstream errors.
- [ ] Extend the mock API only as needed to simulate successful responses and selected failures.
- [ ] Add tests for successful calls, retry limits, and permanent-failure handling.

Retries must account for ambiguous outcomes. A timeout does not prove that the downstream service failed to apply an update. Prevent duplicate effects through idempotency or another justified mechanism.

## Checkpoint 5 — Event Ordering and Failure Recovery

**Status: Not started.**

Goal: Handle outdated events and isolate failures without stopping unrelated processing.

- [ ] Use a trustworthy source version or sequence number when one exists.
- [ ] Do not assume timestamps alone reliably establish event order.
- [ ] Document limitations when the source does not provide sufficient ordering information.
- [ ] Store sufficient failure context for investigation.
- [ ] Expose relevant event status or failure information through an appropriate inspection endpoint.
- [ ] Provide safe replay or recovery behaviour where justified.
- [ ] Ensure a malformed or permanently failing event does not prevent independent valid events from being processed.

## Checkpoint 6 — End-to-End Verification

**Status: Not started.**

Goal: Verify the complete integration locally.

- [ ] Verify that a valid event is received, transformed, and sent to the mock API.
- [ ] Verify that duplicate delivery does not produce unintended duplicate effects.
- [ ] Verify that invalid or malformed input is rejected or isolated appropriately.
- [ ] Verify supported location normalization.
- [ ] Verify bounded handling of temporary downstream failures.
- [ ] Verify permanent failures are distinguished from transient failures.
- [ ] Verify stale-event handling when reliable ordering information is available.
- [ ] Verify that processing status or failure details can be inspected.
- [ ] Verify that a failure in one event does not prevent unrelated events from being processed.
- [ ] Run the automated tests, build the solution, exercise the local HTTP flow, and review the final changes.

Record actual results and remaining limitations.

## Checkpoint 7 — Project Readiness

**Status: Not started.**

Goal: Ensure the repository is understandable, runnable, and professionally presented.

- [ ] Provide a README with verified prerequisites and local run instructions.
- [ ] Document the actual configuration required to run the Function and mock API.
- [ ] Ensure no credentials or secrets are committed.
- [ ] Ensure documentation accurately reflects implemented functionality and known limitations.
- [ ] Review project structure, naming, dependency direction, error handling, and testability.
- [ ] Document a reasonable Azure evolution path without implementing unnecessary infrastructure.
- [ ] Ensure the AI usage record accurately reflects meaningful assistance and verified outcomes.

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
