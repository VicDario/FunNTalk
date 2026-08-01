using System.Net;

namespace FunNTalk.Tests.TestDoubles;

/// <summary>
/// Minimal <see cref="HttpMessageHandler"/> stand-in so an <see cref="HttpClient"/> can be handed
/// to code under test without touching the network. It records every request so tests can assert
/// on the URL that was built, or on the absence of any call at all.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    private FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public List<Uri?> RequestedUris { get; } = [];

    public int CallCount => RequestedUris.Count;

    public static FakeHttpMessageHandler Responding(HttpStatusCode statusCode, string content) =>
        new(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content),
        });

    public static FakeHttpMessageHandler RespondingWithJson(string json) =>
        Responding(HttpStatusCode.OK, json);

    public static FakeHttpMessageHandler Throwing(Exception exception) =>
        new(_ => throw exception);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestedUris.Add(request.RequestUri);
        return Task.FromResult(_responder(request));
    }
}
