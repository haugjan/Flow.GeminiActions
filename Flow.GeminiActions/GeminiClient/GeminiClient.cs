using System.Net.Http;
using System.Net.Http.Json;
using Flow.GeminiActions.Settings;

namespace Flow.GeminiActions.GeminiClient;

internal interface IGeminiClient
{
    Task<string> GenerateAsync(string instruction, string text, CancellationToken token);
}

internal sealed class GeminiClient(Func<HttpClient> httpClientFactory, PluginSettings settings)
    : IGeminiClient
{
    public async Task<string> GenerateAsync(
        string instruction,
        string text,
        CancellationToken token
    )
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException(
                "Gemini API key is missing. Open the plugin settings and paste your key."
            );

        var prompt = $"{instruction}\n\n---\n{text}";
        var request = new GeminiRequest(
            Contents: [new GeminiContent(Parts: [new GeminiPart(Text: prompt)])]
        );

        var path = $"v1beta/models/{Uri.EscapeDataString(settings.Model)}:generateContent";

        using var client = httpClientFactory();
        using var response = await client
            .PostAsJsonAsync(path, request, cancellationToken: token)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            var detail = TryExtractErrorMessage(raw) ?? raw;
            throw new HttpRequestException(
                $"Gemini API returned {(int)response.StatusCode} {response.StatusCode}: {detail}",
                inner: null,
                statusCode: response.StatusCode
            );
        }

        var data = await response
            .Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: token)
            .ConfigureAwait(false);

        if (data?.Error is { } err)
            throw new HttpRequestException($"Gemini API error: {err.Message}");

        var output = data?.FirstText();
        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException("Gemini returned an empty response.");

        return output.Trim();
    }

    private static string? TryExtractErrorMessage(string raw)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            if (
                doc.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var msg)
            )
                return msg.GetString();
        }
        catch
        {
            // ignore – fall back to raw body
        }
        return null;
    }
}
