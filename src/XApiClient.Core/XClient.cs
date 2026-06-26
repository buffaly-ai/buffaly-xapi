using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XApiClient.Core.Authentication;
using XApiClient.Core.DTOs;
using XApiClient.Core.Models;

namespace XApiClient.Core;

public sealed class XClient
{
    private const string DefaultBaseUrl = "https://api.x.com";
    private const string MediaUploadUrl = "https://upload.twitter.com/1.1/media/upload.json";
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
    public Task<XResponse<Tweet>> PostTweetReplyAsync(string text, string inReplyToTweetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Tweet text is required.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(inReplyToTweetId))
        {
            throw new ArgumentException("Reply target tweet id is required.", nameof(inReplyToTweetId));
        }

        PostTweetRequest payload = new PostTweetRequest
        {
            Text = text,
            Reply = new PostTweetReplyRequest
            {
                InReplyToTweetId = inReplyToTweetId,
            },
        };

        return SendAsync<Tweet>(HttpMethod.Post, "/2/tweets", null, payload, cancellationToken);
    }

    public async Task<string> PostTweetReplyRawJsonAsync(string text, string inReplyToTweetId, CancellationToken cancellationToken = default)
    {
        XResponse<Tweet> response = await PostTweetReplyAsync(text, inReplyToTweetId, cancellationToken).ConfigureAwait(false);
        return response.RawJson;
    }


    public async Task<string> PostThreadRawJsonAsync(string threadJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(threadJson))
        {
            throw new ArgumentException("Thread JSON is required.", nameof(threadJson));
        }

        string[]? tweetTexts = JsonSerializer.Deserialize<string[]>(threadJson, SerializerOptions);
        if (tweetTexts == null || tweetTexts.Length == 0)
        {
            throw new ArgumentException("Thread JSON must contain at least one tweet text.", nameof(threadJson));
        }

        List<JsonDocument> responseDocuments = new List<JsonDocument>();
        List<JsonElement> responseElements = new List<JsonElement>();
        string previousTweetId = string.Empty;
        try
        {
            for (int i = 0; i < tweetTexts.Length; i += 1)
            {
                string tweetText = tweetTexts[i];
                if (string.IsNullOrWhiteSpace(tweetText))
                {
                    throw new ArgumentException("Tweet text at index " + i.ToString() + " is required.", nameof(threadJson));
                }

                string rawJson;
                if (string.IsNullOrWhiteSpace(previousTweetId))
                {
                    rawJson = await PostTweetRawJsonAsync(tweetText, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    rawJson = await PostTweetReplyRawJsonAsync(tweetText, previousTweetId, cancellationToken).ConfigureAwait(false);
                }

                JsonDocument responseDocument = JsonDocument.Parse(rawJson);
                responseDocuments.Add(responseDocument);
                responseElements.Add(responseDocument.RootElement.Clone());
                previousTweetId = ReadTweetIdOrThrow(responseDocument.RootElement, i, rawJson);
            }

            return JsonSerializer.Serialize(responseElements, SerializerOptions);
        }
        finally
        {
            foreach (JsonDocument responseDocument in responseDocuments)
            {
                responseDocument.Dispose();
            }
        }
    }

    private static string ReadTweetIdOrThrow(JsonElement responseRoot, int tweetIndex, string rawJson)
    {
        if (!responseRoot.TryGetProperty("data", out JsonElement dataElement))
        {
            throw new InvalidOperationException("X post response at index " + tweetIndex.ToString() + " did not include data. Raw response: " + rawJson);
        }

        if (!dataElement.TryGetProperty("id", out JsonElement idElement))
        {
            throw new InvalidOperationException("X post response at index " + tweetIndex.ToString() + " did not include data.id. Raw response: " + rawJson);
        }

        string? tweetId = idElement.GetString();
        if (string.IsNullOrWhiteSpace(tweetId))
        {
            throw new InvalidOperationException("X post response at index " + tweetIndex.ToString() + " included an empty data.id. Raw response: " + rawJson);
        }

        return tweetId;
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

        string mediaId = await UploadMediaFileWithOAuth1Async(mediaFilePath, cancellationToken).ConfigureAwait(false);
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
        return await UploadMediaFileWithOAuth1Async(mediaFilePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> UploadMediaFileWithOAuth1Async(string mediaFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mediaFilePath))
        {
            throw new ArgumentException("Media file path is required.", nameof(mediaFilePath));
        }

        if (!File.Exists(mediaFilePath))
        {
            throw new FileNotFoundException("Media file was not found.", mediaFilePath);
        }

        _credentials.ValidateOAuth1MediaCredentials();

        Uri uri = new Uri(MediaUploadUrl);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = BuildOAuth1AuthorizationHeader(HttpMethod.Post, uri);

        await using FileStream fileStream = File.OpenRead(mediaFilePath);
        using StreamContent mediaContent = new StreamContent(fileStream);
        mediaContent.Headers.ContentType = new MediaTypeHeaderValue(GetMediaContentType(mediaFilePath));
        using MultipartFormDataContent form = new MultipartFormDataContent();
        form.Add(mediaContent, "media", Path.GetFileName(mediaFilePath));
        request.Content = form;

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string rawJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            string message = BuildFailureMessage("X OAuth1 media upload request failed", response, rawJson);
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        using JsonDocument document = JsonDocument.Parse(rawJson);
        if (document.RootElement.TryGetProperty("media_id_string", out JsonElement mediaIdStringElement))
        {
            string? mediaId = mediaIdStringElement.GetString();
            if (!string.IsNullOrWhiteSpace(mediaId))
            {
                return mediaId;
            }
        }

        if (document.RootElement.TryGetProperty("media_id", out JsonElement mediaIdElement))
        {
            string mediaId = mediaIdElement.GetRawText().Trim('"');
            if (!string.IsNullOrWhiteSpace(mediaId))
            {
                return mediaId;
            }
        }

        throw new InvalidOperationException("X OAuth1 media upload response did not include a media id.");
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

    private AuthenticationHeaderValue BuildOAuth1AuthorizationHeader(HttpMethod httpMethod, Uri uri)
    {
        Dictionary<string, string> oauthParameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["oauth_consumer_key"] = _credentials.ConsumerKey!.Trim(),
            ["oauth_nonce"] = CreateOAuthNonce(),
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["oauth_token"] = _credentials.AccessToken!.Trim(),
            ["oauth_version"] = "1.0",
        };

        string parameterString = string.Join("&", oauthParameters
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => string.Concat(PercentEncode(pair.Key), "=", PercentEncode(pair.Value))));
        string signatureBase = string.Concat(
            httpMethod.Method.ToUpperInvariant(),
            "&",
            PercentEncode(uri.GetLeftPart(UriPartial.Path)),
            "&",
            PercentEncode(parameterString));
        string signingKey = string.Concat(PercentEncode(_credentials.ConsumerSecret!.Trim()), "&", PercentEncode(_credentials.AccessTokenSecret!.Trim()));
        using HMACSHA1 hmac = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
        string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase)));
        oauthParameters["oauth_signature"] = signature;

        string headerValue = string.Join(", ", oauthParameters
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => string.Concat(PercentEncode(pair.Key), "=\"", PercentEncode(pair.Value), "\"")));
        return new AuthenticationHeaderValue("OAuth", headerValue);
    }

    private static string CreateOAuthNonce()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '0').Replace('/', '1');
    }

    private static string PercentEncode(string value)
    {
        return Uri.EscapeDataString(value).Replace("%7E", "~");
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




