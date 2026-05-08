namespace Flow.GeminiActions.Settings;

public sealed class PluginSettings
{
    public PluginSettings()
    {
        ApiKey = string.Empty;
        Model = "gemini-2.5-flash";
        Timeout = TimeSpan.FromSeconds(30);
        Actions = DefaultActions();
    }

    public string ApiKey { get; set; }
    public string Model { get; set; }
    public TimeSpan Timeout { get; set; }
    public List<GeminiAction> Actions { get; set; }

    public static List<GeminiAction> DefaultActions() =>
        [
            new GeminiAction
            {
                Title = "Translate",
                Description = "Translate the text into English (professional, natural tone).",
                Instruction =
                    "Translate the following text into English. Maintain a professional and natural tone. "
                    + "Provide only the translated text as your response. Do not include any introductions, "
                    + "explanations, or quotation marks.",
            },
            new GeminiAction
            {
                Title = "Correct",
                Description = "Fix grammar, spelling and style. Keep the original language.",
                Instruction =
                    "Review and revise the following text for grammar, spelling, and stylistic flow. "
                    + "Improve the phrasing while preserving the original meaning. Maintain the original "
                    + "language of the input text. Provide only the corrected text. Do not add any comments "
                    + "or meta-talk.",
            },
            new GeminiAction
            {
                Title = "Bullets to text",
                Description = "Turn bullet points or fragments into a cohesive professional text.",
                Instruction =
                    "Transform the following bullet points or fragments into a cohesive, well-structured, "
                    + "and professional text. Ensure logical transitions between ideas. The output must be "
                    + "in the same language as the input. Provide only the resulting text, with no additional "
                    + "preamble or concluding remarks.",
            },
        ];
}
