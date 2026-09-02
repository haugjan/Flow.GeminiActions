using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Flow.GeminiActions.Settings;

namespace Flow.GeminiActions.GeminiClient;

internal interface IGeminiClient
{
    Task<string> GenerateAsync(
        string instruction,
        string text,
        CancellationToken token,
        Func<TimeSpan, Task>? onOverloaded = null
    );
}

internal sealed class GeminiClient(Func<HttpClient> httpClientFactory, PluginSettings settings)
    : IGeminiClient
{
    private static readonly string DiagLogPath = Path.Combine(
        Path.GetTempPath(),
        "GeminiActions-diagnostics.log"
    );

    // Delays between attempts on overload: 5 s before the second attempt,
    // 10 s before the third. Three attempts total.
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
    ];

    public async Task<string> GenerateAsync(
        string instruction,
        string text,
        CancellationToken token,
        Func<TimeSpan, Task>? onOverloaded = null
    )
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException(
                "Gemini API key is missing. Open the plugin settings and paste your key."
            );

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await CallAsync(instruction, text, token).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
                when (onOverloaded is not null
                    && attempt < RetryDelays.Length
                    && IsRetryable(ex.StatusCode)
                )
            {
                await onOverloaded(RetryDelays[attempt]).ConfigureAwait(false);
            }
        }
    }

    private async Task<string> CallAsync(string instruction, string text, CancellationToken token)
    {
        var request = new GeminiRequest(
            SystemInstruction: new GeminiContent(Parts: [new GeminiPart(Text: instruction)]),
            Contents: [new GeminiContent(Parts: [new GeminiPart(Text: text)])],
            GenerationConfig: new GeminiGenerationConfig(
                ThinkingConfig: new GeminiThinkingConfig(ThinkingBudget: 0)
            )
        );

        var requestJson = JsonSerializer.Serialize(request);
        var path = $"v1beta/models/{Uri.EscapeDataString(settings.Model)}:generateContent";

        using var client = httpClientFactory();
        using var httpResponse = await client
            .PostAsJsonAsync(path, request, cancellationToken: token)
            .ConfigureAwait(false);

        var rawBody = await httpResponse
            .Content.ReadAsStringAsync(token)
            .ConfigureAwait(false);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var detail = TryExtractErrorMessage(rawBody) ?? rawBody;
            WriteDiagLog(settings.Model, instruction, text, requestJson, (int)httpResponse.StatusCode, rawBody, error: detail);
            throw new HttpRequestException(
                $"Gemini API returned {(int)httpResponse.StatusCode} {httpResponse.StatusCode}: {detail}",
                inner: null,
                statusCode: httpResponse.StatusCode
            );
        }

        var data = JsonSerializer.Deserialize<GeminiResponse>(rawBody);

        if (data?.Error is { } err)
        {
            WriteDiagLog(settings.Model, instruction, text, requestJson, (int)httpResponse.StatusCode, rawBody, error: err.Message);
            throw new HttpRequestException($"Gemini API error: {err.Message}");
        }

        var output = data?.FirstText();

        WriteDiagLog(settings.Model, instruction, text, requestJson, (int)httpResponse.StatusCode, rawBody, output: output);

        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException("Gemini returned an empty response.");

        return output.Trim();
    }

    private static bool IsRetryable(HttpStatusCode? status) =>
        status is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests;

    private static string? TryExtractErrorMessage(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
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

    private static void WriteDiagLog(
        string model,
        string instruction,
        string text,
        string requestJson,
        int statusCode,
        string rawResponse,
        string? output = null,
        string? error = null
    )
    {
        try
        {
            var fi = new FileInfo(DiagLogPath);
            if (fi.Exists && fi.Length > 2 * 1024 * 1024)
                fi.Delete();

            var instrSnippet = instruction.Length > 120 ? instruction[..120] + "…" : instruction;
            var textSnippet = text.Length > 400 ? text[..400] + "…" : text;
            var entry =
                $"""
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] model={model} status={statusCode}
                INSTRUCTION: {instrSnippet}
                TEXT ({text.Length} chars): {textSnippet}
                REQUEST: {requestJson}
                RESPONSE: {rawResponse}
                OUTPUT: {output ?? "(null)"}
                {(error is not null ? $"ERROR: {error}" : "")}
                ---
                """;

            File.AppendAllText(DiagLogPath, entry + Environment.NewLine);
        }
        catch
        {
            // Never let logging break the call
        }
    }
}
