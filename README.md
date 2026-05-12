# XApiClient

`XApiClient` is a .NET 8 solution that provides:
- `XApiClient.Core`: reusable X API v2 client library.
- `XApiClient.CLI`: command-line workflow for profile, timelines, mentions, posting, and search.
- `XApiClient.Tests`: unit tests for auth signing, request behavior, and CLI/config parsing.

## Build

```powershell
dotnet restore XApiClient.sln
dotnet build XApiClient.sln --no-restore
```

## Test

```powershell
dotnet test XApiClient.sln --no-build
```

## CLI Usage

```powershell
dotnet run --project src/XApiClient.CLI -- me
dotnet run --project src/XApiClient.CLI -- timeline --count 20
dotnet run --project src/XApiClient.CLI -- mytweets --count 15 --json
dotnet run --project src/XApiClient.CLI -- mentions --count 10
dotnet run --project src/XApiClient.CLI -- post "Hello from XApiClient"
dotnet run --project src/XApiClient.CLI -- search "from:OpenAI has:links" --count 20
```

## Config Sources

Credentials load in this order:
1. `src/XApiClient.CLI/appsettings.json` (`XApi` section)
2. Environment variables:
   - `X_CONSUMER_KEY`
   - `X_CONSUMER_SECRET`
   - `X_ACCESS_TOKEN`
   - `X_ACCESS_TOKEN_SECRET`
   - `X_BEARER_TOKEN`
3. CLI overrides (`--consumer-key`, `--consumer-secret`, `--access-token`, `--access-token-secret`, `--bearer-token`)

## Notes

- `me`, `timeline`, `mytweets`, `mentions`, and `post` require user-context credentials.
- Provide all four OAuth 1.0a values for user-context auth:
  - `X_CONSUMER_KEY`
  - `X_CONSUMER_SECRET`
  - `X_ACCESS_TOKEN`
  - `X_ACCESS_TOKEN_SECRET`
- Bearer token mode is supported as a fallback for `search`.
## Licensing

Buffaly core is GPLv3 by default. If your organization needs different terms for proprietary use, redistribution, or supported deployment, contact us for commercial licensing.

Buffaly is developed by Matt Furnari.

See [LICENSING.md](LICENSING.md) and [CONTRIBUTING.md](CONTRIBUTING.md).

