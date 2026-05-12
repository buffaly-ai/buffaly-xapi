namespace XApiClient.CLI.Commands;

public sealed class GlobalOptions
{
    public string? ConsumerKey { get; set; }

    public string? ConsumerSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? AccessTokenSecret { get; set; }

    public string? BearerToken { get; set; }
}
