using System.Net;
using XApiClient.Core;
using XApiClient.Core.Authentication;
using XApiClient.Tests.TestDoubles;

namespace XApiClient.Tests;

public sealed class XClientTests
{
    [Fact]
    public async Task GetMeAsync_ShouldThrowInvalidOperation_WhenOAuth2BearerTokenIsMissing()
    {
        // Ensures every X API path fails before network IO when OAuth2 user-context bearer token is absent.
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\",\"name\":\"A\",\"username\":\"a\"}}"));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials();

        XClient client = new XClient(httpClient, credentials);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMeAsync());

        Assert.Contains("OAuth 2.0 user-context bearer access token is required", exception.Message, StringComparison.Ordinal);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task AllReadAndWriteEndpoints_ShouldUseOAuth2BearerAuthorization()
    {
        // Proves all existing read/search/post client functions use OAuth2 Bearer auth, not OAuth1 signatures.
        List<HttpRequestMessage> requests = new List<HttpRequestMessage>();
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(request =>
        {
            requests.Add(request);
            string path = request.RequestUri!.AbsolutePath;
            if (path == "/2/users/me")
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":{\"id\":\"777\",\"name\":\"A\",\"username\":\"a\"}}");
            }

            if (path == "/2/tweets")
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":{\"id\":\"11\",\"text\":\"posted\"}}");
            }

            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":[],\"meta\":{\"result_count\":0}}");
        });

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = OAuth2Credentials();
        XClient client = new XClient(httpClient, credentials);

        await client.GetMeAsync();
        await client.GetHomeTimelineAsync("777", 20, "cursor-1");
        await client.GetMyTweetsAsync("777", 15, null);
        await client.GetMentionsAsync("777", 10, null);
        await client.SearchRecentAsync("openai", 20, null);
        await client.PostTweetAsync("hello world");

        Assert.Equal(6, requests.Count);
        Assert.All(requests, AssertOAuth2BearerRequest);
        Assert.Contains(requests, request => request.RequestUri!.AbsolutePath == "/2/users/me");
        Assert.Contains(requests, request => request.RequestUri!.AbsolutePath == "/2/users/777/timelines/reverse_chronological");
        Assert.Contains(requests, request => request.RequestUri!.AbsolutePath == "/2/users/777/tweets");
        Assert.Contains(requests, request => request.RequestUri!.AbsolutePath == "/2/users/777/mentions");
        Assert.Contains(requests, request => request.RequestUri!.AbsolutePath == "/2/tweets/search/recent");
        Assert.Contains(requests, request => request.RequestUri!.AbsolutePath == "/2/tweets");
    }

    [Fact]
    public async Task GetHomeTimelineAsync_ShouldBuildExpectedEndpointAndQuery()
    {
        // Confirms timeline endpoint and query parameters are generated for OAuth2 bearer requests.
        string payload = "{\"data\":[{\"id\":\"10\",\"text\":\"hello\"}],\"meta\":{\"result_count\":1,\"next_token\":\"next-1\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XClient client = new XClient(httpClient, OAuth2Credentials());
        var response = await client.GetHomeTimelineAsync("321", 20, "cursor-1");

        Assert.NotNull(handler.LastRequest);
        Uri? uri = handler.LastRequest!.RequestUri;
        Assert.NotNull(uri);
        Assert.Equal("/2/users/321/timelines/reverse_chronological", uri!.AbsolutePath);
        Assert.Contains("max_results=20", uri.Query, StringComparison.Ordinal);
        Assert.Contains("pagination_token=cursor-1", uri.Query, StringComparison.Ordinal);
        AssertOAuth2BearerRequest(handler.LastRequest);
        Assert.Equal("next-1", response.NextToken);
        Assert.Single(response.Data!);
    }

    [Fact]
    public async Task SearchRecentAsync_ShouldUseOAuth2BearerToken()
    {
        // Confirms recent search uses OAuth2 bearer auth and query routing.
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":[],\"meta\":{\"result_count\":0}}"));

        HttpClient httpClient = new HttpClient(handler);
        XClient client = new XClient(httpClient, OAuth2Credentials());
        var response = await client.SearchRecentAsync("openai", 20);

        Assert.NotNull(handler.LastRequest);
        AssertOAuth2BearerRequest(handler.LastRequest!);
        Assert.NotNull(handler.LastRequest!.RequestUri);
        Assert.Equal("/2/tweets/search/recent", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("query=openai", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.NotNull(response.Meta);
        Assert.Equal(0, response.Meta!.ResultCount);
    }

    [Fact]
    public async Task RawJsonWrappers_ShouldReturnRawJsonEnvelope()
    {
        // Confirms ProtoScript-friendly wrappers return the original API response JSON.
        string userPayload = "{\"data\":{\"id\":\"1\",\"name\":\"A\",\"username\":\"a\"}}";
        string listPayload = "{\"data\":[{\"id\":\"10\",\"text\":\"hello\"}],\"meta\":{\"result_count\":1}}";
        string searchPayload = "{\"data\":[],\"meta\":{\"result_count\":0}}";
        string postPayload = "{\"data\":{\"id\":\"11\",\"text\":\"posted\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            if (path == "/2/users/me")
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK, userPayload);
            }

            if (path == "/2/tweets/search/recent")
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK, searchPayload);
            }

            if (path == "/2/tweets")
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK, postPayload);
            }

            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, listPayload);
        });

        HttpClient httpClient = new HttpClient(handler);
        XClient client = new XClient(httpClient, OAuth2Credentials());

        Assert.Equal(userPayload, await client.GetMeRawJsonAsync());
        Assert.Equal(listPayload, await client.GetMyHomeTimelineRawJsonAsync("321", 20, "cursor-1"));
        Assert.Equal(listPayload, await client.GetMyTweetsRawJsonAsync("321", 15, null));
        Assert.Equal(listPayload, await client.GetMyMentionsRawJsonAsync("321", 10, null));
        Assert.Equal(searchPayload, await client.SearchRecentRawJsonAsync("openai", 20, null));
        Assert.Equal(postPayload, await client.PostTweetRawJsonAsync("hello world"));
    }

    [Fact]
    public async Task GetMyUserIdAsync_ShouldReturnEmptyString_WhenIdIsMissing()
    {
        // Ensures convenience user-id helper degrades safely when profile data omits an id.
        string payload = "{\"data\":{\"name\":\"A\",\"username\":\"a\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XClient client = new XClient(httpClient, OAuth2Credentials());
        string userId = await client.GetMyUserIdAsync();

        Assert.Equal(string.Empty, userId);
    }

    [Fact]
    public async Task GetMyUserIdAsync_ShouldReturnId_WhenPresent()
    {
        // Confirms convenience user-id helper returns the parsed id for ProtoScript callers.
        string payload = "{\"data\":{\"id\":\"777\",\"name\":\"A\",\"username\":\"a\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XClient client = new XClient(httpClient, OAuth2Credentials());
        string userId = await client.GetMyUserIdAsync();

        Assert.Equal("777", userId);
    }

    [Fact]
    public async Task PostTweetWithMediaFileRawJsonAsync_ShouldUploadMediaToV2ThenPostTweetWithMediaId()
    {
        // Confirms media posting uses X API v2 media upload with OAuth2 bearer auth, then attaches media_ids to tweet creation.
        string tempFile = Path.Combine(Path.GetTempPath(), string.Concat(Guid.NewGuid().ToString("N"), ".png"));
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });
        List<HttpRequestMessage> requests = new List<HttpRequestMessage>();
        List<HttpContent> postContents = new List<HttpContent>();
        bool sawMultipartUploadContent = false;
        bool sawMediaCategoryPart = false;
        bool sawMediaTypePart = false;

        try
        {
            FakeHttpMessageHandler handler = new FakeHttpMessageHandler(request =>
            {
                requests.Add(request);
                if (request.RequestUri!.AbsolutePath == "/2/media/upload")
                {
                    sawMultipartUploadContent = request.Content is MultipartFormDataContent;
                    if (request.Content is MultipartFormDataContent multipartContent)
                    {
                        foreach (HttpContent part in multipartContent)
                        {
                            string partName = part.Headers.ContentDisposition?.Name?.Trim('"') ?? string.Empty;
                            if (partName == "media_category")
                            {
                                sawMediaCategoryPart = true;
                            }

                            if (partName == "media_type")
                            {
                                sawMediaTypePart = true;
                            }
                        }
                    }

                    return FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":{\"id\":\"999\"}}");
                }

                if (request.Content != null)
                {
                    postContents.Add(request.Content);
                }

                return FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":{\"id\":\"11\",\"text\":\"posted\"}}");
            });

            HttpClient httpClient = new HttpClient(handler);
            XClient client = new XClient(httpClient, OAuth2Credentials());
            string rawJson = await client.PostTweetWithMediaFileRawJsonAsync("hello media", tempFile);

            Assert.Equal("{\"data\":{\"id\":\"11\",\"text\":\"posted\"}}", rawJson);
            Assert.Equal(2, requests.Count);
            Assert.Equal("/2/media/upload", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("/2/tweets", requests[1].RequestUri!.AbsolutePath);
            Assert.All(requests, AssertOAuth2BearerRequest);
            Assert.True(sawMultipartUploadContent);
            Assert.True(sawMediaCategoryPart);
            Assert.True(sawMediaTypePart);
            string postBody = await postContents[0].ReadAsStringAsync();
            Assert.Contains("\"media_ids\":[\"999\"]", postBody, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PostTweetWithMediaFileRawJsonAsync_ShouldThrowFileNotFound_WhenMediaFileIsMissing()
    {
        // Ensures missing media paths fail before any network call is attempted.
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        HttpClient httpClient = new HttpClient(handler);
        XClient client = new XClient(httpClient, OAuth2Credentials());
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => client.PostTweetWithMediaFileRawJsonAsync("hello media", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.png")));

        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public void XCredentials_GetRequiredBearerAccessToken_ShouldPreferAccessTokenOverBearerAlias()
    {
        // Documents OAuth2 token resolution while keeping BearerToken as a compatibility alias.
        XCredentials credentials = new XCredentials
        {
            AccessToken = " access-token ",
            BearerToken = "bearer-token",
        };

        Assert.Equal("access-token", credentials.GetRequiredBearerAccessToken());
    }

    private static XCredentials OAuth2Credentials()
    {
        return new XCredentials
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            AccessToken = "oauth2-user-access-token",
            RefreshToken = "refresh-token",
            Scopes = "tweet.read tweet.write users.read media.write offline.access",
        };
    }

    private static void AssertOAuth2BearerRequest(HttpRequestMessage request)
    {
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("oauth2-user-access-token", request.Headers.Authorization.Parameter);
        Assert.False(request.Headers.TryGetValues("Authorization", out IEnumerable<string>? values)
            && values.Any(value => value.StartsWith("OAuth ", StringComparison.Ordinal)));
    }
}

