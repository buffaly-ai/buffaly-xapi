namespace XApiClient.CLI.Commands;

public sealed class CliParseResult
{
    public bool IsHelp { get; set; }

    public string? Error { get; set; }

    public GlobalOptions GlobalOptions { get; set; } = new GlobalOptions();

    public CliCommandOptions? Command { get; set; }
}
