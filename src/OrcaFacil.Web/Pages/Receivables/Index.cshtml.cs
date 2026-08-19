using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Receivables;

[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<Row> Entries { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public Guid? ClientId { get; set; }
    [BindProperty(SupportsGet = true)] public FinancialEntryStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public FinancialEntryOrigin? Origin { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? DueFrom { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? DueTo { get; set; }
    public decimal Pending => Entries.Where(x => x.EffectiveStatus is FinancialEntryStatus.Pending or FinancialEntryStatus.PartiallyPaid or FinancialEntryStatus.Overdue).Sum(x => x.Balance);
    public decimal Overdue => Entries.Where(x => x.EffectiveStatus == FinancialEntryStatus.Overdue).Sum(x => x.Balance);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!account.AccountId.HasValue) return Forbid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = db.FinancialEntries.AsNoTracking().Where(x => x.AccountId == account.AccountId && !x.IsDeleted);
        if (ClientId.HasValue) query = query.Where(x => x.ClientId == ClientId.Value);
        if (Origin.HasValue) query = query.Where(x => x.Origin == Origin);
        if (DueFrom.HasValue) query = query.Where(x => x.DueDate >= DueFrom.Value);
        if (DueTo.HasValue) query = query.Where(x => x.DueDate <= DueTo.Value);
        var rows = await (from entry in query join client in db.Clients.AsNoTracking() on entry.ClientId equals client.Id
            select new { Entry = entry, ClientName = client.Name }).OrderBy(x => x.Entry.DueDate).Take(500).ToListAsync(ct);
        Entries = rows.Select(x => new Row(x.Entry.Id, x.Entry.ClientId, x.ClientName, x.Entry.Description, x.Entry.Origin,
            Effective(x.Entry, today), x.Entry.DueDate, x.Entry.Amount, Math.Max(0, x.Entry.Amount - x.Entry.PaidAmount),
            x.Entry.DocumentId, x.Entry.WorkOrderId, x.Entry.ContractId)).Where(x => !Status.HasValue || x.EffectiveStatus == Status).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, string reason, CancellationToken ct)
    {
        if (!account.AccountId.HasValue || !await account.HasPermissionAsync("finance.manage", ct)) return Forbid();
        if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Informe o motivo do cancelamento."; return RedirectToPage(); }
        var entry = await db.FinancialEntries.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
        if (entry is null) return NotFound();
        if (entry.PaidAmount > 0) { TempData["Error"] = "Uma conta com pagamentos não pode ser cancelada."; return RedirectToPage(); }
        entry.Status = FinancialEntryStatus.Canceled; entry.CanceledAt = DateTime.UtcNow; entry.CanceledByUserId = account.UserId; entry.CancellationReason = reason.Trim();
        await db.SaveChangesAsync(ct); TempData["Success"] = "Conta a receber cancelada."; return RedirectToPage();
    }

    private static FinancialEntryStatus Effective(FinancialEntry x, DateOnly today) =>
        x.Status is FinancialEntryStatus.Pending or FinancialEntryStatus.PartiallyPaid && x.DueDate < today ? FinancialEntryStatus.Overdue : x.Status;
    public sealed record Row(Guid Id, Guid ClientId, string ClientName, string Description, FinancialEntryOrigin Origin,
        FinancialEntryStatus EffectiveStatus, DateOnly DueDate, decimal Amount, decimal Balance, Guid? DocumentId, Guid? WorkOrderId, Guid? ContractId);
}
