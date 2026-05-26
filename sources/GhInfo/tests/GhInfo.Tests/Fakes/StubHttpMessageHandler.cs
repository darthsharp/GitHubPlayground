using System.Net;
using System.Text;

namespace GhInfo.Tests.Fakes;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    private StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public List<HttpRequestMessage> Requests { get; } = new();

    public static StubHttpMessageHandler ReturnsJson(HttpStatusCode statusCode, string json)
    {
        return new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }

    public static StubHttpMessageHandler ReturnsStatus(HttpStatusCode statusCode, string? body = null)
    {
        return new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = body is null ? new StringContent(string.Empty) : new StringContent(body),
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        return Task.FromResult(_responder(request));
    }
}
