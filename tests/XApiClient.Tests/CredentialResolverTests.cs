using XApiClient.CLI.Commands;
using XApiClient.CLI.Configuration;

namespace XApiClient.Tests;

public sealed class CredentialResolverTests
{
    [Fact]
    public void Resolve_ShouldApplyPrecedence_AppSettingsThenEnvironmentThenCli()
    {
        // Verifies credentials are resolved with strict source precedence.
        XApiSettings appSettings = new XApiSettings
        {
            ConsumerKey = "app-key",
            ConsumerSecret = "app-secret",
            AccessToken = "app-access",
            AccessTokenSecret = "app-access-secret",
            BearerToken = "app-bearer",
        };

        Dictionary<string, string?> env = new Dictionary<string, string?>
        {
            ["X_CONSUMER_KEY"] = "env-key",
            ["X_BEARER_TOKEN"] = "env-bearer",
        };

        GlobalOptions global = new GlobalOptions
        {
            ConsumerKey = "cli-key",
            AccessTokenSecret = "cli-access-secret",
        };

        var credentials = CredentialResolver.Resolve(appSettings, env, global);

        Assert.Equal("cli-key", credentials.ConsumerKey);
        Assert.Equal("app-secret", credentials.ConsumerSecret);
        Assert.Equal("app-access", credentials.AccessToken);
        Assert.Equal("cli-access-secret", credentials.AccessTokenSecret);
        Assert.Equal("env-bearer", credentials.BearerToken);
    }
}
