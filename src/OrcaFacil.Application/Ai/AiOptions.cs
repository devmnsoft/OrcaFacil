namespace OrcaFacil.Application.Ai;

public sealed class AiOptions
{
    public string DefaultProvider { get; set; } = string.Empty;
    public Dictionary<string, AiProviderSettings> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int MaxInputTokens { get; set; } = 4000;
    public int MaxOutputTokens { get; set; } = 1500;
    public int RateLimitPerMinute { get; set; } = 30;
    public int MonthlyAccountLimit { get; set; } = 200;
    public int DailyUserLimit { get; set; } = 40;
}

public sealed class AiProviderSettings
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public List<string> AllowedModels { get; set; } = [];
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public List<string> SupportedCapabilities { get; set; } = ["chat", "structured_output"];
}

public enum AiCapability
{
    Chat,
    StructuredOutput,
    Documents,
    Embeddings
}

public sealed record AiClientRequest(
    string Prompt,
    string? SystemPrompt = null,
    string? Model = null,
    int? MaxTokens = null,
    decimal Temperature = 0.2m,
    IReadOnlyList<string>? StopSequences = null);

public sealed record AiClientResponse(
    bool Succeeded,
    string Content,
    string Provider,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    long LatencyMs,
    string? ErrorCode = null,
    string? ErrorMessage = null);
