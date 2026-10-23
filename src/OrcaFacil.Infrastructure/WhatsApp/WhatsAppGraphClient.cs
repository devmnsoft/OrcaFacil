using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Omnichannel;

namespace OrcaFacil.Infrastructure.WhatsApp;

/// <summary>
/// Cliente real da Cloud API (Meta) do WhatsApp. Nenhum envio é simulado: erro de
/// rede, status não 2xx ou corpo inválido retornam <see cref="WhatsAppSendResult"/>
/// com <c>Succeeded = false</c>.
/// </summary>
public sealed class WhatsAppGraphClient(HttpClient httpClient, ILogger<WhatsAppGraphClient> logger) : IWhatsAppGraphClient
{
    private const string BaseUrl = "https://graph.facebook.com/v21.0";

    public async Task<WhatsAppSendResult> SendTextMessageAsync(
        string accessToken, string phoneNumberId, string toPhone, string content, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(phoneNumberId)}/messages";
        var payload = new
        {
            messaging_product = "whatsapp",
            to = OrcaFacil.Application.Omnichannel.WhatsAppSignature.NormalizePhone(toPhone),
            type = "text",
            text = new { preview_url = false, body = content }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("WHATSAPP_SEND_HTTP {Status} PhoneNumberId {PhoneNumberId}",
                    (int)response.StatusCode, phoneNumberId);
                return new WhatsAppSendResult(false, null, $"HTTP {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("messages", out var messages) &&
                messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0 &&
                messages[0].TryGetProperty("id", out var idElement) &&
                idElement.ValueKind == JsonValueKind.String)
            {
                return new WhatsAppSendResult(true, idElement.GetString(), null);
            }

            return new WhatsAppSendResult(false, null, "Resposta da Cloud API sem id de mensagem.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WHATSAPP_SEND_FAILED PhoneNumberId {PhoneNumberId}", phoneNumberId);
            return new WhatsAppSendResult(false, null, ex.Message);
        }
    }

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500];
}
