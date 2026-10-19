using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages.WorkOrders;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account, IPlanAccessService plans) : PageModel
{
    public IReadOnlyList<WorkOrder> Orders { get; private set; } = [];
    public string? Search { get; private set; }
    public WorkOrderStatus? Status { get; private set; }
    public string? Origin { get; private set; }
    public string? Phase { get; private set; }
    public string? Finance { get; private set; }
    public string? Agenda { get; private set; }
    public async Task<IActionResult> OnGetAsync(string? search, WorkOrderStatus? status, string? origin, DateTime? from, DateTime? to, string? phase, string? finance, string? agenda, CancellationToken ct)
    {
        Search = search; Status = status; Origin = origin; Phase = phase; Finance = finance; Agenda = agenda; var accountId = account.AccountId; if (accountId is null) return Page();
        var decision = await plans.CanUseAsync(accountId.Value, PlanFeatureCodes.WorkOrdersEnabled, ct);
        if (!decision.IsAllowed)
        {
            TempData.Warning("As ordens de serviço estão disponíveis a partir do plano Profissional. Conheça os planos.");
            return RedirectToPage("/Subscription/Index");
        }
        var query = db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Number.Contains(search) || x.Title.Contains(search));
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (string.Equals(phase, "active", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Status != WorkOrderStatus.Completed && x.Status != WorkOrderStatus.Cancelled);
        if (string.Equals(agenda, "upcoming", StringComparison.OrdinalIgnoreCase))
        {
            var start = DateTime.UtcNow;
            var end = start.AddDays(7);
            query = query.Where(x => x.ScheduledStart != null && x.ScheduledStart >= start && x.ScheduledStart <= end
                && x.Status != WorkOrderStatus.Completed && x.Status != WorkOrderStatus.Cancelled);
        }
        if (string.Equals(finance, "open", StringComparison.OrdinalIgnoreCase))
            query = query.Where(order => order.Status != WorkOrderStatus.Cancelled && order.TotalSnapshot >
                (db.ManualPayments.Where(payment => payment.AccountId == accountId && payment.WorkOrderId == order.Id && !payment.IsDeleted && payment.Status == FinancialRecordStatus.Active)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0m));
        if (origin == "proposal") query = query.Where(x => x.SourceDocumentId != null);
        if (origin == "manual") query = query.Where(x => x.SourceDocumentId == null);
        if (from.HasValue) query = query.Where(x => x.ScheduledStart >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(x => x.ScheduledStart < to.Value.Date.AddDays(1).ToUniversalTime());
        Orders = await query.OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
        return Page();
    }
}
