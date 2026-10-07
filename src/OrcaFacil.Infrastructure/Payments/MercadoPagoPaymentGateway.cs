using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Payments;

namespace OrcaFacil.Infrastructure.Payments;

public class MercadoPagoPaymentGateway : IPaymentGateway
{
    private const string BaseUrl = "https://api.mercadopago.com";
    private readonly MercadoPagoOptions _options;
    private readonly HttpClient _httpClient;

    public MercadoPagoPaymentGateway(
        IOptions<MercadoPagoOptions> options,
        HttpClient? httpClient = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        _options = options.Value;
        if (httpClient != null)
        {
            _httpClient = httpClient;
        }
        else if (httpClientFactory != null)
        {
            _httpClient = httpClientFactory.CreateClient("MercadoPago");
        }
        else
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        }

        _httpClient.BaseAddress ??= new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
    }

    public Task<PaymentGatewayResult> CreatePixPaymentAsync(PaymentGatewayRequest request, CancellationToken ct = default)
        => CreatePaymentAsync("pix", request, ct);

    public Task<PaymentGatewayResult> CreateBoletoPaymentAsync(PaymentGatewayRequest request, CancellationToken ct = default)
        => CreatePaymentAsync("boleto", request, ct);

    public async Task<PaymentGatewayResult> CreateSubscriptionAsync(PaymentGatewayRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled())
            return UnavailableAsync();

        var billingCycle = request.BillingCycle?.Trim().ToLowerInvariant();
        if (billingCycle != "monthly" && billingCycle != "annual")
        {
            return new PaymentGatewayResult(
                false,
                null,
                "invalid_billing_cycle",
                Error: "Periodicidade de cobrança inválida. Valores permitidos: monthly, annual.");
        }

        var annual = billingCycle == "annual";
        var backUrl = !string.IsNullOrWhiteSpace(_options.BackUrl)
            ? _options.BackUrl
            : "https://app.orcafacil.com/MeuPlano";

        var payload = new Dictionary<string, object?>
        {
            ["reason"] = string.IsNullOrWhiteSpace(request.Description) ? "Assinatura OrçaFácil" : request.Description,
            ["payer_email"] = request.PayerEmail,
            ["auto_recurring"] = new Dictionary<string, object?>
            {
                ["frequency"] = annual ? 12 : 1,
                ["frequency_type"] = "months",
                ["transaction_amount"] = decimal.Round(request.Amount, 2),
                ["currency_id"] = "BRL"
            },
            ["back_url"] = backUrl,
            ["external_reference"] = request.ExternalReference,
            ["status"] = "pending"
        };

        if (!string.IsNullOrWhiteSpace(_options.NotificationUrl))
        {
            payload["notification_url"] = _options.NotificationUrl;
        }

        var response = await SendAsync("/preapproval", HttpMethod.Post, payload, ct, request.IdempotencyKey);
        if (!response.IsSuccess)
        {
            return new PaymentGatewayResult(false, null, response.StatusCode, Error: response.Message);
        }

        return ParseSubscriptionResult(response.Body);
    }

    public async Task<PaymentGatewayStatus> GetPaymentStatusAsync(string externalPaymentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalPaymentId))
            return new PaymentGatewayStatus(string.Empty, "invalid_payment_id", "{}");

        if (!IsEnabled())
            return new PaymentGatewayStatus(externalPaymentId, "provider_not_configured", "{}");

        var response = await SendAsync($"/v1/payments/{externalPaymentId}", HttpMethod.Get, ct: ct);
        if (!response.IsSuccess)
            return new PaymentGatewayStatus(externalPaymentId, response.StatusCode, response.Body);

        using var document = JsonDocument.Parse(response.Body);
        var status = GetString(document.RootElement, "status") ?? "pending";
        return new PaymentGatewayStatus(externalPaymentId, status, response.Body);
    }

    public async Task<PaymentGatewayWebhookResult> HandleWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return new PaymentGatewayWebhookResult("unvalidated", null, "empty_payload", false);

        if (!IsEnabled() || string.IsNullOrWhiteSpace(_options.WebhookSecret))
            return new PaymentGatewayWebhookResult("unvalidated", null, "provider_not_configured", false);

        var signatureHeader = GetHeader(headers, "x-signature", "x-signature-sha256");
        var requestId = GetHeader(headers, "x-request-id") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(signatureHeader))
            return new PaymentGatewayWebhookResult("unvalidated", null, "missing_signature", false);

        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return new PaymentGatewayWebhookResult("unvalidated", null, "invalid_json", false);
        }

        using (document)
        {
            var root = document.RootElement;
            var eventType = GetString(root, "type") ?? GetString(root, "topic") ?? "payment";
            var action = GetString(root, "action");
            var dataId = GetString(root, "data.id") ?? GetString(root, "resource.id") ?? GetString(root, "id");

            if (string.IsNullOrWhiteSpace(dataId))
                return new PaymentGatewayWebhookResult("unvalidated", null, "missing_data_id", false);

            if (!VerifySignature(rawBody, signatureHeader, requestId, dataId, _options.WebhookSecret))
                return new PaymentGatewayWebhookResult("unvalidated", dataId, "invalid_signature", false);

            var tsPart = ExtractTsFromSignature(signatureHeader) ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var eventKey = $"mp:{eventType}:{dataId}:{action ?? "status"}:{tsPart}";

            return new PaymentGatewayWebhookResult(
                eventKey,
                dataId,
                "verified",
                true,
                Topic: eventType,
                Action: action,
                RequestId: requestId);
        }
    }

    private async Task<PaymentGatewayResult> CreatePaymentAsync(string paymentKind, PaymentGatewayRequest request, CancellationToken ct)
    {
        if (!IsEnabled())
            return UnavailableAsync();

        var payload = new Dictionary<string, object?>
        {
            ["transaction_amount"] = decimal.Round(request.Amount, 2),
            ["description"] = string.IsNullOrWhiteSpace(request.Description) ? "OrçaFácil cobrança" : request.Description,
            ["payment_method_id"] = paymentKind == "pix" ? "pix" : "bolbradesco",
            ["external_reference"] = request.ExternalReference,
            ["payer"] = new Dictionary<string, object?>
            {
                ["email"] = request.PayerEmail,
                ["identification"] = new Dictionary<string, object?>
                {
                    ["type"] = request.DocumentType,
                    ["number"] = request.DocumentNumber
                }
            },
            ["date_of_expiration"] = DateTime.UtcNow.AddMinutes(paymentKind == "pix" ? _options.PixExpirationMinutes : _options.BoletoExpirationDays * 24 * 60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["idempotency_key"] = request.IdempotencyKey
        };

        if (!string.IsNullOrWhiteSpace(_options.NotificationUrl))
        {
            payload["notification_url"] = _options.NotificationUrl;
        }

        var response = await SendAsync("/v1/payments", HttpMethod.Post, payload, ct, request.IdempotencyKey);
        if (!response.IsSuccess)
            return new PaymentGatewayResult(false, null, response.StatusCode, Error: response.Message);

        return ParsePaymentResult(response.Body, paymentKind);
    }

    private PaymentGatewayResult UnavailableAsync()
    {
        var code = !_options.Enabled || string.IsNullOrWhiteSpace(_options.AccessToken)
            ? "provider_not_configured" : "provider_integration_unavailable";
        return new PaymentGatewayResult(false, null, code, Error: "Checkout indisponível no momento. Fale com a MNSOFT.");
    }

    private async Task<ProviderResponse> SendAsync(string path, HttpMethod method, object? payload = null, CancellationToken ct = default, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("X-Idempotency-Key", idempotencyKey);
            request.Headers.Add("X-Request-Id", idempotencyKey);
        }
        if (payload is not null)
            request.Content = JsonContent.Create(payload);

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new ProviderResponse(false, body, response.StatusCode.ToString());
            return new ProviderResponse(true, body, "ok");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ProviderResponse(false, "{\"message\":\"gateway_timeout\"}", "timeout");
        }
        catch (Exception ex)
        {
            return new ProviderResponse(false, $"{{\"message\":\"{EscapeJson(ex.Message)}\"}}", "provider_error");
        }
    }

    private bool IsEnabled() => _options.Enabled && !string.IsNullOrWhiteSpace(_options.AccessToken);

    private static string EscapeJson(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    internal static bool VerifySignature(string rawBody, string signatureHeader, string requestId, string dataId, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        // 1. Official Mercado Pago manifest verification (ts=...,v1=...)
        var ts = ExtractTsFromSignature(signatureHeader);
        var v1 = ExtractV1FromSignature(signatureHeader);

        if (!string.IsNullOrWhiteSpace(ts) && !string.IsNullOrWhiteSpace(v1))
        {
            // Manifest format: id:[data.id];request-id:[x-request-id];ts:[ts];
            var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
            var expectedBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(manifest));
            var expectedHex = Convert.ToHexString(expectedBytes).ToLowerInvariant();
            try
            {
                var suppliedBytes = Encoding.UTF8.GetBytes(v1.ToLowerInvariant());
                var expBytes = Encoding.UTF8.GetBytes(expectedHex);
                if (CryptographicOperations.FixedTimeEquals(expBytes, suppliedBytes))
                    return true;
            }
            catch
            {
                // Proceed to fallback
            }
        }

        // 2. Fallback: payload-level HMAC (e.g. sha256=... or raw body hash) for legacy or direct webhook tests
        var candidate = signatureHeader.Trim();
        if (candidate.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            candidate = candidate[7..];

        var rawExpected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        try
        {
            var suppliedBytes = Encoding.UTF8.GetBytes(candidate.ToLowerInvariant());
            var expBytes = Encoding.UTF8.GetBytes(rawExpected);
            if (suppliedBytes.Length == expBytes.Length && CryptographicOperations.FixedTimeEquals(expBytes, suppliedBytes))
                return true;
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string? ExtractTsFromSignature(string header)
    {
        foreach (var part in header.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("ts=", StringComparison.OrdinalIgnoreCase))
                return trimmed[3..].Trim();
        }
        return null;
    }

    private static string? ExtractV1FromSignature(string header)
    {
        foreach (var part in header.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))
                return trimmed[3..].Trim();
        }
        return null;
    }

    private static string? GetHeader(IReadOnlyDictionary<string, string> headers, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (headers.TryGetValue(key, out var value))
                return value;
            var lowered = key.ToLowerInvariant();
            foreach (var pair in headers)
            {
                if (string.Equals(pair.Key, lowered, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }
        }
        return null;
    }

    private PaymentGatewayResult ParseSubscriptionResult(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var id = GetString(root, "id") ?? GetString(root, "external_reference");
            var status = GetString(root, "status") ?? "pending";
            var initPoint = GetString(root, "init_point");
            var sandboxInitPoint = GetString(root, "sandbox_init_point");
            var isSandbox = _options.Environment.Equals("Sandbox", StringComparison.OrdinalIgnoreCase);
            var checkoutUrl = isSandbox ? (sandboxInitPoint ?? initPoint) : (initPoint ?? sandboxInitPoint);

            var succeeded = status is "pending" or "authorized" or "active";
            return new PaymentGatewayResult(succeeded, id, status, RawResponseJson: body, CheckoutUrl: checkoutUrl);
        }
        catch (JsonException ex)
        {
            return new PaymentGatewayResult(false, null, "invalid_json", Error: ex.Message, RawResponseJson: body);
        }
    }

    private static PaymentGatewayResult ParsePaymentResult(string body, string paymentKind)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var id = GetString(root, "id") ?? GetString(root, "external_reference");
        var status = GetString(root, "status") ?? "pending";

        var pixQrCode = GetString(root, "point_of_interaction.transaction_data.qr_code");
        var pixQrCodeBase64 = GetString(root, "point_of_interaction.transaction_data.qr_code_base64");
        var pixTicketUrl = GetString(root, "point_of_interaction.transaction_data.ticket_url");

        var rawBoleto = GetString(root, "transaction_details.external_resource_url");
        var boletoUrl = (rawBoleto != null && Uri.TryCreate(rawBoleto, UriKind.Absolute, out var parsedUri) &&
            (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
            ? rawBoleto : null;
        var boletoBarcode = GetString(root, "barcode.content");

        var succeeded = status is "approved" or "authorized" or "in_process" or "pending" or "active";
        if (paymentKind == "pix")
            return new PaymentGatewayResult(succeeded, id, status, PixQrCode: pixQrCode, PixQrCodeBase64: pixQrCodeBase64, PixTicketUrl: pixTicketUrl, RawResponseJson: body);
        if (paymentKind == "boleto")
            return new PaymentGatewayResult(succeeded, id, status, BoletoUrl: boletoUrl, BoletoBarcode: boletoBarcode, RawResponseJson: body);
        return new PaymentGatewayResult(succeeded, id, status, RawResponseJson: body);
    }

    private static string? GetString(JsonElement element, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        JsonElement current = element;
        foreach (var token in path.Split('.'))
        {
            if (!current.ValueKind.Equals(JsonValueKind.Object) || !current.TryGetProperty(token, out var next))
                return null;
            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => null,
            _ => current.GetRawText()
        };
    }

    private readonly record struct ProviderResponse(bool IsSuccess, string Body, string StatusCode)
    {
        public string Message => string.IsNullOrWhiteSpace(Body) ? StatusCode : Body;
    }
}
