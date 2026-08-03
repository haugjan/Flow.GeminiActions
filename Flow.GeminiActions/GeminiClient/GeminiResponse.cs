using System.Text.Json.Serialization;

namespace Flow.GeminiActions.GeminiClient;

internal sealed record GeminiResponse(
    [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates,
    [property: JsonPropertyName("error")] GeminiError? Error
)
{
    public string? FirstText() =>
        Candidates?.FirstOrDefault()?.Content?.Parts is { Count: > 0 } parts
            ? string.Concat(parts.Where(p => !p.Thought).Select(p => p.Text))
            : null;
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
