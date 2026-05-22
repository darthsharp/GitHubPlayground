using System.Net;

namespace GhInfo.Core.Tests.Fakes;

/// <summary>
/// A stub <see cref="HttpMessageHandler"/> that returns a single preconfigured response and
/// captures the request it received.
/// </summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string body, string? mediaType = "application/json")
    : HttpMessageHandler
{
    /// <summary>Gets the last request that was sent through this handler.</summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>Gets or sets the headers to add to the response.</summary>
    public IReadOnlyDictionary<string, string>? ResponseHeaders { get; set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;

        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType!),
        };

        if (ResponseHeaders is not null)
        {
            foreach (var (key, value) in ResponseHeaders)
            {
                response.Headers.TryAddWithoutValidation(key, value);
            }
        }

        return Task.FromResult(response);
    }
}
