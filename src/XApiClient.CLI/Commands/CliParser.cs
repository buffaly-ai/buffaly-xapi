namespace XApiClient.CLI.Commands;

public sealed class CliParser
{
    public CliParseResult Parse(string[] args)
    {
        CliParseResult result = new CliParseResult();
        GlobalOptions globalOptions = new GlobalOptions();
        List<string> filtered = new List<string>();

        for (int index = 0; index < args.Length; index++)
        {
            string token = args[index];
            if (token.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                result.IsHelp = true;
                result.GlobalOptions = globalOptions;
                return result;
            }

            if (IsGlobalOption(token))
            {
                if (index + 1 >= args.Length)
                {
                    result.Error = string.Concat("Missing value for ", token, ".");
                    result.GlobalOptions = globalOptions;
                    return result;
                }

                string value = args[index + 1];
                index++;
                AssignGlobalOption(globalOptions, token, value);
                continue;
            }

            filtered.Add(token);
        }

        result.GlobalOptions = globalOptions;
        if (filtered.Count == 0)
        {
            result.IsHelp = true;
            return result;
        }

        string commandToken = filtered[0].ToLowerInvariant();
        List<string> commandArgs = filtered.Skip(1).ToList();
        CliCommandOptions? command = ParseCommand(commandToken, commandArgs, out string? parseError);
        result.Command = command;
        result.Error = parseError;
        return result;
    }

    private static bool IsGlobalOption(string token)
    {
        if (token.Equals("--consumer-key", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (token.Equals("--consumer-secret", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (token.Equals("--access-token", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (token.Equals("--access-token-secret", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (token.Equals("--bearer-token", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static void AssignGlobalOption(GlobalOptions options, string token, string value)
    {
        if (token.Equals("--consumer-key", StringComparison.OrdinalIgnoreCase))
        {
            options.ConsumerKey = value;
            return;
        }

        if (token.Equals("--consumer-secret", StringComparison.OrdinalIgnoreCase))
        {
            options.ConsumerSecret = value;
            return;
        }

        if (token.Equals("--access-token", StringComparison.OrdinalIgnoreCase))
        {
            options.AccessToken = value;
            return;
        }

        if (token.Equals("--access-token-secret", StringComparison.OrdinalIgnoreCase))
        {
            options.AccessTokenSecret = value;
            return;
        }

        if (token.Equals("--bearer-token", StringComparison.OrdinalIgnoreCase))
        {
            options.BearerToken = value;
        }
    }

    private static CliCommandOptions? ParseCommand(
        string commandToken,
        List<string> args,
        out string? parseError)
    {
        parseError = null;
        CliCommandKind kind;
        int defaultCount;

        if (commandToken == "me")
        {
            kind = CliCommandKind.Me;
            defaultCount = 1;
        }
        else if (commandToken == "timeline")
        {
            kind = CliCommandKind.Timeline;
            defaultCount = 10;
        }
        else if (commandToken == "mytweets")
        {
            kind = CliCommandKind.MyTweets;
            defaultCount = 15;
        }
        else if (commandToken == "mentions")
        {
            kind = CliCommandKind.Mentions;
            defaultCount = 10;
        }
        else if (commandToken == "post")
        {
            kind = CliCommandKind.Post;
            defaultCount = 1;
        }
        else if (commandToken == "search")
        {
            kind = CliCommandKind.Search;
            defaultCount = 10;
        }
        else
        {
            parseError = string.Concat("Unknown command: ", commandToken, ".");
            return null;
        }

        int count = defaultCount;
        bool json = false;
        List<string> positional = new List<string>();

        for (int index = 0; index < args.Count; index++)
        {
            string token = args[index];
            if (token.Equals("--json", StringComparison.OrdinalIgnoreCase))
            {
                json = true;
                continue;
            }

            if (token.Equals("--count", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Count)
                {
                    parseError = "Missing value for --count.";
                    return null;
                }

                string rawValue = args[index + 1];
                if (!int.TryParse(rawValue, out int parsedCount) || parsedCount <= 0)
                {
                    parseError = "Invalid --count value. Use a positive integer.";
                    return null;
                }

                count = parsedCount;
                index++;
                continue;
            }

            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                parseError = string.Concat("Unknown option: ", token, ".");
                return null;
            }

            positional.Add(token);
        }

        string? text = null;
        if (kind == CliCommandKind.Post || kind == CliCommandKind.Search)
        {
            if (positional.Count == 0)
            {
                parseError = kind == CliCommandKind.Post
                    ? "The post command requires tweet text."
                    : "The search command requires a query string.";
                return null;
            }

            text = string.Join(' ', positional);
        }
        else if (positional.Count > 0)
        {
            parseError = "Unexpected positional argument(s).";
            return null;
        }

        CliCommandOptions command = new CliCommandOptions
        {
            Kind = kind,
            Count = count,
            Json = json,
            Text = text,
        };

        return command;
    }
}
