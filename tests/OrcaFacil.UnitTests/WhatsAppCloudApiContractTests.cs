using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OrcaFacil.Application.Omnichannel;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Infrastructure.WhatsApp;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

// ============================================================================
// CONTRATO DA CONTA WHATSAPP CLOUD API (V7.7)
//
// Quatro garantias, todas verificadas sem provedor real:
//  1. Telefone normalizado para E.164 com DDI 55 (o `to` da Cloud API rejeita
//     número local; antes a função era um no-op: os dois ramos do ternário
//     devolviam `digits`).
//  2. Assinatura HMAC-SHA256 do Meta verificada em tempo constante.
//  3. Deduplicação por CONTEÚDO: o `entry.id` do Meta é constante para a conta
//     (é o phone number id), então duas mensagens diferentes chegam em POSTs
//     distintos com o mesmo `entry.id`. Uma chave baseada só nele descartaria a
//     segunda mensagem como "duplicada".
//  4. O cliente real não simula: rede indisponível ou status não-2xx viram
//     `Succeeded = false`, nunca um envio fictício.
// ============================================================================
public sealed class WhatsAppCloudApiContractTests
{
    // ---------- 1. Normalização de telefone (E.164 com DDI) ----------

    [Theory]
    [InlineData("(11) 99999-8888", "5511999998888")]   // local com DDD -> recebe 55
    [InlineData("11 99999-8888", "5511999998888")]
    [InlineData("+55 11 99999-8888", "5511999998888")] // já internacional -> passa adiante
    [InlineData("5511999998888", "5511999998888")]
    [InlineData("011 99999-8888", "5511999998888")]    // zero de operadora é removido
    [InlineData("55 9999-8888", "555599998888")]       // DDD 55 local (10 dígitos)
    [InlineData("55 99999-8888", "5555999998888")]     // DDD 55 local (11 dígitos)
    [InlineData("", "")]
    public void Telefone_e_normalizado_para_e164_com_ddi_brasil(string input, string expected)
        => Assert.Equal(expected, WhatsAppSignature.NormalizePhone(input));

    [Fact]
    public void Telefone_vazio_ou_so_formatado_nao_vira_codigo_de_pais()
    {
        // Símbolos não viram número, e "55" sozinho não é um telefone.
        Assert.Equal(string.Empty, WhatsAppSignature.NormalizePhone("  () "));
        Assert.Equal("55", WhatsAppSignature.NormalizePhone("55"));
    }

    // ---------- 2. Assinatura HMAC do Meta ----------

    private const string AppSecret = "app-secret-de-teste";

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret));
        return "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }

    [Fact]
    public void Assinatura_valida_e_aceita()
    {
        const string body = """{"object":"whatsapp_business_account"}""";
        Assert.True(WhatsAppSignature.Verify(AppSecret, body, Sign(body)));
    }

    [Fact]
    public void Assinatura_valida_com_hex_maiusculo_e_aceita()
    {
        const string body = """{"object":"whatsapp_business_account"}""";
        Assert.True(WhatsAppSignature.Verify(AppSecret, body, Sign(body).ToUpperInvariant().Replace("SHA256=", "sha256=")));
    }

    [Fact]
    public void Corpo_alterado_invalida_assinatura()
    {
        var body = """{"object":"whatsapp_business_account"}""";
        Assert.False(WhatsAppSignature.Verify(AppSecret, body + " ", Sign(body)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sha1=abc")]
    [InlineData("sha256=")]
    public void Assinatura_ausente_ou_mal_formada_e_recusada(string? header)
        => Assert.False(WhatsAppSignature.Verify(AppSecret, """{"object":"x"}""", header));

    [Fact]
    public void Segredo_vazio_nunca_valida()
    {
        const string body = """{"object":"x"}""";
        Assert.False(WhatsAppSignature.Verify("", body, Sign(body)));
        Assert.False(WhatsAppSignature.Verify("  ", body, Sign(body)));
    }

    [Fact]
    public void Segredo_errado_nao_valida()
        => Assert.False(WhatsAppSignature.Verify("outro-segredo", """{"object":"x"}""", Sign("""{"object":"x"}""")));

    [Fact]
    public void Header_e_lido_com_ignorar_maiusculas()
        => Assert.True(WhatsAppSignature.Verify(AppSecret, """{"object":"x"}""", Sign("""{"object":"x"}""").Replace("sha256", "SHA256")));

    // ---------- 3. Deduplicação por conteúdo ----------

    private static string Payload(string entryId, params string[] messageIds)
    {
        var messages = string.Join(',', messageIds.Select(id =>
            "{\"id\":\"" + id + "\",\"type\":\"text\",\"from\":\"5511999998888\",\"timestamp\":\"1729545600\"," +
            "\"text\":{\"body\":\"oi\"}}"));

        return "{\"object\":\"whatsapp_business_account\",\"entry\":[{" +
               "\"id\":\"" + entryId + "\",\"changes\":[{\"field\":\"messages\",\"value\":{" +
               "\"metadata\":{\"phone_number_id\":\"111222333\",\"display_phone_number\":\"+55 11 99999-8888\"}," +
               "\"messages\":[" + messages + "]}}]}]}";
    }

    [Fact]
    public void Duas_mensagens_diferentes_com_mesmo_entry_id_tem_chaves_diferentes()
    {
        // Regressão: a chave anterior era `object:entry.id`. Como o Meta usa o phone
        // number id como entry.id, a segunda mensagem real seria descartada como duplicada.
        var primeira = WhatsAppWebhookEventKey.Build(Payload("111222333", "wamid.AAA"));
        var segunda = WhatsAppWebhookEventKey.Build(Payload("111222333", "wamid.BBB"));

        Assert.NotEqual(primeira, segunda);
        Assert.Contains("wamid.AAA", primeira);
        Assert.Contains("wamid.BBB", segunda);
    }

    [Fact]
    public void Retentativa_do_mesmo_payload_produz_a_mesma_chave()
    {
        var body = Payload("111222333", "wamid.AAA");
        Assert.Equal(WhatsAppWebhookEventKey.Build(body), WhatsAppWebhookEventKey.Build(body));
    }

    [Fact]
    public void Ordem_dos_ids_nao_muda_a_chave()
    {
        var um = WhatsAppWebhookEventKey.Build(Payload("111222333", "wamid.AAA", "wamid.BBB"));
        var outro = WhatsAppWebhookEventKey.Build(Payload("111222333", "wamid.BBB", "wamid.AAA"));
        Assert.Equal(um, outro);
    }

    [Fact]
    public void Callback_de_status_tambem_entra_na_chave_com_o_estado()
    {
        var body = """
        { "object": "whatsapp_business_account", "entry": [ { "id": "111222333", "changes": [ { "field": "messages", "value": {
          "metadata": { "phone_number_id": "111222333" },
          "statuses": [ { "id": "wamid.SENT1", "status": "delivered", "timestamp": "1729545600" } ] } } ] } ] }
        """;
        var key = WhatsAppWebhookEventKey.Build(body);
        Assert.Contains("wamid.SENT1|delivered", key);

        // Mesma mensagem, próximo estado: chaves diferentes, senão o status nunca avançaria.
        Assert.NotEqual(key, WhatsAppWebhookEventKey.Build(body.Replace("delivered", "read")));

        // Retentativa do mesmo callback continua deduplicando.
        Assert.Equal(key, WhatsAppWebhookEventKey.Build(body));
    }

    [Fact]
    public void Payload_sem_ids_cai_no_hash_do_corpo_e_permanece_deterministico()
    {
        const string body = """{ "object": "whatsapp_business_account", "entry": [] }""";
        var key = WhatsAppWebhookEventKey.Build(body);
        Assert.Equal(64, key.Length);
        Assert.Equal(key, WhatsAppWebhookEventKey.Build(body));
        Assert.NotEqual(key, WhatsAppWebhookEventKey.Build("""{ "object": "whatsapp_business_account", "entry": [ { "id": "x" } ] }"""));
    }

    [Fact]
    public void Corpo_malformado_nao_quebra_a_chave()
    {
        Assert.Equal(64, WhatsAppWebhookEventKey.Build("{ nao e json").Length);
    }

    // ---------- 4. Cliente real: sem simulação de sucesso ----------

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Exception? Throw { get; set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            if (Throw is not null) throw Throw;
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static WhatsAppGraphClient Client(StubHandler handler) =>
        new(new HttpClient(handler), NullLogger<WhatsAppGraphClient>.Instance);

    [Fact]
    public async Task Resposta_2xx_com_id_e_sucesso_e_devolve_o_id_externo()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"messages":[{"id":"wamid.OUT1"}]}""");
        var result = await Client(handler).SendTextMessageAsync("token", "111222333", "+55 11 99999-8888", "oi");

        Assert.True(result.Succeeded);
        Assert.Equal("wamid.OUT1", result.ExternalMessageId);
        Assert.Null(result.Error);
        Assert.Contains("graph.facebook.com/v21.0/111222333/messages", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task Telefone_e_enviado_normalizado_e_com_ddi()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"messages":[{"id":"wamid.OUT1"}]}""");
        await Client(handler).SendTextMessageAsync("token", "111222333", "(11) 99999-8888", "oi");

        using var doc = System.Text.Json.JsonDocument.Parse(handler.LastBody!);
        Assert.Equal("5511999998888", doc.RootElement.GetProperty("to").GetString());
    }

    [Fact]
    public async Task Status_nao_2xx_vira_falha_e_nunca_sucesso()
    {
        var result = await Client(new StubHandler(HttpStatusCode.Unauthorized,
                """{"error":{"message":"Invalid OAuth access token"}}"""))
            .SendTextMessageAsync("token", "111222333", "5511999998888", "oi");

        Assert.False(result.Succeeded);
        Assert.Null(result.ExternalMessageId);
        Assert.Contains("401", result.Error);
    }

    [Fact]
    public async Task Rede_indisponivel_vira_falha_e_nunca_sucesso()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}")
            { Throw = new HttpRequestException("Connection refused") };

        var result = await Client(handler)
            .SendTextMessageAsync("token", "111222333", "5511999998888", "oi");

        Assert.False(result.Succeeded);
        Assert.Null(result.ExternalMessageId);
        Assert.Contains("Connection refused", result.Error);
    }

    [Fact]
    public async Task Resposta_2xx_sem_id_de_mensagem_e_falha_honestamente()
    {
        // 200 com corpo sem `messages[].id` não pode ser tratado como entregue.
        var result = await Client(new StubHandler(HttpStatusCode.OK, """{"messages":[]}"""))
            .SendTextMessageAsync("token", "111222333", "5511999998888", "oi");

        Assert.False(result.Succeeded);
        Assert.Null(result.ExternalMessageId);
    }

    // ---------- 5. Estado inicial da conta ----------

    [Fact]
    public void Conta_comeca_desabilitada_e_nao_configurada()
    {
        var account = new OmnichannelWhatsAppAccount();
        Assert.False(account.IsEnabled);
        Assert.Equal("NotConfigured", account.Status);
        Assert.Null(account.ProtectedAccessToken);
        Assert.Null(account.ProtectedAppSecret);
    }

    // ---------- 6. Composição (DI) ----------

    [Fact]
    public void Servicos_do_whatsapp_resolvem_no_mesmo_escopo()
    {
        using var factory = new RouteApplicationFactory();
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OmnichannelWhatsAppApiService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IWhatsAppWebhookProcessor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IWhatsAppGraphClient>());
    }
}
