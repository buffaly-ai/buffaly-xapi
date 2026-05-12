using XApiClient.Core.Models;

namespace XApiClient.Core.Services;

public sealed class SearchService : ISearchService
{
    private readonly XClient _client;

    public SearchService(XClient client)
    {
        _client = client;
    }

    public Task<XResponse<List<Tweet>>> SearchRecentAsync(
        string query,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        return _client.SearchRecentAsync(query, maxResults, paginationToken, cancellationToken);
    }
}
