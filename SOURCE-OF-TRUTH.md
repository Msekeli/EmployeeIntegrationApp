# Project Source of Truth

## Purpose

Employee Integration is a C#/.NET application that receives employee update events, validates and transforms employee data, and reliably forwards updates to a downstream Employee 360 REST API.

The application prioritises clear separation of concerns, predictable behaviour, maintainability, and reliable event processing.

## Functional Requirements

### 1. Event Intake

- Receive `EmployeeUpdated` events through an HTTP endpoint.
- Support a clear, documented event contract.
- Handle malformed payloads without unhandled application errors.

### 2. Validation and Transformation

- Validate event structure and required fields.
- Reject or isolate invalid or unsafe data.
- Normalise supported values, including `CPT` to `Cape Town`.
- Transform valid events into the downstream Employee 360 request contract.
- Do not invent source fields, mappings, or business rules.

### 3. Downstream Communication

- Send valid employee updates to the downstream REST API.
- Handle HTTP responses, timeouts, and transient failures.
- Apply bounded retries only where appropriate.
- Distinguish transient failures from permanent failures.
- Account for uncertain outcomes so retries do not cause unintended duplicate effects.

### 4. Idempotency and Event Ordering

- Identify events using a stable event identifier.
- Prevent repeated delivery of successfully processed events from causing unintended duplicate effects.
- Protect newer employee data from stale updates when reliable ordering information is available.
- Do not assume timestamps alone reliably establish event order.

### 5. Failure Handling and Recovery

- Record processing outcomes and sufficient failure context for investigation.
- Make event status and relevant failure information available for inspection.
- Support safe recovery or replay where appropriate.
- Ensure one invalid or failing event does not prevent unrelated events from being processed.

### 6. Operational Visibility

- Provide at least one useful endpoint for inspecting event status, employee data, or application health.
- Return clear and consistent responses for successful, invalid, duplicate, and failed processing outcomes.

### 7. Automated Testing

- Test validation and transformation rules.
- Test malformed and invalid events.
- Test duplicate delivery and event-order handling where supported.
- Test downstream success, transient failures, retry limits, and permanent failures.
- Keep tests focused on meaningful application behaviour. Exhaustive test coverage is not required.

## Architecture Principles

- Use C#/.NET and Azure Functions with the isolated worker model.
- Maintain clear separation between the Domain, Application, Functions, and Infrastructure layers.
- Keep domain rules independent of HTTP clients, persistence frameworks, and Azure Functions.
- Keep the solution small, testable, and straightforward to extend.
- Introduce projects, packages, interfaces, and abstractions only when justified by an actual requirement.
- Prefer explicit, understandable behaviour over unnecessary frameworks or speculative features.

## Approved Implementation Decisions

The following are approved design choices for this project. They describe the intended implementation and do not imply that the functionality has already been implemented or verified.

- Use an HTTP-triggered Azure Function for event intake.
- Use a separate, minimal Employee 360 mock API for local integration testing.
- Keep the layered solution structure without a `src/` directory.
- Use the simplest persistence approach that adequately supports processing status, idempotency, and recovery.
- Keep configuration separate from application code.
- Never commit credentials or secrets.

These decisions must not be changed unilaterally. If a demonstrated requirement, technical limitation, or conflict makes a change necessary, explain the evidence, impact, and proposed alternative, then obtain the user's explicit approval before proceeding.

## AI Agent Scope Control

The original technical challenge and the approved project documents define the requirements and boundaries of this application.

AI coding agents must:

- Follow the current implementation checkpoint in `docs/implementation-plan.md` and implement only work authorised for that checkpoint.
- Treat this document as the functional source of truth and `docs/architecture.md` as the approved architecture reference.
- Never invent requirements, event fields, downstream mappings, business rules, or expected behaviour.
- Never add features, projects, packages, services, infrastructure, or abstractions merely because they are conventional, interesting, or potentially useful.
- Never change approved requirements, architecture, dependencies, or implementation decisions without the user's explicit approval.
- Inspect the existing code and configuration before proposing changes, and reuse existing implementations where appropriate.
- If requirements conflict, essential contract details are missing, or a proposed solution would expand scope, stop and explain the issue. Ask the user for clarification or approval rather than guessing.
- Distinguish clearly between planned, implemented, and verified functionality. Never carry implementation status or test results over from another repository without independently verifying them.
- Prefer the smallest understandable implementation that demonstrates the required behaviour. The assessment does not require exhaustive tests, live Azure infrastructure, or production deployment.

## Scope Boundaries

The core application focuses on employee event processing and downstream REST integration.

The following are outside the current implementation scope unless explicitly approved:

- Integration with a live D365 environment.
- A user interface.
- Full Azure Service Bus or Event Grid infrastructure.
- Azure deployment and production infrastructure provisioning.
- Additional services or abstractions that do not support a defined requirement.

These boundaries keep the application focused without preventing future extension.

## Production Evolution

A production deployment could use Azure Functions or App Service for hosting, Azure Service Bus for durable message processing and dead-letter handling, and Event Grid where event distribution is appropriate.

Managed identity and Key Vault can support secure access. Application Insights can provide monitoring and diagnostics. API Management and Logic Apps can be introduced where API governance or orchestration needs justify them.

Production architecture should be driven by reliability, security, operational requirements, and expected scale rather than by adding services without a clear purpose.
