using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Ai;

namespace OrcaFacil.Infrastructure.Ai;

public interface IAiModelClient
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    IReadOnlyList<string> AllowedModels { get; }
    Task<AiClientResponse> ExecuteChatAsync(AiClientRequest request, CancellationToken ct = default);
}

public sealed class GroqAiClient : IAiModelClient
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "api.groq.com"
    };

    private readonly HttpClient _httpClient;
    private readonly AiProviderSettings? _settings;
    private readonly ILogger<GroqAiClient> _logger;
    private readonly string? _apiKey;

    public string ProviderName => "Groq";
    public bool IsConfigured => _settings is { Enabled: true } &&
                                _settings.AllowedModels.Count > 0 &&
                                !string.IsNullOrWhiteSpace(_apiKey);
    public IReadOnlyList<string> AllowedModels => _settings?.AllowedModels ?? [];

    public GroqAiClient(
        IOptions<AiOptions> options,
        HttpClient? httpClient = null,
        IHttpClientFactory? httpClientFactory = null,
        ILogger<GroqAiClient>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GroqAiClient>.Instance;
        _settings = options.Value.Providers.GetValueOrDefault("Groq");

        _apiKey = !string.IsNullOrWhiteSpace(_settings?.ApiKey)
            ? _settings.ApiKey
            : Environment.GetEnvironmentVariable("AI_GROQ_API_KEY") ??
              Environment.GetEnvironmentVariable("GROQ_API_KEY");

        _httpClient = httpClient ??
                      (httpClientFactory != null ? httpClientFactory.CreateClient("Groq") : new HttpClient());

        if (_settings != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            var baseUrl = _settings.BaseUrl.TrimEnd('/');
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && AllowedHosts.Contains(uri.Host))
            {
                _httpClient.BaseAddress = new Uri(baseUrl + "/");
            }
        }
    }

    public async Task<AiClientResponse> ExecuteChatAsync(AiClientRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, string.Empty, 0, 0, 0,
                "provider_not_configured", "Provedor Groq não está habilitado ou credenciais não foram configuradas.");
        }

        var model = request.Model ?? _settings!.AllowedModels[0];
        if (!_settings!.AllowedModels.Contains(model, StringComparer.OrdinalIgnoreCase))
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, 0,
                "model_not_allowed", $"O modelo '{model}' não consta na lista de modelos autorizados da Groq.");
        }

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }
        messages.Add(new { role = "user", content = request.Prompt });

        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["max_tokens"] = request.MaxTokens ?? 1500
        };

        var sw = Stopwatch.StartNew();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(payload)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.TimeoutSeconds)));

            using var response = await _httpClient.SendAsync(httpRequest, timeoutCts.Token);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                    response.StatusCode.ToString(), $"Falha na resposta da API Groq ({response.StatusCode}).");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

            var promptTokens = 0;
            var completionTokens = 0;
            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt)) promptTokens = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ctEl)) completionTokens = ctEl.GetInt32();
            }

            return new AiClientResponse(true, content, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "timeout", "Tempo limite excedido na comunicação com a Groq.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GROQ_REQUEST_ERROR");
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "provider_error", "Erro ao comunicar com a Groq.");
        }
    }
}

public sealed class GeminiAiClient : IAiModelClient
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "generativelanguage.googleapis.com"
    };

    private readonly HttpClient _httpClient;
    private readonly AiProviderSettings? _settings;
    private readonly ILogger<GeminiAiClient> _logger;
    private readonly string? _apiKey;

    public string ProviderName => "Gemini";
    public bool IsConfigured => _settings is { Enabled: true } &&
                                _settings.AllowedModels.Count > 0 &&
                                !string.IsNullOrWhiteSpace(_apiKey);
    public IReadOnlyList<string> AllowedModels => _settings?.AllowedModels ?? [];

    public GeminiAiClient(
        IOptions<AiOptions> options,
        HttpClient? httpClient = null,
        IHttpClientFactory? httpClientFactory = null,
        ILogger<GeminiAiClient>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GeminiAiClient>.Instance;
        _settings = options.Value.Providers.GetValueOrDefault("Gemini");

        _apiKey = !string.IsNullOrWhiteSpace(_settings?.ApiKey)
            ? _settings.ApiKey
            : Environment.GetEnvironmentVariable("AI_GEMINI_API_KEY") ??
              Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        _httpClient = httpClient ??
                      (httpClientFactory != null ? httpClientFactory.CreateClient("Gemini") : new HttpClient());

        if (_settings != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            var baseUrl = _settings.BaseUrl.TrimEnd('/');
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && AllowedHosts.Contains(uri.Host))
            {
                _httpClient.BaseAddress = new Uri(baseUrl + "/");
            }
        }
    }

    public async Task<AiClientResponse> ExecuteChatAsync(AiClientRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, string.Empty, 0, 0, 0,
                "provider_not_configured", "Provedor Gemini não está habilitado ou credenciais não foram configuradas.");
        }

        var model = request.Model ?? _settings!.AllowedModels[0];
        if (!_settings!.AllowedModels.Contains(model, StringComparer.OrdinalIgnoreCase))
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, 0,
                "model_not_allowed", $"O modelo '{model}' não consta na lista de modelos autorizados do Gemini.");
        }

        var parts = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            parts.Add(new { text = $"[Instrução do Sistema]: {request.SystemPrompt}\n\n" });
        }
        parts.Add(new { text = request.Prompt });

        var payload = new Dictionary<string, object?>
        {
            ["contents"] = new[]
            {
                new { parts }
            },
            ["generationConfig"] = new Dictionary<string, object?>
            {
                ["temperature"] = request.Temperature,
                ["maxOutputTokens"] = request.MaxTokens ?? 1500
            }
        };

        var sw = Stopwatch.StartNew();
        var relativeUrl = $"models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(_apiKey!)}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(payload)
        };

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.TimeoutSeconds)));

            using var response = await _httpClient.SendAsync(httpRequest, timeoutCts.Token);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                    response.StatusCode.ToString(), $"Falha na resposta da API Gemini ({response.StatusCode}).");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var content = string.Empty;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var partsArr) && partsArr.GetArrayLength() > 0)
                {
                    content = partsArr[0].GetProperty("text").GetString() ?? string.Empty;
                }
            }

            var promptTokens = 0;
            var completionTokens = 0;
            if (root.TryGetProperty("usageMetadata", out var usage))
            {
                if (usage.TryGetProperty("promptTokenCount", out var pt)) promptTokens = pt.GetInt32();
                if (usage.TryGetProperty("candidatesTokenCount", out var ctEl)) completionTokens = ctEl.GetInt32();
            }

            return new AiClientResponse(true, content, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "timeout", "Tempo limite excedido na comunicação com o Gemini.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GEMINI_REQUEST_ERROR");
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "provider_error", "Erro ao comunicar com o Gemini.");
        }
    }
}

public sealed class DeepSeekAiClient : IAiModelClient
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "api.deepseek.com"
    };

    private readonly HttpClient _httpClient;
    private readonly AiProviderSettings? _settings;
    private readonly ILogger<DeepSeekAiClient> _logger;
    private readonly string? _apiKey;

    public string ProviderName => "DeepSeek";
    public bool IsConfigured => _settings is { Enabled: true } &&
                                _settings.AllowedModels.Count > 0 &&
                                !string.IsNullOrWhiteSpace(_apiKey);
    public IReadOnlyList<string> AllowedModels => _settings?.AllowedModels ?? [];

    public DeepSeekAiClient(
        IOptions<AiOptions> options,
        HttpClient? httpClient = null,
        IHttpClientFactory? httpClientFactory = null,
        ILogger<DeepSeekAiClient>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DeepSeekAiClient>.Instance;
        _settings = options.Value.Providers.GetValueOrDefault("DeepSeek");

        _apiKey = !string.IsNullOrWhiteSpace(_settings?.ApiKey)
            ? _settings.ApiKey
            : Environment.GetEnvironmentVariable("AI_DEEPSEEK_API_KEY") ??
              Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        _httpClient = httpClient ??
                      (httpClientFactory != null ? httpClientFactory.CreateClient("DeepSeek") : new HttpClient());

        if (_settings != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            var baseUrl = _settings.BaseUrl.TrimEnd('/');
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && AllowedHosts.Contains(uri.Host))
            {
                _httpClient.BaseAddress = new Uri(baseUrl + "/");
            }
        }
    }

    public async Task<AiClientResponse> ExecuteChatAsync(AiClientRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, string.Empty, 0, 0, 0,
                "provider_not_configured", "Provedor DeepSeek não está habilitado ou credenciais não foram configuradas.");
        }

        var model = request.Model ?? _settings!.AllowedModels[0];
        if (!_settings!.AllowedModels.Contains(model, StringComparer.OrdinalIgnoreCase))
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, 0,
                "model_not_allowed", $"O modelo '{model}' não consta na lista de modelos autorizados do DeepSeek.");
        }

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }
        messages.Add(new { role = "user", content = request.Prompt });

        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["max_tokens"] = request.MaxTokens ?? 1500
        };

        var sw = Stopwatch.StartNew();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(payload)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.TimeoutSeconds)));

            using var response = await _httpClient.SendAsync(httpRequest, timeoutCts.Token);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                    response.StatusCode.ToString(), $"Falha na resposta da API DeepSeek ({response.StatusCode}).");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

            var promptTokens = 0;
            var completionTokens = 0;
            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt)) promptTokens = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ctEl)) completionTokens = ctEl.GetInt32();
            }

            return new AiClientResponse(true, content, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "timeout", "Tempo limite excedido na comunicação com o DeepSeek.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DEEPSEEK_REQUEST_ERROR");
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "provider_error", "Erro ao comunicar com o DeepSeek.");
        }
    }
}
