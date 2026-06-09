namespace XApiClient.Core.Authentication;

public sealed class XCredentials
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public string? Scopes { get; set; }

    public string? BearerToken { get; set; }

    public bool HasOAuth2UserContext()
    {
        return !string.IsNullOrWhiteSpace(GetBearerAccessToken());
    }

    public string GetRequiredBearerAccessToken()
    {
        string token = GetBearerAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("OAuth 2.0 user-context bearer access token is required. Provide AccessToken, or BearerToken as a compatibility alias.");
        }

        return token;
    }

    public string GetBearerAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(AccessToken))
        {
            return AccessToken!.Trim();
        }

        if (!string.IsNullOrWhiteSpace(BearerToken))
        {
            return BearerToken!.Trim();
        }

        return string.Empty;
    }
}
