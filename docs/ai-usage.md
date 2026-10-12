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

Each entry should include:

- **Milestone:** The feature or development objective.
- **AI assistance:** The work AI contributed to.
- **Decisions:** Important decisions reviewed, changed, accepted, or rejected.
- **Verification:** Actual build, test, or manual verification results.
- **Outstanding work:** Known limitations or follow-up items, if applicable.

This document should remain an accurate record of the development process. Update it when new evidence becomes available, and do not rewrite history to imply that decisions or results were known before they occurred.
