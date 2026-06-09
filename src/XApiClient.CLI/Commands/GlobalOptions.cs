namespace XApiClient.CLI.Commands;

public sealed class GlobalOptions
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public string? BearerToken { get; set; }

    public string? Scopes { get; set; }
}
