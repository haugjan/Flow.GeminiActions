using System.Net;
using System.Net.Http;
using Flow.GeminiActions.GeminiClient;
using Flow.GeminiActions.Settings;
using Shouldly;

namespace Flow.GeminiActions.Test;

public class GeminiClientTest
{
    private static (GeminiClient.GeminiClient client, TestHttpMessageHandler handler) Build(
        PluginSettings? settings = null
    )
    {
        var handler = new TestHttpMessageHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
        };
        settings ??= new PluginSettings { ApiKey = "test-key", Model = "gemini-2.5-flash" };
        return (new GeminiClient.GeminiClient(() => http, settings), handler);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsFirstCandidateText()
    {
        var (client, handler) = Build();
        handler.SetResponse(
            HttpStatusCode.OK,
            """{"candidates":[{"content":{"parts":[{"text":"Hello world"}]}}]}"""
        );

        var result = await client.GenerateAsync(
            "translate",
            "hallo welt",
            TestContext.Current.CancellationToken
        );

        result.ShouldBe("Hello world");
    }

    [Fact]
    public async Task GenerateAsync_TrimsWhitespaceFromResponse()
    {
        var (client, handler) = Build();
        handler.SetResponse(
            HttpStatusCode.OK,
            """{"candidates":[{"content":{"parts":[{"text":"  Hello\n"}]}}]}"""
        );

        var result = await client.GenerateAsync("x", "y", TestContext.Current.CancellationToken);

        result.ShouldBe("Hello");
    }

    [Fact]
    public async Task GenerateAsync_PostsToCorrectModelEndpoint()
    {
        var settings = new PluginSettings { ApiKey = "abc", Model = "gemini-2.5-pro" };
        var (client, handler) = Build(settings);
        handler.SetResponse(
            HttpStatusCode.OK,
            """{"candidates":[{"content":{"parts":[{"text":"ok"}]}}]}"""
        );

        await client.GenerateAsync("inst", "txt", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler
            .LastRequest!.RequestUri!.ToString()
            .ShouldContain("v1beta/models/gemini-2.5-pro:generateContent");
    }

    [Fact]
    public async Task GenerateAsync_SendsInstructionAndTextSeparatedByDashes()
    {
        var (client, handler) = Build();
        handler.SetResponse(
            HttpStatusCode.OK,
            """{"candidates":[{"content":{"parts":[{"text":"ok"}]}}]}"""
        );

        await client.GenerateAsync(
            "Translate to English",
            "Hallo Welt",
            TestContext.Current.CancellationToken
        );

        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldContain("Translate to English");
        handler.LastRequestBody.ShouldContain("Hallo Welt");
        handler.LastRequestBody.ShouldContain("---");
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenApiKeyMissing()
    {
        var settings = new PluginSettings { ApiKey = string.Empty };
        var (client, _) = Build(settings);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.GenerateAsync("i", "t", TestContext.Current.CancellationToken)
        );
        ex.Message.ShouldContain("API key");
    }

    [Fact]
    public async Task GenerateAsync_ThrowsHttpRequestException_OnHttpError()
    {
        var (client, handler) = Build();
        handler.SetResponse(
            HttpStatusCode.Unauthorized,
            """{"error":{"code":401,"message":"API key not valid","status":"UNAUTHENTICATED"}}"""
        );

        var ex = await Should.ThrowAsync<HttpRequestException>(() =>
            client.GenerateAsync("i", "t", TestContext.Current.CancellationToken)
        );
        ex.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ex.Message.ShouldContain("API key not valid");
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenResponseHasNoCandidates()
    {
        var (client, handler) = Build();
        handler.SetResponse(HttpStatusCode.OK, """{"candidates":[]}""");

        await Should.ThrowAsync<InvalidOperationException>(() =>
            client.GenerateAsync("i", "t", TestContext.Current.CancellationToken)
        );
    }
}
