using XApiClient.Core.Authentication;

namespace XApiClient.Tests;

public sealed class OAuth1AuthenticatorTests
{
    [Fact]
    public void CreateAuthorizationHeader_ShouldBeDeterministic_ForFixedNonceAndTimestamp()
    {
        // Verifies deterministic OAuth header output for regression-safe signature behavior.
        XCredentials credentials = new XCredentials
        {
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
            AccessToken = "access-token",
            AccessTokenSecret = "access-secret",
        };

        OAuth1Authenticator authenticator = new OAuth1Authenticator(
            () => "fixed-nonce",
            () => DateTimeOffset.FromUnixTimeSeconds(1700000000));

        Dictionary<string, string?> query = new Dictionary<string, string?>
        {
            ["max_results"] = "20",
            ["expansions"] = "author_id",
        };

        Uri uri = new Uri("https://api.x.com/2/users/123/tweets");
        string first = authenticator.CreateAuthorizationHeader(HttpMethod.Get, uri, query, null, credentials);
        string second = authenticator.CreateAuthorizationHeader(HttpMethod.Get, uri, query, null, credentials);

        Assert.Equal(first, second);
        Assert.StartsWith("OAuth ", first, StringComparison.Ordinal);
        Assert.Contains("oauth_nonce=\"fixed-nonce\"", first, StringComparison.Ordinal);
        Assert.Contains("oauth_timestamp=\"1700000000\"", first, StringComparison.Ordinal);
        Assert.Contains("oauth_signature=\"", first, StringComparison.Ordinal);
    }
}
