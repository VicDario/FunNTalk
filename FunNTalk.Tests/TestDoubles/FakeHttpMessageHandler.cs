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

    /// <summary>Request bodies in the same order as <see cref="RequestedUris"/>; empty for a GET.</summary>
    public List<string> RequestBodies { get; } = [];

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

    /// <summary>
    /// Lets one handler answer a multi-step exchange differently per request, which the
    /// mint-then-fetch flow needs: a POST that creates a credential, then a GET that redeems it.
    /// </summary>
    public static FakeHttpMessageHandler RespondingPerRequest(
        Func<HttpRequestMessage, HttpResponseMessage> responder) => new(responder);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestedUris.Add(request.RequestUri);
        RequestBodies.Add(
            request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult() ?? string.Empty);
        return Task.FromResult(_responder(request));
    }
}
