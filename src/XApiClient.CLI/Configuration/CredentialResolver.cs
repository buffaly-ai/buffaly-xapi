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
            ClientId = appSettings.ClientId,
            ClientSecret = appSettings.ClientSecret,
            AccessToken = appSettings.AccessToken,
            RefreshToken = appSettings.RefreshToken,
            BearerToken = appSettings.BearerToken,
            Scopes = appSettings.Scopes,
        };

        ApplyIfPresent(environmentValues, "X_CLIENT_ID", value => credentials.ClientId = value);
        ApplyIfPresent(environmentValues, "X_CLIENT_SECRET", value => credentials.ClientSecret = value);
        ApplyIfPresent(environmentValues, "X_ACCESS_TOKEN", value => credentials.AccessToken = value);
        ApplyIfPresent(environmentValues, "X_REFRESH_TOKEN", value => credentials.RefreshToken = value);
        ApplyIfPresent(environmentValues, "X_BEARER_TOKEN", value => credentials.BearerToken = value);
        ApplyIfPresent(environmentValues, "X_SCOPES", value => credentials.Scopes = value);

        OverrideIfPresent(globalOptions.ClientId, value => credentials.ClientId = value);
        OverrideIfPresent(globalOptions.ClientSecret, value => credentials.ClientSecret = value);
        OverrideIfPresent(globalOptions.AccessToken, value => credentials.AccessToken = value);
        OverrideIfPresent(globalOptions.RefreshToken, value => credentials.RefreshToken = value);
        OverrideIfPresent(globalOptions.BearerToken, value => credentials.BearerToken = value);
        OverrideIfPresent(globalOptions.Scopes, value => credentials.Scopes = value);

        return credentials;
    }

    private static void ApplyIfPresent(
        IReadOnlyDictionary<string, string?> environmentValues,
        string key,
        Action<string> apply)
    {
        if (!environmentValues.TryGetValue(key, out string? value))
        {
            return;
        }

        OverrideIfPresent(value, apply);
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
