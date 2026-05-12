# Agent Guidelines

- When you fix any error in this repository, consult `Errors.md` at the repo root to see previously resolved issues.
- After fixing an error, append a bullet point to `Errors.md` describing the error, how you fixed it, and any generalized lesson that could help avoid similar issues.
- Only update `Errors.md` when resolving real bugs, such as C# exceptions or JavaScript errors. Routine commits that do not address an error should **not** modify the log.
- Never use expression-bodied members; always use block bodies (`{ }`) instead.

# Testing Guidelines

- Separate request/response shaping logic into pure, deterministic helpers so tests do not require network calls or external services.
- Keep unit tests self-contained by providing minimal configuration in-memory; do not depend on appsettings files or environment state.
- Tag integration tests explicitly (for example, `TestCategory("Integration")`) and exclude them from default test runs.
- Document every test with a brief purpose comment describing the behavior under test and the scenario it covers.
- Prefer testing observable behavior (inputs to outputs) over internal implementation details, unless a regression requires internal verification.
- When changing API payload shapes, add tests for both happy paths and known failure modes to prevent regressions.
