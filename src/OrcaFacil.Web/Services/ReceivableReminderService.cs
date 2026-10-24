using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Payments;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Email;
using OrcaFacil.Web.Security;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Services;

/// <summary>Cross-account dunning for open receivables. It tells the end client what is still owed, by e-mail
/// and/or WhatsApp, and moves nothing: no payment, no amount change. Cadence and the reminder cap live on the
/// receivable itself; the e-mail is idempotent by (receivable, day) through the outbox unique key.</summary>
public sealed class ReceivableReminderService(
    OrcaFacilDbContext db,
    IDataProtectionProvider protection,
    IOptions<SecuritySecretOptions> secrets,
    OmnichannelWhatsAppApiService whatsapp,
    IOptions<ReceivableReminderOptions> policy,
    ILogger<ReceivableReminderService> logger) : IReceivableReminderService
{
    private const int BatchSize = 500;
    private readonly ReceivableReminderOptions _policy = policy.Value;

    public async Task<ReceivableReminderRunResult> RunAsync(DateOnly today, CancellationToken ct = default)
    {
        if (!_policy.Enabled) return new(0, 0, 0, 0);
        var grace = Math.Max(0, _policy.GraceDays);
        var threshold = today.AddDays(-grace);

        var entries = await db.FinancialEntries
            .Where(x => !x.IsDeleted
                && (x.Status == FinancialEntryStatus.Pending || x.Status == FinancialEntryStatus.PartiallyPaid)
                && x.DueDate <= threshold)
            .OrderBy(x => x.DueDate)
            .Take(BatchSize)
            .ToListAsync(ct);
        if (entries.Count == 0) return new(0, 0, 0, 0);

        var accountIds = entries.Select(x => x.AccountId).Distinct().ToHashSet();
        var clientIds = entries.Select(x => x.ClientId).Distinct().ToHashSet();

        var integrations = await db.IntegrationSettings
            .Where(s => !s.IsDeleted && accountIds.Contains(s.AccountId))
            .ToDictionaryAsync(s => s.AccountId, ct);
        var clients = await db.Clients.AsNoTracking()
            .Where(c => clientIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);
        var whatsappEnabled = await db.OmnichannelWhatsAppAccounts
            .Where(a => !a.IsDeleted && a.IsEnabled && accountIds.Contains(a.AccountId))
            .Select(a => a.AccountId)
            .Distinct()
            .ToListAsync(ct);
        var whatsappAccounts = whatsappEnabled.ToHashSet();

        var emails = 0; var sentWhatsApps = 0; var reminded = 0;
        var touchedAccounts = new HashSet<Guid>();

        foreach (var entry in entries)
        {
            if (!ReceivableReminderRules.ShouldRemind(entry.Status, entry.Amount, entry.PaidAmount,
                    entry.DueDate, today, entry.LastReminderAt, entry.ReminderCount,
                    _policy.GraceDays, _policy.IntervalDays, _policy.MaxReminders))
                continue;

            var accountId = entry.AccountId;
            if (!clients.TryGetValue(entry.ClientId, out var client) || string.IsNullOrWhiteSpace(client.Name)) continue;

            var emailOk = false;
            if (integrations.GetValueOrDefault(accountId)?.EmailSendingEnabled == true && !string.IsNullOrWhiteSpace(client.Email))
            {
                var emailHash = IdentityHash(client.Email.Trim().ToLowerInvariant());
                emailOk = !await db.OmnichannelOptOutEvents.AnyAsync(
                    x => x.AccountId == accountId && x.Channel == "Email" && x.Scope == "Commercial" && x.IdentityHash == emailHash, ct);
            }
            var waOk = !string.IsNullOrWhiteSpace(client.Phone) && whatsappAccounts.Contains(accountId);
            if (!emailOk && !waOk) continue; // nobody to reach yet; leave it for when a channel exists

            if (emailOk && await EnqueueEmailAsync(accountId, client, entry, $"receivable-reminder:{entry.Id:N}:{today:yyyyMMdd}", ct))
                emails++;
            if (waOk)
            {
                var result = await whatsapp.SendAsync(accountId, client.Phone!, ReminderText(client, entry), null, ct);
                if (result.Succeeded) sentWhatsApps++;
            }

            entry.LastReminderAt = today;
            entry.ReminderCount += 1;
            entry.Touch();
            db.ActivityEvents.Add(new ActivityEvent
            {
                AccountId = accountId, Action = "ReceivableReminded", EntityType = "FinancialEntry", EntityId = entry.Id,
                Summary = $"Lembrete de cobrança enviado (saldo {ReceivableReminderRules.OpenBalance(entry.Amount, entry.PaidAmount):C}, venceu em {entry.DueDate:dd/MM/yyyy})."
            });
            touchedAccounts.Add(accountId);
            reminded++;
        }

        if (reminded > 0) await db.SaveChangesAsync(ct);
        logger.LogInformation("RECEIVABLE_REMINDER_COMPLETED Accounts {Accounts} Reminded {Reminded} Emails {Emails} WhatsApps {WhatsApps}",
            touchedAccounts.Count, reminded, emails, sentWhatsApps);
        return new(touchedAccounts.Count, reminded, emails, sentWhatsApps);
    }

    private async Task<bool> EnqueueEmailAsync(Guid accountId, Client client, FinancialEntry entry, string idempotencyKey, CancellationToken ct)
    {
        if (await db.EmailOutboxMessages.AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct)) return false;
        var normalized = client.Email!.Trim().ToLowerInvariant();
        var balance = ReceivableReminderRules.OpenBalance(entry.Amount, entry.PaidAmount);
        var subject = "Fechô: cobrança vencida";
        var text = $"{Greeting(client)}, o pagamento de {balance:C} ({Clean(entry.Description)}) venceu em {entry.DueDate:dd/MM/yyyy} e segue em aberto.\n" +
                   "Regularize para evitar novo lembrete. Suporte: comercial@mnsoft.com.br\nMNSOFT — CNPJ 18.160.057/0001-13";
        var html = Row(Greeting(client), $"{Greeting(client)}, o pagamento de {balance:C} ({WebUtility.HtmlEncode(Clean(entry.Description))}) venceu em {entry.DueDate:dd/MM/yyyy} e segue em aberto. Regularize para evitar novo lembrete.");
        var pepper = secrets.Value.SecurityEventPepper;

        db.EmailOutboxMessages.Add(new EmailOutboxMessage
        {
            AccountId = accountId,
            TemplateCode = "receivable-reminder",
            RecipientHash = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant(),
            RecipientMasked = Mask(normalized),
            ProtectedRecipient = protection.CreateProtector("OrcaFacil.EmailOutbox.Recipient.v1").Protect(normalized),
            ProtectedPayload = protection.CreateProtector("OrcaFacil.EmailOutbox.Payload.v1").Protect(System.Text.Json.JsonSerializer.Serialize(new OutboxPayload(subject, text, html))),
            Priority = EmailPriority.Normal,
            CorrelationId = $"receivable:{entry.Id:N}",
            IdempotencyKey = idempotencyKey
        });
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false; // same receivable already enqueued for this day
        }
    }

    private static string ReminderText(Client client, FinancialEntry entry) =>
        $"{Greeting(client)}, o pagamento de {ReceivableReminderRules.OpenBalance(entry.Amount, entry.PaidAmount):C} ({Clean(entry.Description)}) " +
        $"venceu em {entry.DueDate:dd/MM/yyyy}. Regularize para evitar novo lembrete.";

    private static string Greeting(Client client) => $"Olá, {client.Name.Split(' ').First()}";
    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "este serviço" : value.Trim();
    private static string IdentityHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string Row(string greeting, string message) =>
        $"<div style=\"background:#f4f6f8;padding:32px;font-family:Arial,sans-serif;color:#172033\"><div style=\"max-width:600px;margin:auto;background:#fff\"><div style=\"background:#102a43;color:#fff;padding:24px;font-size:24px;font-weight:bold\">Fechô</div><div style=\"padding:32px\"><p>{greeting}</p><p>{message}</p><p style=\"font-size:13px\">MNSOFT · CNPJ 18.160.057/0001-13 · comercial@mnsoft.com.br</p></div></div></div>";

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at < 2 ? "***" : $"{email[0]}***{email[(at - 1)..]}";
    }
}
