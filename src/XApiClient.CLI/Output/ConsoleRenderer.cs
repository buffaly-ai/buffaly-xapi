using Spectre.Console;
using XApiClient.Core.Models;

namespace XApiClient.CLI.Output;

public sealed class ConsoleRenderer
{
    public void RenderUser(User user)
    {
        Table table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn("Field");
        table.AddColumn("Value");
        table.AddRow("Id", user.Id ?? string.Empty);
        table.AddRow("Name", user.Name ?? string.Empty);
        table.AddRow("Username", user.Username ?? string.Empty);
        table.AddRow("Description", user.Description ?? string.Empty);
        table.AddRow("Created At", user.CreatedAt?.ToString("u") ?? string.Empty);
        if (user.PublicMetrics != null)
        {
            table.AddRow("Followers", user.PublicMetrics.FollowersCount.ToString());
            table.AddRow("Following", user.PublicMetrics.FollowingCount.ToString());
            table.AddRow("Tweet Count", user.PublicMetrics.TweetCount.ToString());
        }

        AnsiConsole.Write(table);
    }

    public void RenderTweets(IReadOnlyList<Tweet> tweets)
    {
        Table table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn("Id");
        table.AddColumn("Author");
        table.AddColumn("Created");
        table.AddColumn("Text");
        table.AddColumn("Metrics");

        foreach (Tweet tweet in tweets)
        {
            string metrics = string.Empty;
            if (tweet.PublicMetrics != null)
            {
                metrics = string.Concat(
                    "L:", tweet.PublicMetrics.LikeCount.ToString(),
                    " R:", tweet.PublicMetrics.RetweetCount.ToString(),
                    " Rp:", tweet.PublicMetrics.ReplyCount.ToString());
            }

            table.AddRow(
                tweet.Id ?? string.Empty,
                tweet.AuthorId ?? string.Empty,
                tweet.CreatedAt?.ToString("u") ?? string.Empty,
                Truncate(tweet.Text, 100),
                metrics);
        }

        AnsiConsole.Write(table);
    }

    public void RenderMessage(string message)
    {
        AnsiConsole.MarkupLine("[green]" + EscapeMarkup(message) + "[/]");
    }

    public void RenderError(string message)
    {
        AnsiConsole.MarkupLine("[red]" + EscapeMarkup(message) + "[/]");
    }

    private static string Truncate(string? value, int length)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= length)
        {
            return value;
        }

        return string.Concat(value[..(length - 3)], "...");
    }

    private static string EscapeMarkup(string value)
    {
        return Markup.Escape(value);
    }
}
