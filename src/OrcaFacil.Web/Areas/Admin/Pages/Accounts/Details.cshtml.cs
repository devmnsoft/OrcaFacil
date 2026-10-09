using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.Accounts;

[Authorize(Policy = "SuperAdminOnly")]
public sealed class DetailsModel(OrcaFacilDbContext db) : PageModel
{
    public Account360ViewModel Account { get; private set; } = default!;
    public IReadOnlyList<AuditRow> RecentAuditLogs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var a = await db.BusinessAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (a is null) return NotFound();

        var members = await db.AccountMembers.AsNoTracking().CountAsync(x => x.AccountId == id && !x.IsDeleted, ct);
        var subscription = await db.Subscriptions.AsNoTracking().OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.AccountId == id && !x.IsDeleted, ct);

        Account = new(
            a.Id,
            a.DisplayName,
            Mask(a.DocumentNumber),
            a.Email,
            a.CreatedAt,
            a.Status.ToString(),
            subscription?.Plan.ToString() ?? a.CurrentPlanCode,
            subscription?.Status.ToString() ?? "Grátis",
            subscription?.NextDueAt,
            members,
            a.Status == AccountStatus.Active);

        RecentAuditLogs = await db.AuditLogs.AsNoTracking()
            .Where(x => x.AccountId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new AuditRow(x.CreatedAt, x.Action, x.Summary))
            .ToListAsync(ct);

        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Informe a justificativa para alterar o status da conta.";
            return RedirectToPage(new { id });
        }

        var account = await db.BusinessAccounts.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (account is null) return NotFound();

        var previousStatus = account.Status;
        if (account.Status == AccountStatus.Active)
        {
            account.Block(reason);
        }
        else
        {
            account.Activate();
        }

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : (Guid?)null;

        db.AuditLogs.Add(new AuditLog
        {
            AccountId = account.Id,
            UserId = actorId,
            Action = "ACCOUNT_STATUS_CHANGE",
            EntityType = "BusinessAccount",
            EntityId = account.Id.ToString(),
            BeforeJson = JsonSerializer.Serialize(new { Status = previousStatus.ToString() }),
            AfterJson = JsonSerializer.Serialize(new { Status = account.Status.ToString() }),
            Summary = $"Status alterado de {previousStatus} para {account.Status}. Motivo: {reason.Trim()}",
            CorrelationId = Guid.NewGuid()
        });

        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"Status da conta atualizado para {account.Status}.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostManualReleaseAsync(Guid id, int days, string reason, CancellationToken ct)
    {
        if (days <= 0 || days > 365)
        {
            TempData["Error"] = "Informe um número de dias válido entre 1 e 365.";
            return RedirectToPage(new { id });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Informe a justificativa operacional para a liberação manual.";
            return RedirectToPage(new { id });
        }

        var account = await db.BusinessAccounts.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (account is null) return NotFound();

        var subscription = await db.Subscriptions.OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.AccountId == id && !x.IsDeleted, ct);

        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : (Guid?)null;

        if (subscription is null)
        {
            subscription = new Subscription
            {
                AccountId = id,
                UserId = actorId ?? Guid.Empty,
                Plan = PlanType.Professional,
                Status = SubscriptionStatus.ManualRelease,
                StartedAt = now,
                PaidThroughAt = now.AddDays(days),
                NextDueAt = now.AddDays(days),
                BillingCycle = "monthly"
            };
            db.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.Status = SubscriptionStatus.ManualRelease;
            var baseDate = subscription.PaidThroughAt.HasValue && subscription.PaidThroughAt.Value > now
                ? subscription.PaidThroughAt.Value
                : now;
            subscription.PaidThroughAt = baseDate.AddDays(days);
            subscription.NextDueAt = subscription.PaidThroughAt;
            subscription.ManualReleaseUntil = subscription.NextDueAt;
            subscription.Touch();
        }

        db.AuditLogs.Add(new AuditLog
        {
            AccountId = id,
            UserId = actorId,
            Action = "SUBSCRIPTION_MANUAL_RELEASE",
            EntityType = "Subscription",
            EntityId = subscription.Id.ToString(),
            Summary = $"Liberação manual concedida por {days} dias. Vence em {subscription.NextDueAt:dd/MM/yyyy}. Motivo: {reason.Trim()}",
            CorrelationId = Guid.NewGuid()
        });

        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"Benefícios liberados manualmente por {days} dias (até {subscription.NextDueAt:dd/MM/yyyy}).";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostOverridePlanAsync(Guid id, string planCode, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Informe a justificativa operacional para alteração do plano.";
            return RedirectToPage(new { id });
        }

        var account = await db.BusinessAccounts.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (account is null) return NotFound();

        if (!Enum.TryParse<PlanType>(planCode, true, out var parsedPlan))
        {
            parsedPlan = planCode.ToLowerInvariant() switch
            {
                "free" => PlanType.Free,
                "pro" => PlanType.Professional,
                "business" => PlanType.Business,
                "enterprise" => PlanType.Enterprise,
                _ => PlanType.Professional
            };
        }

        var previousPlan = account.CurrentPlanCode;
        account.CurrentPlanCode = parsedPlan.ToString();
        account.Touch();

        var subscription = await db.Subscriptions.OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.AccountId == id && !x.IsDeleted, ct);

        if (subscription is not null)
        {
            subscription.Plan = parsedPlan;
            subscription.Touch();
        }

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : (Guid?)null;

        db.AuditLogs.Add(new AuditLog
        {
            AccountId = id,
            UserId = actorId,
            Action = "SUBSCRIPTION_PLAN_OVERRIDE",
            EntityType = "BusinessAccount",
            EntityId = account.Id.ToString(),
            BeforeJson = JsonSerializer.Serialize(new { Plan = previousPlan }),
            AfterJson = JsonSerializer.Serialize(new { Plan = account.CurrentPlanCode }),
            Summary = $"Plano alterado de {previousPlan} para {account.CurrentPlanCode}. Motivo: {reason.Trim()}",
            CorrelationId = Guid.NewGuid()
        });

        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"Plano atualizado com sucesso para {account.CurrentPlanCode}.";
        return RedirectToPage(new { id });
    }

    private static string Mask(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Não informado" : value.Length < 5 ? "***" : $"***{value[^4..]}";
}

public sealed record Account360ViewModel(
    Guid Id,
    string Name,
    string MaskedDocument,
    string Email,
    DateTime CreatedAt,
    string Status,
    string SelectedPlan,
    string PaymentStatus,
    DateTime? DueAt,
    int Members,
    bool IsActive);

public sealed record AuditRow(DateTime CreatedAt, string Action, string Summary);
