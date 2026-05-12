using XApiClient.Core.Models;

namespace XApiClient.Core.Services;

public interface ITweetService
{
    Task<XResponse<List<Tweet>>> GetHomeTimelineAsync(
        string userId,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default);

    Task<XResponse<List<Tweet>>> GetMyTweetsAsync(
        string userId,
        int maxResults = 15,
        string? paginationToken = null,
        CancellationToken cancellationToken = default);

    Task<XResponse<List<Tweet>>> GetMentionsAsync(
        string userId,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default);

    Task<XResponse<Tweet>> PostTweetAsync(string text, CancellationToken cancellationToken = default);
}
