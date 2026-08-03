using System.Text.Json.Serialization;

namespace Flow.GeminiActions.GeminiClient;

internal sealed record GeminiRequest(
    [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents
);

internal sealed record GeminiContent(
    [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts
);

internal sealed record GeminiPart(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("thought")] bool Thought = false
);
