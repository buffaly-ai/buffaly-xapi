# Sanitized Export Report

Source: C:\dev\Buffaly.XAPI
Destination: C:\dev\buffaly-ai\buffaly-xapi
Solution: 
Included files: 49
Excluded files: 521
Included allowed binaries: 0
Manual review candidates: 10
Secret pattern hits: 0 after sanitizing src\\XApiClient.CLI\\appsettings.json credentials to empty placeholders

## Included Allowed Binaries
None.

## Manual Review Candidates
- src\XApiClient.CLI\appsettings.json
- src\XApiClient.CLI\Configuration\CredentialResolver.cs
- src\XApiClient.CLI\Output\HttpRequestErrorFormatter.cs
- src\XApiClient.Core\Authentication\OAuth1Authenticator.cs
- src\XApiClient.Core\Authentication\XCredentials.cs
- src\XApiClient.Core\DTOs\PostTweetRequest.cs
- src\XApiClient.Core\Models\XResponse.cs
- tests\XApiClient.Tests\CredentialResolverTests.cs
- tests\XApiClient.Tests\HttpRequestErrorFormatterTests.cs
- tests\XApiClient.Tests\OAuth1AuthenticatorTests.cs

## Secret Pattern Hits
No literal credentials remain after manual sanitation of X API appsettings placeholders.

## AttributionCheck
Before commit/push, run: powershell -NoProfile -ExecutionPolicy Bypass -File C:\\dev\\buffaly-ai\\scripts\\Test-PrePushAttribution.ps1 -RepoRoot <repo-root>

