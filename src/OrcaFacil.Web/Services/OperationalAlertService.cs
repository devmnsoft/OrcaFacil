using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public interface IOperationalAlertService { Task GenerateAsync(CancellationToken ct = default); }

/// <summary>Creates idempotent, account-scoped operational alerts from live business data.</summary>
public sealed class OperationalAlertService(ICurrentAccountService account, OrcaFacilDbContext db) : IOperationalAlertService
{
    public async Task GenerateAsync(CancellationToken ct = default)
    {
        if (account.AccountId is not { } accountId) return;
        var now = DateTime.UtcNow; var today = now.Date;
        var candidates = new List<(string Key, string Title, string Message, NotificationType Severity, NotificationCategory Category, string Url, Guid? DocumentId)>();

        var quotes = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.ClientDecision == ClientDecision.Pending).Select(x => new { x.Id, x.Number, x.ValidUntil, x.NextFollowUpAt, x.FollowUpStatus }).ToListAsync(ct);
        foreach (var quote in quotes)
        {
            if (quote.NextFollowUpAt < now && quote.FollowUpStatus != FollowUpStatus.Completed)
                candidates.Add(($"followup:{quote.Id}:{quote.NextFollowUpAt:yyyyMMdd}", "Follow-up atrasado", $"O retorno da proposta {quote.Number} está atrasado.", NotificationType.Danger, NotificationCategory.Document, $"/Documents/Details/{quote.Id}", quote.Id));
            else if (quote.NextFollowUpAt?.Date == today)
                candidates.Add(($"followup-today:{quote.Id}:{today:yyyyMMdd}", "Follow-up para hoje", $"A proposta {quote.Number} precisa de retorno hoje.", NotificationType.Warning, NotificationCategory.Document, $"/Documents/Details/{quote.Id}", quote.Id));
            if (quote.ValidUntil < today)
                candidates.Add(($"expired:{quote.Id}:{quote.ValidUntil:yyyyMMdd}", "Orçamento vencido", $"A validade da proposta {quote.Number} terminou.", NotificationType.Danger, NotificationCategory.Document, $"/Documents/Details/{quote.Id}", quote.Id));
            else if (quote.ValidUntil <= today.AddDays(3))
                candidates.Add(($"expiring:{quote.Id}:{quote.ValidUntil:yyyyMMdd}", "Orçamento próximo do vencimento", $"A proposta {quote.Number} vence em até 3 dias.", NotificationType.Warning, NotificationCategory.Document, $"/Documents/Details/{quote.Id}", quote.Id));
        }
        var orders = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status != WorkOrderStatus.Completed && x.Status != WorkOrderStatus.Cancelled && x.ScheduledEnd < now).Select(x => new { x.Id, x.Number, x.ScheduledEnd }).ToListAsync(ct);
        candidates.AddRange(orders.Select(x => ($"workorder:{x.Id}:{x.ScheduledEnd:yyyyMMdd}", "OS atrasada", $"A OS {x.Number} passou da data prevista.", NotificationType.Danger, NotificationCategory.System, $"/WorkOrders/Details/{x.Id}", (Guid?)null)));
        var pendingPayments = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == WorkOrderStatus.Completed && !x.PaymentReceived).Select(x => new { x.Id, x.Number }).ToListAsync(ct);
        candidates.AddRange(pendingPayments.Select(x => ($"payment:{x.Id}", "Pagamento pendente", $"A OS concluída {x.Number} ainda possui pagamento pendente.", NotificationType.Warning, NotificationCategory.Billing, $"/Payments/Register?workOrderId={x.Id}", (Guid?)null)));

        var existing = await db.Notifications.AsNoTracking().Where(x => x.AccountId == accountId && x.Message.Contains("[alert:")).Select(x => x.Message).ToListAsync(ct);
        foreach (var item in candidates.Where(c => !existing.Any(message => message.Contains($"[alert:{c.Key}]"))))
            db.Notifications.Add(new Notification { AccountId = accountId, UserId = account.UserId, Title = item.Title, Message = $"{item.Message} [alert:{item.Key}]", Type = item.Severity, Category = item.Category, ActionUrl = item.Url, ActionText = "Ver detalhes", DocumentId = item.DocumentId });
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
    }
}
