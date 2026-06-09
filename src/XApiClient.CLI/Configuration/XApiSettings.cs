namespace XApiClient.CLI.Configuration;

public sealed class XApiSettings
{
    public string BaseUrl { get; set; } = "https://api.x.com";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public string? BearerToken { get; set; }

    public string? Scopes { get; set; }
}
