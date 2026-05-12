using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using XApiClient.Core.Authentication;
using XApiClient.Core.DTOs;
using XApiClient.Core.Models;

namespace XApiClient.Core;

public sealed class XClient
{
    private const string DefaultBaseUrl = "https://api.x.com";
    private const string TweetFields = "id,text,author_id,conversation_id,created_at,lang,public_metrics,referenced_tweets,attachments";
    private const string UserFields = "id,name,username,description,created_at,public_metrics";
    private const string MediaFields = "media_key,type,url,preview_image_url,width,height";
    private const string Expansions = "author_id,referenced_tweets.id,attachments.media_keys";

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly XCredentials _credentials;
    private readonly OAuth1Authenticator _oauth1Authenticator;

    public XClient(
        HttpClient httpClient,
        XCredentials credentials,
        string? baseUrl = null,
        OAuth1Authenticator? oauth1Authenticator = null)
    {
        _httpClient = httpClient;
        _credentials = credentials;
        _oauth1Authenticator = oauth1Authenticator ?? new OAuth1Authenticator();

        string effectiveBaseUrl = DefaultBaseUrl;
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            effectiveBaseUrl = baseUrl;
        }

        _httpClient.BaseAddress = new Uri(effectiveBaseUrl);
    }

    public Task<XResponse<User>> GetMeAsync(CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["user.fields"] = UserFields,
        };

        return SendAsync<User>(HttpMethod.Get, "/2/users/me", query, null, requiresUserContext: true, cancellationToken);
    }

    public async Task<string> GetMeRawJsonAsync(CancellationToken cancellationToken = default)
    {
        XResponse<User> response = await GetMeAsync(cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }

    public async Task<string> GetMyUserIdAsync(CancellationToken cancellationToken = default)
    {
        XResponse<User> response = await GetMeAsync(cancellationToken).ConfigureAwait(false);
        if (response.Data == null)
        {
            return string.Empty;
        }

        if (response.Data.Id == null)
        {
            return string.Empty;
        }

        return response.Data.Id;
    }

    public Task<XResponse<List<Tweet>>> GetHomeTimelineAsync(
        string userId,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> query = BuildTimelineQuery(maxResults, paginationToken);
        string path = string.Concat("/2/users/", userId, "/timelines/reverse_chronological");
        return SendAsync<List<Tweet>>(HttpMethod.Get, path, query, null, requiresUserContext: true, cancellationToken);
    }

    public async Task<string> GetMyHomeTimelineRawJsonAsync(
        string userId,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        XResponse<List<Tweet>> response = await GetHomeTimelineAsync(userId, maxResults, paginationToken, cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }

    public Task<XResponse<List<Tweet>>> GetMyTweetsAsync(
        string userId,
        int maxResults = 15,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> query = BuildTimelineQuery(maxResults, paginationToken);
        string path = string.Concat("/2/users/", userId, "/tweets");
        return SendAsync<List<Tweet>>(HttpMethod.Get, path, query, null, requiresUserContext: true, cancellationToken);
    }

    public async Task<string> GetMyTweetsRawJsonAsync(
        string userId,
        int maxResults = 15,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        XResponse<List<Tweet>> response = await GetMyTweetsAsync(userId, maxResults, paginationToken, cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }

    public Task<XResponse<List<Tweet>>> GetMentionsAsync(
        string userId,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> query = BuildTimelineQuery(maxResults, paginationToken);
        string path = string.Concat("/2/users/", userId, "/mentions");
        return SendAsync<List<Tweet>>(HttpMethod.Get, path, query, null, requiresUserContext: true, cancellationToken);
    }

    public async Task<string> GetMyMentionsRawJsonAsync(
        string userId,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        XResponse<List<Tweet>> response = await GetMentionsAsync(userId, maxResults, paginationToken, cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }

    public Task<XResponse<Tweet>> PostTweetAsync(string text, CancellationToken cancellationToken = default)
    {
        PostTweetRequest payload = new PostTweetRequest
        {
            Text = text,
        };

        return SendAsync<Tweet>(HttpMethod.Post, "/2/tweets", null, payload, requiresUserContext: true, cancellationToken);
    }

    public async Task<string> PostTweetRawJsonAsync(string text, CancellationToken cancellationToken = default)
    {
        XResponse<Tweet> response = await PostTweetAsync(text, cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }

    public Task<XResponse<List<Tweet>>> SearchRecentAsync(
        string queryText,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> query = BuildTimelineQuery(maxResults, paginationToken);
        query["query"] = queryText;

        return SendAsync<List<Tweet>>(HttpMethod.Get, "/2/tweets/search/recent", query, null, requiresUserContext: false, cancellationToken);
    }

    public async Task<string> SearchRecentRawJsonAsync(
        string queryText,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        XResponse<List<Tweet>> response = await SearchRecentAsync(queryText, maxResults, paginationToken, cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }

    private async Task<XResponse<T>> SendAsync<T>(
        HttpMethod httpMethod,
        string path,
        IReadOnlyDictionary<string, string?>? queryParameters,
        object? body,
        bool requiresUserContext,
        CancellationToken cancellationToken)
    {
        Uri uri = BuildUri(path, queryParameters);
        HttpRequestMessage request = new HttpRequestMessage(httpMethod, uri);
        ApplyAuthentication(request, httpMethod, uri, queryParameters, requiresUserContext);

        if (body != null)
        {
            string bodyJson = JsonSerializer.Serialize(body, SerializerOptions);
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string rawJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string message = string.Concat(
                "X API request failed with status ",
                ((int)response.StatusCode).ToString(),
                " (",
                response.StatusCode.ToString(),
                "). Body: ",
                rawJson);
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        ApiEnvelope<T>? envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(rawJson, SerializerOptions);
        if (envelope == null)
        {
            envelope = new ApiEnvelope<T>();
        }

        XResponse<T> result = new XResponse<T>(
            envelope.Data,
            envelope.Includes,
            envelope.Meta,
            envelope.Errors,
            rawJson);

        return result;
    }

    private void ApplyAuthentication(
        HttpRequestMessage request,
        HttpMethod httpMethod,
        Uri uri,
        IReadOnlyDictionary<string, string?>? queryParameters,
        bool requiresUserContext)
    {
        if (_credentials.HasOAuth1UserContext())
        {
            string oauthHeader = _oauth1Authenticator.CreateAuthorizationHeader(
                httpMethod,
                uri,
                queryParameters,
                null,
                _credentials);

            request.Headers.TryAddWithoutValidation("Authorization", oauthHeader);
            return;
        }

        if (!requiresUserContext && _credentials.HasBearerToken())
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _credentials.BearerToken);
            return;
        }

        if (requiresUserContext)
        {
            IReadOnlyList<string> missingFields = _credentials.GetMissingOAuth1UserContextFields();
            string missingDescription = string.Join(", ", missingFields);
            string message = string.Concat(
                "This endpoint requires user-context authentication. Provide OAuth 1.0a credentials (consumer key, consumer secret, access token, access token secret). Missing: ",
                missingDescription,
                ".");
            throw new InvalidOperationException(message);
        }

        throw new InvalidOperationException("No usable credentials found. Provide OAuth 1.0a keys or a bearer token.");
    }

    private Uri BuildUri(string path, IReadOnlyDictionary<string, string?>? queryParameters)
    {
        string queryString = BuildQueryString(queryParameters);
        string fullPath = path;
        if (!string.IsNullOrEmpty(queryString))
        {
            fullPath = string.Concat(path, "?", queryString);
        }

        Uri uri = new Uri(_httpClient.BaseAddress!, fullPath);
        return uri;
    }

    private static string BuildQueryString(IReadOnlyDictionary<string, string?>? queryParameters)
    {
        if (queryParameters == null || queryParameters.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        foreach (KeyValuePair<string, string?> pair in queryParameters)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            if (pair.Value == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(pair.Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(pair.Value));
        }

        return builder.ToString();
    }

    private static Dictionary<string, string?> BuildTimelineQuery(int maxResults, string? paginationToken)
    {
        Dictionary<string, string?> query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["max_results"] = maxResults.ToString(),
            ["tweet.fields"] = TweetFields,
            ["user.fields"] = UserFields,
            ["media.fields"] = MediaFields,
            ["expansions"] = Expansions,
        };

        if (!string.IsNullOrWhiteSpace(paginationToken))
        {
            query["pagination_token"] = paginationToken;
        }

        return query;
    }
}
