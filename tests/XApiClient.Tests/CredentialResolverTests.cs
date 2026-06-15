using XApiClient.CLI.Commands;
using XApiClient.CLI.Configuration;

namespace XApiClient.Tests;

public sealed class CredentialResolverTests
{
    private const string DummyClientSecret = "AppClientPlaceholder";
    private const string DummyAccessToken = "AppAccessPlaceholder";
    private const string DummyBearerToken = "AppBearerPlaceholder";

    [Fact]
    public void Resolve_ShouldApplyPrecedence_AppSettingsThenEnvironmentThenCli()
    {
        // Verifies OAuth2 credentials are resolved with strict source precedence.
        XApiSettings appSettings = new XApiSettings
        {
            ClientId = "app-client-id",
            ClientSecret = DummyClientSecret,
            AccessToken = DummyAccessToken,
            RefreshToken = "app-refresh",
            BearerToken = DummyBearerToken,
            Scopes = "app-scope",
        };

        Dictionary<string, string?> env = new Dictionary<string, string?>
        {
            ["X_CLIENT_ID"] = "env-client-id",
            ["X_ACCESS_TOKEN"] = "EnvAccessPlaceholder",
            ["X_BEARER_TOKEN"] = "EnvBearerPlaceholder",
        };

        GlobalOptions global = new GlobalOptions
        {
            ClientId = "cli-client-id",
            RefreshToken = "cli-refresh",
            Scopes = "cli-scope",
        };

        var credentials = CredentialResolver.Resolve(appSettings, env, global);

        Assert.Equal("cli-client-id", credentials.ClientId);
        Assert.Equal(DummyClientSecret, credentials.ClientSecret);
        Assert.Equal("EnvAccessPlaceholder", credentials.AccessToken);
        Assert.Equal("cli-refresh", credentials.RefreshToken);
        Assert.Equal("EnvBearerPlaceholder", credentials.BearerToken);
        Assert.Equal("cli-scope", credentials.Scopes);
    }
}
