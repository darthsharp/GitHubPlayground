namespace GhInfo.Tests.Fakes;

/// <summary>
/// Test stub for <see cref="HttpMessageHandler"/> that returns a pre-configured response
/// (or throws a pre-configured exception) for the first incoming request.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(HttpResponseMessage response)
    {
        _responder = _ => response;
    }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;

        return Task.FromResult(_responder(request));
    }
}
