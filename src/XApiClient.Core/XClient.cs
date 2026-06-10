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
    private const string MediaUploadPath = "/2/media/upload";
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

    public XClient(
        HttpClient httpClient,
        XCredentials credentials,
        string? baseUrl = null)
    {
        _httpClient = httpClient;
        _credentials = credentials;

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

        return SendAsync<User>(HttpMethod.Get, "/2/users/me", query, null, cancellationToken);
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
        return SendAsync<List<Tweet>>(HttpMethod.Get, path, query, null, cancellationToken);
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
        return SendAsync<List<Tweet>>(HttpMethod.Get, path, query, null, cancellationToken);
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
        return SendAsync<List<Tweet>>(HttpMethod.Get, path, query, null, cancellationToken);
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

        return SendAsync<Tweet>(HttpMethod.Post, "/2/tweets", null, payload, cancellationToken);
    }

    public async Task<string> PostTweetRawJsonAsync(string text, CancellationToken cancellationToken = default)
    {
        XResponse<Tweet> response = await PostTweetAsync(text, cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }

    public async Task<XResponse<Tweet>> PostTweetWithMediaFileAsync(
        string text,
        string mediaFilePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Tweet text is required.", nameof(text));
        }

        string mediaId = await UploadMediaFileAsync(mediaFilePath, cancellationToken).ConfigureAwait(false);
        PostTweetRequest payload = new PostTweetRequest
        {
            Text = text,
            Media = new PostTweetMediaRequest
            {
                MediaIds = new[] { mediaId },
            },
        };

        return await SendAsync<Tweet>(HttpMethod.Post, "/2/tweets", null, payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> PostTweetWithMediaFileRawJsonAsync(
        string text,
        string mediaFilePath,
        CancellationToken cancellationToken = default)
    {
        XResponse<Tweet> response = await PostTweetWithMediaFileAsync(text, mediaFilePath, cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }

    public async Task<string> UploadMediaFileAsync(string mediaFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mediaFilePath))
        {
            throw new ArgumentException("Media file path is required.", nameof(mediaFilePath));
        }

        if (!File.Exists(mediaFilePath))
        {
            throw new FileNotFoundException("Media file was not found.", mediaFilePath);
        }

        Uri uri = BuildUri(MediaUploadPath, null);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
        ApplyOAuth2BearerAuthentication(request);

        await using FileStream fileStream = File.OpenRead(mediaFilePath);
        using StreamContent mediaContent = new StreamContent(fileStream);
        mediaContent.Headers.ContentType = new MediaTypeHeaderValue(GetMediaContentType(mediaFilePath));
        using MultipartFormDataContent form = new MultipartFormDataContent();
        form.Add(mediaContent, "media", Path.GetFileName(mediaFilePath));
        form.Add(new StringContent(GetMediaCategory(mediaFilePath), Encoding.UTF8), "media_category");
        form.Add(new StringContent(GetMediaContentType(mediaFilePath), Encoding.UTF8), "media_type");
        request.Content = form;

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string rawJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            string message = BuildFailureMessage("X media upload request failed", response, rawJson);
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        using JsonDocument document = JsonDocument.Parse(rawJson);
        if (document.RootElement.TryGetProperty("data", out JsonElement dataElement))
        {
            if (dataElement.TryGetProperty("id", out JsonElement idElement))
            {
                string? mediaId = idElement.GetString();
                if (!string.IsNullOrWhiteSpace(mediaId))
                {
                    return mediaId;
                }
            }

            if (dataElement.TryGetProperty("media_id", out JsonElement mediaIdElement))
            {
                string? mediaId = mediaIdElement.GetString();
                if (!string.IsNullOrWhiteSpace(mediaId))
                {
                    return mediaId;
                }
            }
        }

        if (document.RootElement.TryGetProperty("media_id_string", out JsonElement legacyMediaIdElement))
        {
            string? mediaId = legacyMediaIdElement.GetString();
            if (!string.IsNullOrWhiteSpace(mediaId))
            {
                return mediaId;
            }
        }

        throw new InvalidOperationException("X media upload response did not include a media id.");
    }

    public Task<XResponse<List<Tweet>>> SearchRecentAsync(
        string queryText,
        int maxResults = 10,
        string? paginationToken = null,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> query = BuildTimelineQuery(maxResults, paginationToken);
        query["query"] = queryText;

        return SendAsync<List<Tweet>>(HttpMethod.Get, "/2/tweets/search/recent", query, null, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        Uri uri = BuildUri(path, queryParameters);
        HttpRequestMessage request = new HttpRequestMessage(httpMethod, uri);
        ApplyOAuth2BearerAuthentication(request);

        if (body != null)
        {
            string bodyJson = JsonSerializer.Serialize(body, SerializerOptions);
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string rawJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string message = BuildFailureMessage("X API request failed", response, rawJson);
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

    private void ApplyOAuth2BearerAuthentication(HttpRequestMessage request)
    {
        string token = _credentials.GetRequiredBearerAccessToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

    private static string BuildFailureMessage(string prefix, HttpResponseMessage response, string rawJson)
    {
        string message = string.Concat(
            prefix,
            " with status ",
            ((int)response.StatusCode).ToString(),
            " (",
            response.StatusCode.ToString(),
            "). Body: ",
            rawJson);
        return message;
    }

    private static string GetMediaContentType(string mediaFilePath)
    {
        string extension = Path.GetExtension(mediaFilePath).ToLowerInvariant();
        if (extension == ".jpg" || extension == ".jpeg")
        {
            return "image/jpeg";
        }

        if (extension == ".png")
        {
            return "image/png";
        }

        if (extension == ".gif")
        {
            return "image/gif";
        }

        if (extension == ".mp4")
        {
            return "video/mp4";
        }

        return "application/octet-stream";
    }

    private static string GetMediaCategory(string mediaFilePath)
    {
        string extension = Path.GetExtension(mediaFilePath).ToLowerInvariant();
        if (extension == ".mp4")
        {
            return "tweet_video";
        }

        if (extension == ".gif")
        {
            return "tweet_gif";
        }

        return "tweet_image";
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
