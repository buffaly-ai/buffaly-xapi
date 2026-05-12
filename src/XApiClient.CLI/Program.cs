using Microsoft.Extensions.Configuration;
using XApiClient.CLI.Commands;
using XApiClient.CLI.Configuration;
using XApiClient.CLI.Output;
using XApiClient.Core;
using XApiClient.Core.Models;
using XApiClient.Core.Services;

namespace XApiClient.CLI;

public sealed class Program
{
    private const int ExitSuccess = 0;
    private const int ExitArgumentError = 2;
    private const int ExitConfigError = 3;
    private const int ExitProviderError = 4;

    public static async Task<int> Main(string[] args)
    {
        ConsoleRenderer renderer = new ConsoleRenderer();
        CliParser parser = new CliParser();
        CliParseResult parsed = parser.Parse(args);

        if (parsed.IsHelp)
        {
            PrintUsage();
            return ExitSuccess;
        }

        if (!string.IsNullOrWhiteSpace(parsed.Error))
        {
            renderer.RenderError(parsed.Error);
            PrintUsage();
            return ExitArgumentError;
        }

        if (parsed.Command == null)
        {
            renderer.RenderError("No command parsed.");
            PrintUsage();
            return ExitArgumentError;
        }

        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            XApiSettings settings = configuration.GetSection("XApi").Get<XApiSettings>() ?? new XApiSettings();
            IReadOnlyDictionary<string, string?> environment = EnvironmentReader.ReadAll();
            XApiClient.Core.Authentication.XCredentials credentials = CredentialResolver.Resolve(
                settings,
                environment,
                parsed.GlobalOptions);

            if (!credentials.HasOAuth1UserContext() && !credentials.HasBearerToken())
            {
                renderer.RenderError("Missing credentials. Provide OAuth 1.0a keys or a bearer token.");
                return ExitConfigError;
            }

            using HttpClient httpClient = new HttpClient();
            XClient xClient = new XClient(httpClient, credentials, settings.BaseUrl);
            IUserService userService = new UserService(xClient);
            ITweetService tweetService = new TweetService(xClient);
            ISearchService searchService = new SearchService(xClient);

            return await ExecuteCommandAsync(parsed.Command, userService, tweetService, searchService, renderer);
        }
        catch (InvalidOperationException ex)
        {
            renderer.RenderError(ex.Message);
            return ExitConfigError;
        }
        catch (HttpRequestException ex)
        {
            string message = HttpRequestErrorFormatter.Format(ex);
            renderer.RenderError(message);
            return ExitProviderError;
        }
        catch (Exception ex)
        {
            renderer.RenderError(string.Concat("Unexpected error: ", ex.Message));
            return ExitProviderError;
        }
    }

    private static async Task<int> ExecuteCommandAsync(
        CliCommandOptions command,
        IUserService userService,
        ITweetService tweetService,
        ISearchService searchService,
        ConsoleRenderer renderer)
    {
        if (command.Kind == CliCommandKind.Me)
        {
            XResponse<User> me = await userService.GetMeAsync();
            if (command.Json)
            {
                Console.WriteLine(me.RawJson);
                return ExitSuccess;
            }

            if (me.Data == null)
            {
                renderer.RenderError("No user data returned.");
                return ExitProviderError;
            }

            renderer.RenderUser(me.Data);
            return ExitSuccess;
        }

        if (command.Kind == CliCommandKind.Post)
        {
            XResponse<Tweet> posted = await tweetService.PostTweetAsync(command.Text ?? string.Empty);
            if (command.Json)
            {
                Console.WriteLine(posted.RawJson);
                return ExitSuccess;
            }

            if (posted.Data == null)
            {
                renderer.RenderError("Tweet was not returned.");
                return ExitProviderError;
            }

            renderer.RenderMessage(string.Concat("Tweet posted with id ", posted.Data.Id ?? "(unknown)", "."));
            renderer.RenderTweets(new List<Tweet> { posted.Data });
            return ExitSuccess;
        }

        if (command.Kind == CliCommandKind.Search)
        {
            XResponse<List<Tweet>> search = await searchService.SearchRecentAsync(
                command.Text ?? string.Empty,
                command.Count);

            if (command.Json)
            {
                Console.WriteLine(search.RawJson);
                return ExitSuccess;
            }

            renderer.RenderTweets(search.Data ?? new List<Tweet>());
            return ExitSuccess;
        }

        string userId = await GetCurrentUserIdAsync(userService);

        if (command.Kind == CliCommandKind.Timeline)
        {
            XResponse<List<Tweet>> timeline = await tweetService.GetHomeTimelineAsync(userId, command.Count);
            if (command.Json)
            {
                Console.WriteLine(timeline.RawJson);
                return ExitSuccess;
            }

            renderer.RenderTweets(timeline.Data ?? new List<Tweet>());
            return ExitSuccess;
        }

        if (command.Kind == CliCommandKind.MyTweets)
        {
            XResponse<List<Tweet>> myTweets = await tweetService.GetMyTweetsAsync(userId, command.Count);
            if (command.Json)
            {
                Console.WriteLine(myTweets.RawJson);
                return ExitSuccess;
            }

            renderer.RenderTweets(myTweets.Data ?? new List<Tweet>());
            return ExitSuccess;
        }

        if (command.Kind == CliCommandKind.Mentions)
        {
            XResponse<List<Tweet>> mentions = await tweetService.GetMentionsAsync(userId, command.Count);
            if (command.Json)
            {
                Console.WriteLine(mentions.RawJson);
                return ExitSuccess;
            }

            renderer.RenderTweets(mentions.Data ?? new List<Tweet>());
            return ExitSuccess;
        }

        renderer.RenderError("Unhandled command.");
        return ExitArgumentError;
    }

    private static async Task<string> GetCurrentUserIdAsync(IUserService userService)
    {
        XResponse<User> me = await userService.GetMeAsync();
        string? userId = me.Data?.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Unable to resolve current user id from /2/users/me.");
        }

        return userId;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("XApiClient CLI");
        Console.WriteLine("Usage:");
        Console.WriteLine("  xcli me [--json]");
        Console.WriteLine("  xcli timeline [--count 20] [--json]");
        Console.WriteLine("  xcli mytweets [--count 15] [--json]");
        Console.WriteLine("  xcli mentions [--count 10] [--json]");
        Console.WriteLine("  xcli post \"Your tweet text\" [--json]");
        Console.WriteLine("  xcli search \"query\" [--count 20] [--json]");
        Console.WriteLine();
        Console.WriteLine("Global credential overrides:");
        Console.WriteLine("  --consumer-key <value>");
        Console.WriteLine("  --consumer-secret <value>");
        Console.WriteLine("  --access-token <value>");
        Console.WriteLine("  --access-token-secret <value>");
        Console.WriteLine("  --bearer-token <value>");
    }
}
