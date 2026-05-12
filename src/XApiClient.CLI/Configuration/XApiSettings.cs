namespace XApiClient.CLI.Configuration;

public sealed class XApiSettings
{
    public string BaseUrl { get; set; } = "https://api.x.com";

    public string? ConsumerKey { get; set; }

    public string? ConsumerSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? AccessTokenSecret { get; set; }

    public string? BearerToken { get; set; }
}
