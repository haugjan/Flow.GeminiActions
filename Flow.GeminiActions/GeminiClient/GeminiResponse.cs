using System.Text.Json.Serialization;

namespace Flow.GeminiActions.GeminiClient;

internal sealed record GeminiResponse(
    [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates,
    [property: JsonPropertyName("error")] GeminiError? Error
)
{
    public string? FirstText()
    {
        var parts = Candidates?.FirstOrDefault()?.Content?.Parts;
        if (parts is not { Count: > 0 })
            return null;

        var text = string.Concat(parts.Where(p => !p.Thought).Select(p => p.Text));
        return text.Length > 0 ? text : string.Concat(parts.Select(p => p.Text));
    }
}

internal sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent? Content,
    [property: JsonPropertyName("finishReason")] string? FinishReason
);

internal sealed record GeminiError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("status")] string? Status
);
