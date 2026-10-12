# Agent Instructions

## Required Reading

Before changing code, read:

- `SOURCE-OF-TRUTH.md`
- `docs/architecture.md`
- `docs/implementation-plan.md`
- `docs/ai-usage.md`
- `README.md`

Use the implementation plan to identify the current checkpoint. Follow the source of truth, architecture, and existing code when making implementation decisions.

## Technology and Architecture

- Use C#/.NET 10 and Azure Functions .NET isolated worker.
- Keep the solution small, testable, and easy to extend. Do not introduce a `src/` directory.
- Keep Domain independent of Azure Functions, Infrastructure, persistence frameworks, and HTTP clients.
- Keep project dependencies consistent with `docs/architecture.md`.
- Avoid unnecessary NuGet packages, projects, interfaces, abstractions, and configuration.
- Reuse existing code where appropriate instead of creating duplicate implementations.

## Scope Control

- Implement only the current checkpoint in `docs/implementation-plan.md`.
- Do not begin later checkpoints or add unrelated features without explicit user approval.
- Before creating a file, project, service, interface, or dependency, establish why it is needed for the current requirement.
- Do not add speculative event fields, invented mappings, unnecessary frameworks, a user interface, real D365 integration, message brokers, or deployment infrastructure unless explicitly approved.
- Do not implement Service Bus or Event Grid integration or deploy to Azure as part of the current local integration scope.
- Prefer the simplest implementation that satisfies the documented requirements and can be explained and tested.
- If a requirement is ambiguous or an architectural change would materially increase scope, explain the issue and ask before proceeding.

## Implementation and Verification

For each checkpoint, follow:

Plan → Inspect → Implement → Build → Test → Review diff → Update documentation → Report results → Obtain approval → Commit.

- Inspect the existing implementation before modifying it.
- Keep changes focused on the current task.
- Run relevant build and test commands after implementation.
- Report failures, limitations, and unverified behaviour honestly.
- Update `docs/ai-usage.md` with meaningful AI-assisted work and decisions, using actual verification results.
- Keep documentation consistent with the implementation. Do not claim planned or incomplete functionality is already implemented.

## Git and Repository Safety

- Never commit automatically.
- Never push, deploy, delete files, rewrite Git history, or perform other destructive repository operations without explicit user approval.
- Before proposing a commit, report the changed files, verification results, outstanding issues, and proposed commit message.
- Preserve unrelated user changes and uncommitted work.
- Do not stage unrelated files or include private material in public commits.

## Reporting

At the end of each checkpoint, report:

1. What was implemented.
2. Which files were created or changed and why.
3. Which build and test commands were run and their actual results.
4. Any outstanding issues or limitations.
5. The proposed next step and commit message.

Wait for explicit approval before committing or proceeding with work that requires a new scope decision.
