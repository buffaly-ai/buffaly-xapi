using XApiClient.Core.Models;

namespace XApiClient.Core.Services;

public sealed class TweetService : ITweetService
{
    private readonly XClient _client;

    public TweetService(XClient client)
    {
        _client = client;
    }

    public Task<XResponse<List<Tweet>>> GetHomeTimelineAsync(
        string userId,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        return _client.GetHomeTimelineAsync(userId, maxResults, paginationToken, cancellationToken);
    }

    public Task<XResponse<List<Tweet>>> GetMyTweetsAsync(
        string userId,
        int maxResults = 15,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        return _client.GetMyTweetsAsync(userId, maxResults, paginationToken, cancellationToken);
    }

    public Task<XResponse<List<Tweet>>> GetMentionsAsync(
        string userId,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        return _client.GetMentionsAsync(userId, maxResults, paginationToken, cancellationToken);
    }

    public Task<XResponse<Tweet>> PostTweetAsync(string text, CancellationToken cancellationToken = default)
    {
        return _client.PostTweetAsync(text, cancellationToken);
    }
}
