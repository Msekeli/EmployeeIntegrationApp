# AI Usage and Development Record

## Purpose

This document records how AI tools contribute to the development of Employee Integration. It provides a consistent approach for documenting AI-assisted work, engineering decisions, and verification results.

These guidelines apply to all AI coding assistants used on the project, including Antigravity, GitHub Copilot, and ChatGPT.

## Guidelines for AI-Assisted Development

- Treat `SOURCE-OF-TRUTH.md`, architecture documentation, repository instructions, and existing code as the primary references for development decisions.
- Understand the existing implementation before proposing or changing code.
- Keep changes focused on the current development objective.
- Avoid unnecessary dependencies, abstractions, and speculative features.
- Do not invent requirements, payload fields, mappings, test results, or implementation details.
- Review generated code for correctness, consistency, maintainability, and compatibility with the existing solution.
- Never claim that code builds, tests pass, or functionality works unless the relevant verification has actually been performed.
- Do not commit, push, deploy, or perform destructive repository operations without explicit approval.

## Recording AI Assistance

For each significant development milestone, record:

1. **Task:** What AI helped implement, investigate, or review.
2. **Engineering decisions:** What was accepted, modified, challenged, or rejected, and why.
3. **Verification:** The commands executed, their actual results, and any outstanding issues.

Record meaningful contributions rather than every prompt or minor interaction. Distinguish between AI-generated suggestions, decisions made by the developer, and outcomes verified through testing or inspection.

## Verification and Accuracy

Verification results must be recorded only after the relevant commands or checks have been executed against the corresponding version of the code.

Do not predetermine test counts, build outcomes, or other results in planning documentation. Update this record as implementation progresses and verified evidence becomes available.

If verification has not been performed, state that explicitly. If a check fails, record the failure and its status rather than presenting the work as complete.

## Development Milestone Record

### Milestone: Checkpoint 5 — Event Ordering and Recovery

- **Task:** Added per-employee `sequenceNumber` ordering to event claims, stale-event outcomes, event inspection, failed-event listing, and explicit failed-state recovery for caller-driven replay.
- **Engineering decisions:** Sequence numbers are compared when ordering information and employee identity are present. Equal or lower numbers are stale; timestamps are not used. Failed events may be reset and resubmitted; uncertain outcomes remain blocked because the downstream side effect may have occurred. No queue or external persistence was added.
- **Verification:** `dotnet build EmployeeIntegration.slnx` passed with 0 warnings and 0 errors. `dotnet test EmployeeIntegration.slnx` passed (116 total: 116 passed, 0 failed, 0 skipped). `git diff --check` passed.
- **Limitations:** State and dead-letter inspection are in-memory and volatile. Replay requires the caller to resubmit the original payload.

Add entries as meaningful implementation milestones are completed.

### Milestone: Checkpoint 3 — Idempotency and Persistence

- **Milestone:** Stable event deduplication and processing state management (Checkpoint 3).
- **AI assistance:**
  - Designed and implemented processing lifecycle state models (`ProcessingState`, `EventProcessingRecord`, `ProcessingClaimResult`).
  - Defined the application-level state management contract (`IEventProcessingStore`) in `EmployeeIntegration.Application`.
  - Implemented thread-safe `InMemoryEventProcessingStore` using `ConcurrentDictionary` in `EmployeeIntegration.Infrastructure`.
  - Integrated deduplication and atomic claim checking in `EmployeeEventProcessor` to prevent concurrent duplicate processing and repeated side-effects.
  - Implemented transparent failure and uncertainty handling to avoid claiming exactly-once delivery.
  - Updated Azure Function (`EmployeeUpdatedFunction`) HTTP response status code mappings for duplicate, conflict, and uncertain states.
  - Added focused automated tests for first delivery, duplicate delivery, concurrency control, independent event IDs, and state transitions.
- **Decisions:**
  - Rejected introducing an external database, Redis cache, or message broker, adhering strictly to the architecture requirement for simplest persistence.
  - Maintained dependency inversion: `IEventProcessingStore` defined in `EmployeeIntegration.Application`; concrete `InMemoryEventProcessingStore` implemented in `EmployeeIntegration.Infrastructure`.
  - Explicitly avoided claiming "exactly-once delivery". Modeled at-least-once delivery with idempotent deduplication and distinct `Uncertain`, `Failed`, and `Conflict` states.
  - Mapped `Duplicate` to HTTP 200 OK with explicit duplicate payload to acknowledge safely settled events without provoking client retries, and mapped concurrent duplicate claims to HTTP 409 Conflict.
- **Verification:**
  - Solution build: `dotnet build EmployeeIntegration.slnx` passed with 0 Warnings and 0 Errors.
  - Test suite: `dotnet test EmployeeIntegration.slnx` passed (97 total tests: 97 passed, 0 failed, 0 skipped).
- **Outstanding work / Limitations:**
  - In-memory persistence is volatile and resets upon application or process restart. A persistent store (e.g. Azure Table/Blob/Cosmos) would be required for restart durability in production.
  - Downstream HTTP integration and transient retries are deferred to Checkpoint 4.

### Milestone: Checkpoint 3 Correctness Review

- **Task:** Reviewed concurrent claiming, duplicate delivery, uncertain and failed outcomes, reset behaviour, EventId comparison, state transitions, and focused tests.
- **Engineering decisions:**
  - Kept the atomic `ConcurrentDictionary.TryAdd` claim path and added ordinal EventId comparison so opaque identifiers are not merged by case.
  - Prevented `TryReset` from removing `Received`, `Completed`, or `Uncertain` records. Only a terminal `Failed` record can be explicitly reset for replay; an uncertain outcome remains blocked because the downstream side effect may already have succeeded.
  - Rejected invalid transitions out of terminal states and added regression coverage for the lifecycle rules.
- **Verification:**
  - `dotnet build EmployeeIntegration.slnx` passed with 0 Warnings and 0 Errors.
  - `dotnet test EmployeeIntegration.slnx` passed (97 total tests: 97 passed, 0 failed, 0 skipped).
- **Remaining limitation:** Processing records remain in memory and are lost on application or process restart, so restart-safe deduplication is not provided.

### Milestone: Checkpoint 4 — Downstream HTTP Reliability

- **Task:** Implemented downstream REST client integration for Employee 360 with bounded retries, explicit timeouts, transparent transient/permanent error handling, idempotency key forwarding, and mock API contract alignment.
- **Engineering decisions:**
  - Designed `IEmployee360Client` and `DownstreamResult` in `EmployeeIntegration.Application`, keeping all HTTP and retry logic encapsulated in `EmployeeIntegration.Infrastructure/Employee360Client.cs`.
  - Avoided third-party retry libraries (no Polly dependency), implementing bounded retries directly in pure C#/.NET 10.
  - Handled timeouts and network dropouts honestly as `DownstreamStatus.Uncertain` and recorded state as `ProcessingState.Uncertain` without falsely claiming completion or assuming the update was unapplied.
  - Treated HTTP 429 and 5xx (500, 502, 503, 504) as eligible transient failures with bounded retry and backoff (respecting `Retry-After` on 429), while treating 4xx client errors (400, 404) as permanent failures without retry.
  - Forwarded `Idempotency-Key: {eventId}` on downstream requests; extended `Employee360.MockApi/Controllers/EmployeesController.cs` to track keys and avoid re-applying updates on duplicate deliveries.
  - Aligned `EmployeesController` request contract with `Employee360UpdateRequest` and added simulation capabilities (`X-Mock-Status`, `X-Mock-Fail-Times`) for automated testing.
  - Configured downstream client base URL, timeout, and max retries via `Employee360ClientOptions` in `local.settings.json` and registered typed client with `AddHttpClient` in Functions DI.
- **Verification:**
  - `dotnet build EmployeeIntegration.slnx` passed with 0 Warnings and 0 Errors.
  - `dotnet test EmployeeIntegration.slnx` passed (112 total tests: 112 passed, 0 failed, 0 skipped).
  - Verified successful delivery, bounded retries on 503 and 429, permanent failure on 400 and 404 without retries, timeout honest uncertainty handling, downstream idempotency key forwarding, and mock API idempotency deduplication.
- **Remaining limitation:** In-memory tracking in both the integration app and mock API is volatile and will reset on application restart. Event ordering (e.g. sequence numbers) and dead-letter/replay handling are deferred to Checkpoint 5.
