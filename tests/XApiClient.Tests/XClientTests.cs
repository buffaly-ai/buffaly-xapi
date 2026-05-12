using System.Net;
using XApiClient.Core;
using XApiClient.Core.Authentication;
using XApiClient.Tests.TestDoubles;

namespace XApiClient.Tests;

public sealed class XClientTests
{
    [Fact]
    public async Task GetMeAsync_ShouldThrowInvalidOperation_WhenOAuthUserCredentialsAreMissing()
    {
        // Ensures user-context endpoints fail fast when OAuth user context is not fully configured.
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\",\"name\":\"A\",\"username\":\"a\"}}"));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials
        {
            BearerToken = "token-123",
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
        };

        XClient client = new XClient(httpClient, credentials);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMeAsync());

        Assert.Contains("requires user-context authentication", exception.Message, StringComparison.Ordinal);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task GetHomeTimelineAsync_ShouldThrowInvalidOperation_WhenOAuthUserCredentialsAreMissing()
    {
        // Ensures timeline command paths fail fast when user-context credentials are incomplete.
        string payload = "{\"data\":[{\"id\":\"10\",\"text\":\"hello\"}],\"meta\":{\"result_count\":1,\"next_token\":\"next-1\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials
        {
            BearerToken = "token-123",
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
        };

        XClient client = new XClient(httpClient, credentials);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetHomeTimelineAsync("321", 20, "cursor-1"));

        Assert.Contains("requires user-context authentication", exception.Message, StringComparison.Ordinal);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task GetHomeTimelineAsync_ShouldBuildExpectedEndpointAndQuery_WhenOAuthUserCredentialsArePresent()
    {
        // Confirms timeline endpoint and query parameters are generated when user-context auth is configured.
        string payload = "{\"data\":[{\"id\":\"10\",\"text\":\"hello\"}],\"meta\":{\"result_count\":1,\"next_token\":\"next-1\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials
        {
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
            AccessToken = "access-token",
            AccessTokenSecret = "access-token-secret",
            BearerToken = "token-123",
        };

        XClient client = new XClient(httpClient, credentials);
        var response = await client.GetHomeTimelineAsync("321", 20, "cursor-1");

        Assert.NotNull(handler.LastRequest);
        Uri? uri = handler.LastRequest!.RequestUri;
        Assert.NotNull(uri);
        Assert.Equal("/2/users/321/timelines/reverse_chronological", uri!.AbsolutePath);
        Assert.Contains("max_results=20", uri.Query, StringComparison.Ordinal);
        Assert.Contains("pagination_token=cursor-1", uri.Query, StringComparison.Ordinal);
        Assert.True(handler.LastRequest.Headers.TryGetValues("Authorization", out IEnumerable<string>? authValues));
        Assert.Contains(authValues!, value => value.StartsWith("OAuth ", StringComparison.Ordinal));
        Assert.Equal("next-1", response.NextToken);
        Assert.Single(response.Data!);
    }

    [Fact]
    public async Task SearchRecentAsync_ShouldUseBearerToken_WhenOAuthUserCredentialsAreMissing()
    {
        // Confirms read-only search can still use bearer-token auth fallback.
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"data\":[],\"meta\":{\"result_count\":0}}"));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials
        {
            BearerToken = "token-123",
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
        };

        XClient client = new XClient(httpClient, credentials);
        var response = await client.SearchRecentAsync("openai", 20);

        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("token-123", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.NotNull(handler.LastRequest.RequestUri);
        Assert.Equal("/2/tweets/search/recent", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("query=openai", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.NotNull(response.Meta);
        Assert.Equal(0, response.Meta!.ResultCount);
    }

    [Fact]
    public async Task GetMeRawJsonAsync_ShouldReturnRawJsonEnvelope()
    {
        // Confirms ProtoScript-friendly wrapper returns the original API response JSON.
        string payload = "{\"data\":{\"id\":\"1\",\"name\":\"A\",\"username\":\"a\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials
        {
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
            AccessToken = "access-token",
            AccessTokenSecret = "access-token-secret",
        };

        XClient client = new XClient(httpClient, credentials);
        string rawJson = await client.GetMeRawJsonAsync();

        Assert.Equal(payload, rawJson);
    }

    [Fact]
    public async Task TimelineAndMentionsRawJsonWrappers_ShouldReturnRawJsonEnvelope()
    {
        // Confirms timeline-based ProtoScript wrappers preserve the full raw payload text.
        string payload = "{\"data\":[{\"id\":\"10\",\"text\":\"hello\"}],\"meta\":{\"result_count\":1}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials
        {
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
            AccessToken = "access-token",
            AccessTokenSecret = "access-token-secret",
        };

        XClient client = new XClient(httpClient, credentials);
        string home = await client.GetMyHomeTimelineRawJsonAsync("321", 20, "cursor-1");
        string tweets = await client.GetMyTweetsRawJsonAsync("321", 15, null);
        string mentions = await client.GetMyMentionsRawJsonAsync("321", 10, null);

        Assert.Equal(payload, home);
        Assert.Equal(payload, tweets);
        Assert.Equal(payload, mentions);
    }

    [Fact]
    public async Task SearchRecentAndPostTweetRawJsonWrappers_ShouldReturnRawJsonEnvelope()
    {
        // Confirms non-timeline ProtoScript wrappers return the unchanged JSON envelope.
        string payload = "{\"data\":{\"id\":\"11\",\"text\":\"posted\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials
        {
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
            AccessToken = "access-token",
            AccessTokenSecret = "access-token-secret",
            BearerToken = "token-123",
        };

        XClient client = new XClient(httpClient, credentials);
        string searchRawJson = await client.SearchRecentRawJsonAsync("openai", 20, null);
        string postRawJson = await client.PostTweetRawJsonAsync("hello world");

        Assert.Equal(payload, searchRawJson);
        Assert.Equal(payload, postRawJson);
    }

    [Fact]
    public async Task GetMyUserIdAsync_ShouldReturnEmptyString_WhenIdIsMissing()
    {
        // Ensures convenience user-id helper degrades safely when profile data omits an id.
        string payload = "{\"data\":{\"name\":\"A\",\"username\":\"a\"}}";
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, payload));

        HttpClient httpClient = new HttpClient(handler);
        XCredentials credentials = new XCredentials
        {
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
            AccessToken = "access-token",
            AccessTokenSecret = "access-token-secret",
        };

        XClient client = new XClient(httpClient, credentials);
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
        XCredentials credentials = new XCredentials
        {
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
            AccessToken = "access-token",
            AccessTokenSecret = "access-token-secret",
        };

        XClient client = new XClient(httpClient, credentials);
        string userId = await client.GetMyUserIdAsync();

        Assert.Equal("777", userId);
    }
}
