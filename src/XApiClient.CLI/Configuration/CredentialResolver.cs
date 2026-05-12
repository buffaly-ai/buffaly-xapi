using XApiClient.CLI.Commands;
using XApiClient.Core.Authentication;

namespace XApiClient.CLI.Configuration;

public static class CredentialResolver
{
    public static XCredentials Resolve(
        XApiSettings appSettings,
        IReadOnlyDictionary<string, string?> environmentValues,
        GlobalOptions globalOptions)
    {
        XCredentials credentials = new XCredentials
        {
            ConsumerKey = appSettings.ConsumerKey,
            ConsumerSecret = appSettings.ConsumerSecret,
            AccessToken = appSettings.AccessToken,
            AccessTokenSecret = appSettings.AccessTokenSecret,
            BearerToken = appSettings.BearerToken,
        };

        ApplyIfPresent(credentials, environmentValues, "X_CONSUMER_KEY", "ConsumerKey");
        ApplyIfPresent(credentials, environmentValues, "X_CONSUMER_SECRET", "ConsumerSecret");
        ApplyIfPresent(credentials, environmentValues, "X_ACCESS_TOKEN", "AccessToken");
        ApplyIfPresent(credentials, environmentValues, "X_ACCESS_TOKEN_SECRET", "AccessTokenSecret");
        ApplyIfPresent(credentials, environmentValues, "X_BEARER_TOKEN", "BearerToken");

        OverrideIfPresent(globalOptions.ConsumerKey, value => credentials.ConsumerKey = value);
        OverrideIfPresent(globalOptions.ConsumerSecret, value => credentials.ConsumerSecret = value);
        OverrideIfPresent(globalOptions.AccessToken, value => credentials.AccessToken = value);
        OverrideIfPresent(globalOptions.AccessTokenSecret, value => credentials.AccessTokenSecret = value);
        OverrideIfPresent(globalOptions.BearerToken, value => credentials.BearerToken = value);

        return credentials;
    }

    private static void ApplyIfPresent(
        XCredentials credentials,
        IReadOnlyDictionary<string, string?> environmentValues,
        string key,
        string targetProperty)
    {
        if (!environmentValues.TryGetValue(key, out string? value))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (targetProperty == "ConsumerKey")
        {
            credentials.ConsumerKey = value;
            return;
        }

        if (targetProperty == "ConsumerSecret")
        {
            credentials.ConsumerSecret = value;
            return;
        }

        if (targetProperty == "AccessToken")
        {
            credentials.AccessToken = value;
            return;
        }

        if (targetProperty == "AccessTokenSecret")
        {
            credentials.AccessTokenSecret = value;
            return;
        }

        if (targetProperty == "BearerToken")
        {
            credentials.BearerToken = value;
        }
    }

    private static void OverrideIfPresent(string? value, Action<string> apply)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        apply(value);
    }
}
