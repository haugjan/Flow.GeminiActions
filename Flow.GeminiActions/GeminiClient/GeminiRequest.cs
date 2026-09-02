using System.Text.Json.Serialization;

namespace Flow.GeminiActions.GeminiClient;

internal sealed record GeminiRequest(
    [property: JsonPropertyName("system_instruction")] GeminiContent SystemInstruction,
    [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
    [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig? GenerationConfig = null
);

internal sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("thinkingConfig")] GeminiThinkingConfig? ThinkingConfig = null
);

internal sealed record GeminiThinkingConfig(
    [property: JsonPropertyName("thinkingBudget")] int ThinkingBudget
);

internal sealed record GeminiContent(
    [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts
);

internal sealed record GeminiPart(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("thought"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Thought = false
);
