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

### Milestone: Checkpoint 3 Correctness Review

- **Task:** Reviewed concurrent claiming, duplicate delivery, uncertain and failed outcomes, reset behaviour, EventId comparison, state transitions, and focused tests.
- **Engineering decisions:**
  - Kept the atomic `ConcurrentDictionary.TryAdd` claim path and added ordinal EventId comparison so opaque identifiers are not merged by case.
  - Prevented `TryReset` from removing `Received`, `Completed`, or `Uncertain` records. Only a terminal `Failed` record can be explicitly reset for replay; an uncertain outcome remains blocked because the downstream side effect may already have succeeded.
  - Rejected invalid transitions out of terminal states and added regression coverage for the lifecycle rules.
- **Verification:**
  - `dotnet build EmployeeIntegration.slnx` passed with 0 Warnings and 0 Errors.
  - `dotnet test EmployeeIntegration.slnx` passed (92 total tests: 92 passed, 0 failed, 0 skipped).
- **Remaining limitation:** Processing records remain in memory and are lost on application or process restart, so restart-safe deduplication is not provided.
