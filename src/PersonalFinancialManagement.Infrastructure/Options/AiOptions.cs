namespace PersonalFinancialManagement.Infrastructure.Options;

// Settings for an OpenAI-compatible provider (chat completions + audio transcriptions).
// Without an ApiKey the AI calls are skipped: text is parsed by built-in rules and
// insights come from rules, while voice input reports that it is not configured.
public class AiOptions
{
    public const string SectionName = "Ai";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = "gpt-4o-mini";

    public string TranscriptionModel { get; set; } = "whisper-1";

    // Language hint for speech recognition (ISO 639-1).
    public string Language { get; set; } = "fa";

    public int TimeoutSeconds { get; set; } = 60;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
