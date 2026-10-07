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

internal static class AiClientHttp
{
    public static bool TryUseAllowedBase(HttpClient client, string? baseUrl, IReadOnlySet<string> allowedHosts, params string[] allowedPaths)
    {
        if (!TryParseAllowedEndpoint(baseUrl, allowedHosts, allowedPaths, out var uri))
            return false;
        if (client.BaseAddress is { } existing && !SameAllowedEndpoint(existing, allowedHosts, allowedPaths))
            return false;
        if (client.BaseAddress is null)
        {
            var path = uri.AbsolutePath.TrimEnd('/');
            client.BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority) + (path.Length == 0 ? "/" : path + "/"));
        }
        return SameAllowedEndpoint(client.BaseAddress, allowedHosts, allowedPaths);
    }

    public static bool IsRedirect(System.Net.HttpStatusCode status) => (int)status is >= 300 and < 400;

    public static HttpClient CreateNonRedirectingClient() => new(new SocketsHttpHandler { AllowAutoRedirect = false }, disposeHandler: true)
    {
        Timeout = System.Threading.Timeout.InfiniteTimeSpan
    };

    private static bool TryParseAllowedEndpoint(string? baseUrl, IReadOnlySet<string> allowedHosts, string[] allowedPaths, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var parsed))
            return false;
        if (!SameAllowedEndpoint(parsed, allowedHosts, allowedPaths))
            return false;
        uri = parsed;
        return true;
    }

    private static bool SameAllowedEndpoint(Uri uri, IReadOnlySet<string> allowedHosts, string[] allowedPaths)
    {
        if (!uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443 || !string.IsNullOrEmpty(uri.UserInfo))
            return false;
        if (!allowedHosts.Contains(uri.Host))
            return false;
        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.Length == 0) path = "/";
        return allowedPaths.Any(prefix =>
        {
            var normalized = string.IsNullOrWhiteSpace(prefix) || prefix == "/" ? "/" : prefix.TrimEnd('/');
            return path.Equals(normalized, StringComparison.OrdinalIgnoreCase);
        });
    }

    public static int ReadTokenCount(JsonElement usage, string name)
    {
        if (!usage.TryGetProperty(name, out var value)) return 0;
        if (value.TryGetInt32(out var count)) return Math.Max(0, count);
        return value.TryGetDecimal(out var number) ? Math.Max(0, (int)number) : 0;
    }
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
    private readonly bool _endpointAccepted;

    public string ProviderName => "Groq";
    public bool IsConfigured => _endpointAccepted &&
                                _settings is { Enabled: true } &&
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
                      (httpClientFactory != null ? httpClientFactory.CreateClient("Groq") : AiClientHttp.CreateNonRedirectingClient());
        _endpointAccepted = AiClientHttp.TryUseAllowedBase(_httpClient, _settings?.BaseUrl, AllowedHosts, "/openai/v1");
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

            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            if (body.Length > 200_000 || AiClientHttp.IsRedirect(response.StatusCode))
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                    AiClientHttp.IsRedirect(response.StatusCode) ? "redirect_blocked" : "response_too_large",
                    "A resposta externa foi recusada antes de ser usada.");
            }
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
                promptTokens = AiClientHttp.ReadTokenCount(usage, "prompt_tokens");
                completionTokens = AiClientHttp.ReadTokenCount(usage, "completion_tokens");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds,
                    "empty_response", "A Groq não retornou conteúdo utilizável.");
            }

            return new AiClientResponse(true, content, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds);
        }
        catch (JsonException)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "invalid_json", "A resposta externa não pôde ser lida e não foi aplicada.");
        }
        catch (KeyNotFoundException)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "invalid_json", "A resposta externa não pôde ser lida e não foi aplicada.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "timeout", "Tempo limite excedido na comunicação com a Groq.");
        }
        catch (Exception ex)
        {
            _logger.LogError("GROQ_REQUEST_ERROR Type {ExceptionType}", ex.GetType().Name);
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
    private readonly bool _endpointAccepted;

    public string ProviderName => "Gemini";
    public bool IsConfigured => _endpointAccepted &&
                                _settings is { Enabled: true } &&
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
                      (httpClientFactory != null ? httpClientFactory.CreateClient("Gemini") : AiClientHttp.CreateNonRedirectingClient());
        _endpointAccepted = AiClientHttp.TryUseAllowedBase(_httpClient, _settings?.BaseUrl, AllowedHosts, "/v1beta");
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

        var payload = new Dictionary<string, object?>
        {
            ["contents"] = new[]
            {
                new { parts = new[] { new { text = request.Prompt } } }
            },
            ["generationConfig"] = new Dictionary<string, object?>
            {
                ["temperature"] = request.Temperature,
                ["maxOutputTokens"] = request.MaxTokens ?? 1500
            }
        };
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            payload["systemInstruction"] = new { parts = new[] { new { text = request.SystemPrompt } } };

        var sw = Stopwatch.StartNew();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"models/{Uri.EscapeDataString(model)}:generateContent")
        {
            Content = JsonContent.Create(payload)
        };
        httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.TimeoutSeconds)));

            using var response = await _httpClient.SendAsync(httpRequest, timeoutCts.Token);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            if (body.Length > 200_000 || AiClientHttp.IsRedirect(response.StatusCode))
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                    AiClientHttp.IsRedirect(response.StatusCode) ? "redirect_blocked" : "response_too_large",
                    "A resposta externa foi recusada antes de ser usada.");
            }
            if (!response.IsSuccessStatusCode)
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                    response.StatusCode.ToString(), $"Falha na resposta da API Gemini ({response.StatusCode}).");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var content = string.Empty;

            if (root.TryGetProperty("promptFeedback", out var feedback) && feedback.TryGetProperty("blockReason", out _))
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                    "blocked_response", "O Gemini bloqueou a resposta. Nada foi aplicado ao orçamento.");
            }

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                foreach (var candidate in candidates.EnumerateArray())
                {
                    var finish = candidate.TryGetProperty("finishReason", out var finishElement) ? finishElement.GetString() : null;
                    if (finish is "SAFETY" or "RECITATION" or "BLOCKLIST")
                    {
                        return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                            "blocked_response", "O Gemini bloqueou a resposta. Nada foi aplicado ao orçamento.");
                    }

                    if (candidate.TryGetProperty("content", out var contentObj) && contentObj.TryGetProperty("parts", out var partsArr))
                    {
                        foreach (var part in partsArr.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var text))
                                content += text.GetString();
                        }
                    }

                    if (finish == "MAX_TOKENS")
                    {
                        return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                            "truncated_response", "A resposta do Gemini veio incompleta e não foi aplicada.");
                    }
                }
            }

            var promptTokens = 0;
            var completionTokens = 0;
            if (root.TryGetProperty("usageMetadata", out var usage))
            {
                promptTokens = AiClientHttp.ReadTokenCount(usage, "promptTokenCount");
                completionTokens = AiClientHttp.ReadTokenCount(usage, "candidatesTokenCount");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds,
                    "empty_response", "O Gemini não retornou conteúdo utilizável.");
            }

            return new AiClientResponse(true, content, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds);
        }
        catch (JsonException)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "invalid_json", "A resposta externa não pôde ser lida e não foi aplicada.");
        }
        catch (KeyNotFoundException)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "invalid_json", "A resposta externa não pôde ser lida e não foi aplicada.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "timeout", "Tempo limite excedido na comunicação com o Gemini.");
        }
        catch (Exception ex)
        {
            _logger.LogError("GEMINI_REQUEST_ERROR Type {ExceptionType}", ex.GetType().Name);
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
    private readonly bool _endpointAccepted;

    public string ProviderName => "DeepSeek";
    public bool IsConfigured => _endpointAccepted &&
                                _settings is { Enabled: true } &&
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
                      (httpClientFactory != null ? httpClientFactory.CreateClient("DeepSeek") : AiClientHttp.CreateNonRedirectingClient());
        _endpointAccepted = AiClientHttp.TryUseAllowedBase(_httpClient, _settings?.BaseUrl, AllowedHosts, "/", "/v1");
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

            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            if (body.Length > 200_000 || AiClientHttp.IsRedirect(response.StatusCode))
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                    AiClientHttp.IsRedirect(response.StatusCode) ? "redirect_blocked" : "response_too_large",
                    "A resposta externa foi recusada antes de ser usada.");
            }
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
                promptTokens = AiClientHttp.ReadTokenCount(usage, "prompt_tokens");
                completionTokens = AiClientHttp.ReadTokenCount(usage, "completion_tokens");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new AiClientResponse(false, string.Empty, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds,
                    "empty_response", "O DeepSeek não retornou conteúdo utilizável.");
            }

            return new AiClientResponse(true, content, ProviderName, model, promptTokens, completionTokens, sw.ElapsedMilliseconds);
        }
        catch (JsonException)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "invalid_json", "A resposta externa não pôde ser lida e não foi aplicada.");
        }
        catch (KeyNotFoundException)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "invalid_json", "A resposta externa não pôde ser lida e não foi aplicada.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "timeout", "Tempo limite excedido na comunicação com o DeepSeek.");
        }
        catch (Exception ex)
        {
            _logger.LogError("DEEPSEEK_REQUEST_ERROR Type {ExceptionType}", ex.GetType().Name);
            return new AiClientResponse(false, string.Empty, ProviderName, model, 0, 0, sw.ElapsedMilliseconds,
                "provider_error", "Erro ao comunicar com o DeepSeek.");
        }
    }
}
