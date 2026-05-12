# Buffaly.XAPI Spec: X API v2 Client + CLI

## Goal
Build a production-ready .NET 8 solution that delivers:
- A reusable core library (`XApiClient.Core`) for X API v2.
- A terminal-first CLI (`XApiClient.CLI`) for profile/timeline/search/post workflows.
- Automated tests (`XApiClient.Tests`) that verify auth signing, request construction, and CLI/config behavior.

## Why This Exists
Current repo state only contains a prompt-style draft. This spec converts it into an executable implementation contract with concrete architecture, acceptance criteria, and testable outcomes.

## Scope
In-scope:
- OAuth 1.0a user-context signing support.
- Bearer-token fallback for read-oriented API calls when OAuth user tokens are absent.
- Strongly typed response models for tweets/users/expansions.
- Command-based CLI with JSON and human-readable output modes.
- Config precedence: `appsettings.json` -> environment -> CLI flags.
- Unit tests for core request/auth and CLI parsing/config resolution.

Out-of-scope:
- Streaming APIs.
- Media upload endpoint(s).
- Background workers and web host integration.
- Secret vault integration.

## Architecture
Solution layout:

```text
XApiClient.sln
├── AGENTS.md
├── README.md
├── docs/
│   └── specs.md
├── src/
│   ├── XApiClient.Core/
│   │   ├── Authentication/
│   │   ├── DTOs/
│   │   ├── Models/
│   │   ├── Services/
│   │   └── XClient.cs
│   └── XApiClient.CLI/
│       ├── Commands/
│       ├── Configuration/
│       ├── Output/
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    └── XApiClient.Tests/
```

## Functional Requirements
### FR-1 Endpoints
Implement these API operations:
1. `GET /2/users/me`
2. `GET /2/users/{id}/timelines/reverse_chronological`
3. `GET /2/users/{id}/tweets`
4. `GET /2/users/{id}/mentions`
5. `POST /2/tweets`
6. `GET /2/tweets/search/recent`

### FR-2 Data Models
Implement typed models for:
- `User`, `UserPublicMetrics`
- `Tweet`, `TweetMetrics`, `ReferencedTweet`, attachments/media
- `XIncludes`, `XMeta`, `XApiError`
- `XResponse<T>` wrapper with `RawJson` and `next_token` access

### FR-3 Auth Behavior
Auth strategy precedence:
1. OAuth 1.0a user context when all 4 OAuth user credentials are available.
2. Bearer token fallback (`BearerToken`) when OAuth user credentials are incomplete.
3. Fail fast with clear configuration error when neither mode is usable.

### FR-4 CLI Commands
Required commands:

```bash
xcli me
xcli timeline [--count 20] [--json]
xcli mytweets [--count 15] [--json]
xcli mentions [--count 10] [--json]
xcli post "tweet text"
xcli search "query text" [--count 20] [--json]
```

Supported global flags:
- `--consumer-key`
- `--consumer-secret`
- `--access-token`
- `--access-token-secret`
- `--bearer-token`
- `--help`

Supported command flags:
- `--count <int>`
- `--json`

### FR-5 Config Precedence
Resolve credentials in this exact order:
1. `XApi` section in `appsettings.json`
2. Environment variables (`X_CONSUMER_KEY`, `X_CONSUMER_SECRET`, `X_ACCESS_TOKEN`, `X_ACCESS_TOKEN_SECRET`, `X_BEARER_TOKEN`)
3. Command-line overrides

### FR-6 Error/Exit Code Contract
Exit codes:
- `0` success
- `2` argument/usage error
- `3` configuration/auth setup error
- `4` request/API failure

## Non-Functional Requirements
- Async I/O for all network calls.
- Deterministic unit tests (no live API requirement).
- No dependency on deprecated third-party Twitter libraries.
- Clear extension points (`IUserService`, `ITweetService`, `ISearchService`).

## Implementation Plan
### Phase 1 Spec + Standards
1. Replace draft `docs/specs.md` with implementation-grade spec.
2. Copy shared `AGENTS.md` baseline from `BuffalyNet6`.

### Phase 2 Core Library
1. Add OAuth signer and credentials model.
2. Add API DTO/models and envelope parsing.
3. Add `XClient` with endpoint methods and auth selection.
4. Add service interfaces + implementations.

### Phase 3 CLI
1. Add parser and command model.
2. Add config resolver with strict precedence.
3. Add renderer for profile/tweet output.
4. Implement command execution pipeline with exit codes.
5. Add `appsettings.json` credentials.

### Phase 4 Tests
1. OAuth signer deterministic header tests.
2. `XClient` request-path/auth/query/parse tests with fake `HttpMessageHandler`.
3. CLI parser tests.
4. Credential precedence tests.

## Test Strategy
Unit coverage targets:
- OAuth header fields and deterministic generation.
- Bearer fallback and request header assignment.
- Endpoint query construction and response parsing.
- Parser correctness for each command family.
- Source precedence for credentials.

## Acceptance Criteria
- `XApiClient.sln` builds.
- All tests in `XApiClient.Tests` pass.
- CLI commands parse and execute against core services.
- `docs/specs.md` is implementation-grade and no longer prompt text.
- Root `AGENTS.md` exists and mirrors shared baseline.

## Configuration Notes
`appsettings.json` stores local development credentials. For production/CI, prefer environment variables or a secret store.

## Risks
- X API auth mode constraints vary by account tier.
- Posting requires user-context permissions; bearer-only apps may be read-only.
- Rate limiting requires operational retry policy outside this initial scope.

