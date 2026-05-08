using System.Net;
using System.Net.Http;
using System.Text;

namespace Flow.GeminiActions.Test;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode statusCode, string content)> _queue = new();
    private (HttpStatusCode statusCode, string content) _fallback = (HttpStatusCode.OK, "");

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }
    public int RequestCount { get; private set; }

    public void SetResponse(HttpStatusCode statusCode, string content)
    {
        _queue.Clear();
        _fallback = (statusCode, content);
    }

    public void EnqueueResponse(HttpStatusCode statusCode, string content) =>
        _queue.Enqueue((statusCode, content));

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        RequestCount++;
        LastRequest = request;

        if (request.Content != null)
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        var (statusCode, content) = _queue.Count > 0 ? _queue.Dequeue() : _fallback;
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }
}
