namespace XApiClient.Core.Authentication;

public sealed class XCredentials
{
    public string? ConsumerKey { get; set; }

    public string? ConsumerSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? AccessTokenSecret { get; set; }

    public string? BearerToken { get; set; }

    public bool HasOAuth1UserContext()
    {
        IReadOnlyList<string> missing = GetMissingOAuth1UserContextFields();
        return missing.Count == 0;
    }

    public bool HasBearerToken()
    {
        return !string.IsNullOrWhiteSpace(BearerToken);
    }

    public IReadOnlyList<string> GetMissingOAuth1UserContextFields()
    {
        List<string> missing = new List<string>();

        if (string.IsNullOrWhiteSpace(ConsumerKey))
        {
            missing.Add("consumer key");
        }

        if (string.IsNullOrWhiteSpace(ConsumerSecret))
        {
            missing.Add("consumer secret");
        }

        if (string.IsNullOrWhiteSpace(AccessToken))
        {
            missing.Add("access token");
        }

        if (string.IsNullOrWhiteSpace(AccessTokenSecret))
        {
            missing.Add("access token secret");
        }

        return missing;
    }
}
