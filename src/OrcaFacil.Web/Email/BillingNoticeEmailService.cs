using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Billing;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Security;

namespace OrcaFacil.Web.Email;

/// <summary>
/// Avisos transacionais de cobrança: cada transição REAL registrada pelo
/// BillingStatusService (events billing.overdue / grace_ended / suspended /
/// restored) gera UM e-mail por (evento, assinatura, dia), idempotente pela
/// chave única da outbox — o mesmo aviso nunca é repetido em nova varredura do
/// worker. Sem titular com e-mail ou sem SMTP configurado, nada é enfileirado
/// por engano: este colaborador apenas escreve na outbox; a entrega é do
/// EmailOutboxWorker, que retrata falhas sem simular sucesso.
/// </summary>
public sealed class BillingNoticeEmailService(
    OrcaFacilDbContext db,
    IDataProtectionProvider protection,
    IOptions<ApplicationUrlOptions> appOptions,
    IOptions<SecuritySecretOptions> secrets,
    ILogger<BillingNoticeEmailService> logger) : IBillingNoticeService
{
    private static readonly string[] Notifiable = BillingStatusService.NoticeEventTypes.ToArray();

    public async Task NotifyAsync(Subscription subscription, string eventType, string details, CancellationToken ct = default)
    {
        if (subscription.AccountId is not Guid accountId) return;
        if (!Notifiable.Contains(eventType)) return;

        var ownerEmail = await (
            from member in db.AccountMembers
            join user in db.Users on member.UserId equals user.Id
            where member.AccountId == accountId && member.RoleCode == "Owner"
                  && !member.IsDeleted && member.Status == AccountMemberStatus.Active
                  && !user.IsDeleted && user.IsActive
            select user.Email).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(ownerEmail)) return;

        var normalized = ownerEmail.Trim().ToLowerInvariant();
        var idempotencyKey = $"billing-notice:{eventType}:{subscription.Id}:{DateTime.UtcNow:yyyyMMdd}";
        if (await db.EmailOutboxMessages.AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct))
            return;

        var (subject, textBody, htmlBody) = Template(eventType, subscription, details);
        var link = $"{appOptions.Value.PublicBaseUrl.TrimEnd('/')}/MeuPlano";
        textBody = $"{textBody}\nAcompanhe e regularize: {link}\nMNSOFT — CNPJ 18.160.057/0001-13";
        htmlBody = htmlBody
            .Replace("{{ACTION_URL}}", System.Net.WebUtility.HtmlEncode(link))
            .Replace("{{FOOTER}}", "MNSOFT · CNPJ 18.160.057/0001-13 · comercial@mnsoft.com.br");

        var recipientProtector = protection.CreateProtector("OrcaFacil.EmailOutbox.Recipient.v1");
        var payloadProtector = protection.CreateProtector("OrcaFacil.EmailOutbox.Payload.v1");
        var recipientHash = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secrets.Value.SecurityEventPepper), Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();

        db.EmailOutboxMessages.Add(new EmailOutboxMessage
        {
            AccountId = accountId,
            TemplateCode = eventType.Replace("billing.", "billing-", StringComparison.Ordinal),
            RecipientHash = recipientHash,
            RecipientMasked = Mask(normalized),
            ProtectedRecipient = recipientProtector.Protect(normalized),
            ProtectedPayload = payloadProtector.Protect(JsonSerializer.Serialize(new OutboxPayload(subject, textBody, htmlBody))),
            Priority = eventType == "billing.suspended" ? EmailPriority.High : EmailPriority.Normal,
            CorrelationId = $"billing:{subscription.Id}",
            IdempotencyKey = idempotencyKey
        });

        try
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("BILLING_NOTICE_ENQUEUED EventType {EventType} SubscriptionId {SubscriptionId}", eventType, subscription.Id);
        }
        catch (DbUpdateException)
        {
            // Índice único da IdempotencyKey: transição repetida no mesmo dia não vira duplicata.
        }
    }

    private static (string Subject, string Text, string Html) Template(string eventType, Subscription subscription, string details)
    {
        var dueAt = subscription.NextDueAt ?? subscription.PaidThroughAt ?? subscription.ExpiresAt;
        var dueText = dueAt?.ToString("dd/MM/yyyy");
        var coverage = subscription.PaidThroughAt ?? subscription.NextDueAt;
        var planName = subscription.Plan.ToString() switch
        {
            "Professional" => "Profissional",
            "Business" => "Negócio",
            "Enterprise" => "Enterprise",
            _ => "Grátis"
        };
        return eventType switch
        {
            "billing.overdue" => ("Fechô: pagamento em atraso",
                $"Sua assinatura venceu em {dueText}. Os benefícios pagos continuam ativos durante o período de regularização. Pague a fatura para manter tudo funcionando.{Append(details)}",
                Row("Pagamento em atraso", $"Sua assinatura venceu em {dueText}. Os benefícios pagos continuam ativos durante o período de regularização.")),
            "billing.grace_ended" => ("Fechô: término da regularização",
                $"A carência da fatura que venceu em {dueText} terminou. Regularize agora para não ter os benefícios pausados.{Append(details)}",
                Row("Carência encerrada", $"A fatura que venceu em {dueText} deixou a carência. Regularize agora para evitar a pausa dos benefícios.")),
            "billing.suspended" => ("Fechô: benefícios pagos pausados",
                $"O prazo de regularização da fatura que venceu em {dueText} excedeu e os benefícios pagos estão pausados: sua conta está no plano Grátis. Nenhum dado foi apagado — assim que o pagamento for confirmado, tudo é reativado.{Append(details)}",
                Row("Benefícios pausados", $"O prazo da fatura que venceu em {dueText} terminou e os benefícios pagos estão pausados (plano Grátis). Seus dados permanecem preservados.")),
            "billing.restored" => ("Fechô: pagamento confirmado, benefícios reativos",
                $"Pagamento confirmado. Sua assinatura {planName} está ativa e a vigência vai até {coverage?.ToString("dd/MM/yyyy")}.{Append(details)}",
                Row("Benefícios reativos", $"Pagamento confirmado. A assinatura {planName} está ativa com vigência até {coverage?.ToString("dd/MM/yyyy")}.")),
            _ => ("Fechô: atualização da assinatura", details, Row("Atualização da assinatura", details))
        };

        static string Append(string details) => string.IsNullOrWhiteSpace(details) ? string.Empty : $"\nRegistro: {details}";
    }

    private static string Row(string heading, string message) =>
        $"<div style=\"background:#f4f6f8;padding:32px;font-family:Arial,sans-serif;color:#172033\"><div style=\"max-width:600px;margin:auto;background:#fff\"><div style=\"background:#102a43;color:#fff;padding:24px;font-size:24px;font-weight:bold\">Fechô</div><div style=\"padding:32px\"><h1>{heading}</h1><p>{System.Net.WebUtility.HtmlEncode(message)}</p><p><a href=\"{{{{ACTION_URL}}}}\" style=\"display:inline-block;background:#16865b;color:#fff;padding:14px 22px;text-decoration:none;border-radius:6px\">Acompanhar meu plano</a></p><p style=\"font-size:13px\">{{{{FOOTER}}}}</p></div></div></div>";

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at < 2 ? "***" : $"{email[0]}***{email[(at - 1)..]}";
    }
}
