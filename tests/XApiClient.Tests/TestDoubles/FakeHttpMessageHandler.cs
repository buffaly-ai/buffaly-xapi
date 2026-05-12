using System.Net;
using System.Net.Http;

namespace XApiClient.Tests.TestDoubles;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        HttpResponseMessage response = _handler.Invoke(request);
        return Task.FromResult(response);
    }

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string json)
    {
        HttpResponseMessage response = new HttpResponseMessage(statusCode);
        response.Content = new StringContent(json);
        return response;
    }
}
