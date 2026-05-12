namespace XApiClient.CLI.Commands;

public sealed class CliCommandOptions
{
    public CliCommandKind Kind { get; set; }

    public int Count { get; set; }

    public bool Json { get; set; }

    public string? Text { get; set; }
}
