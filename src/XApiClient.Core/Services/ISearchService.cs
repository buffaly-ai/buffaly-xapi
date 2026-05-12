using XApiClient.Core.Models;

namespace XApiClient.Core.Services;

public interface ISearchService
{
    Task<XResponse<List<Tweet>>> SearchRecentAsync(
        string query,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default);
}
