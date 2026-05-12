using XApiClient.CLI.Commands;

namespace XApiClient.Tests;

public sealed class CliParserTests
{
    [Fact]
    public void Parse_ShouldParseTimelineCommand_WithCountJsonAndGlobalCredentialOverride()
    {
        // Validates command option parsing and global credential override extraction.
        CliParser parser = new CliParser();
        string[] args = new[]
        {
            "timeline",
            "--count",
            "20",
            "--json",
            "--consumer-key",
            "ck-override",
        };

        CliParseResult result = parser.Parse(args);

        Assert.Null(result.Error);
        Assert.NotNull(result.Command);
        Assert.Equal(CliCommandKind.Timeline, result.Command!.Kind);
        Assert.Equal(20, result.Command.Count);
        Assert.True(result.Command.Json);
        Assert.Equal("ck-override", result.GlobalOptions.ConsumerKey);
    }

    [Fact]
    public void Parse_ShouldJoinSearchTextAcrossTokens()
    {
        // Confirms free-form search queries are preserved as one value for API calls.
        CliParser parser = new CliParser();
        string[] args = new[]
        {
            "search",
            "from:openai",
            "since:2026-02-01",
            "--count",
            "10",
        };

        CliParseResult result = parser.Parse(args);

        Assert.Null(result.Error);
        Assert.NotNull(result.Command);
        Assert.Equal(CliCommandKind.Search, result.Command!.Kind);
        Assert.Equal("from:openai since:2026-02-01", result.Command.Text);
    }
}
