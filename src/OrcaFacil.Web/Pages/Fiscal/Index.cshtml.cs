using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Fiscal;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Fiscal;

/// <summary>One fiscalization request pre-shaped for the view.</summary>
public sealed record FiscalRequestView(
    Guid Id, string Description, string ClientName, decimal Received,
    string DocType, string StatusLabel, string StatusBadge,
    bool IsOpen, bool IsIssued, string? Number, string? KeyMasked,
    DateTime? IssuedAt, DateTime RequestedAt);

public sealed record ReceivableOption(Guid Id, string Label);

/// <summary>
/// Manual fiscalization over received money. Opens note requests for paid receivables and lets the operator record the
/// issued number/key, cancel, or mark as not required — always within the current account and never fabricating tax data.
/// </summary>
[Authorize]
public sealed class IndexModel(
    OrcaFacilDbContext db,
    ICurrentAccountService account,
    ICurrentUserService user,
    IFiscalDocumentWorkflowService workflow) : PageModel
{
    public IReadOnlyList<FiscalRequestView> Items { get; private set; } = [];
    public IReadOnlyList<ReceivableOption> Receivables { get; private set; } = [];
    public int OpenCount { get; private set; }
    public int IssuedCount { get; private set; }
    public int UnfiscalizedCount { get; private set; }
    public string SuccessMessage { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return Forbid();
        if (!await CanAccessAsync(ct)) return Forbid();
        await LoadAsync(accountId, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRequestAsync(Guid receivableId, string? docType, string? notes, CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return Forbid();
        if (!await CanAccessAsync(ct)) return Forbid();
        try
        {
            var entry = await db.FinancialEntries.FirstOrDefaultAsync(
                x => x.Id == receivableId && x.AccountId == accountId && !x.IsDeleted, ct)
                ?? throw new InvalidOperationException("Selecione um recebível válido.");
            await workflow.RequestAsync(accountId, ActorId(), entry, docType, notes, ct);
            SuccessMessage = "Solicitação de nota fiscal criada.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        await LoadAsync(accountId, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostIssueAsync(Guid requestId, string number, string? key, DateTime? date, string justification, CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return Forbid();
        if (!await CanAccessAsync(ct)) return Forbid();
        try
        {
            var request = await workflow.RegisterIssuanceAsync(accountId, ActorId(), requestId, number, key, date, justification, ct);
            SuccessMessage = $"Nota fiscal registrada (nº {request.ExternalNumber}).";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        await LoadAsync(accountId, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid requestId, string reason, CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return Forbid();
        if (!await CanAccessAsync(ct)) return Forbid();
        try
        {
            await workflow.CancelAsync(accountId, ActorId(), requestId, reason, ct);
            SuccessMessage = "Solicitação de nota cancelada.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        await LoadAsync(accountId, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostNotRequiredAsync(Guid requestId, string reason, CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return Forbid();
        if (!await CanAccessAsync(ct)) return Forbid();
        try
        {
            await workflow.MarkNotRequiredAsync(accountId, ActorId(), requestId, reason, ct);
            SuccessMessage = "Recebível marcado como sem necessidade de nota.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        await LoadAsync(accountId, ct);
        return Page();
    }

    private Guid ActorId() => user.TryGetUserId() ?? Guid.Empty;

    private async Task<bool> CanAccessAsync(CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return false;
        await account.EnsureAccountAccessAsync(ct);
        return await account.HasPermissionAsync(PermissionCodes.FinanceManage, ct);
    }

    private async Task LoadAsync(Guid accountId, CancellationToken ct)
    {
        var requests = await db.FiscalDocumentRequests.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderByDescending(x => x.RequestedAt)
            .Take(200)
            .ToListAsync(ct);

        var clientIds = requests.Where(r => r.ClientId.HasValue).Select(r => r.ClientId!.Value).Distinct().ToList();
        var entryIds = requests.Where(r => r.ReceivableId.HasValue).Select(r => r.ReceivableId!.Value).Distinct().ToList();
        var clientMap = await db.Clients.AsNoTracking()
            .Where(c => clientIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var entryRows = await db.FinancialEntries.AsNoTracking()
            .Where(e => entryIds.Contains(e.Id)).Select(e => new { e.Id, e.Description, e.PaidAmount }).ToListAsync(ct);
        var entryMap = entryRows.ToDictionary(e => e.Id);

        Items = requests.Select(r =>
        {
            var open = FiscalRequestRules.IsActive(r.Status);
            var issued = r.Status == FiscalRequestStatus.Issued;
            var (label, badge) = Badge(r.Status);
            string desc, name; decimal received;
            if (r.ReceivableId.HasValue && entryMap.TryGetValue(r.ReceivableId.Value, out var e))
            {
                desc = e.Description; received = e.PaidAmount;
            }
            else { desc = "(recebível removido)"; received = 0m; }
            name = r.ClientId.HasValue && clientMap.TryGetValue(r.ClientId.Value, out var cn) ? cn : "—";
            return new FiscalRequestView(
                r.Id, desc, name, received,
                string.IsNullOrWhiteSpace(r.FiscalDocumentType) ? "—" : r.FiscalDocumentType,
                label, badge, open, issued,
                r.ExternalNumber, r.ExternalKeyMasked, r.IssuedAt, r.RequestedAt);
        }).ToList();

        OpenCount = await db.FiscalDocumentRequests.CountAsync(x =>
            x.AccountId == accountId && !x.IsDeleted &&
            (x.Status == FiscalRequestStatus.Pending || x.Status == FiscalRequestStatus.InReview), ct);
        IssuedCount = await db.FiscalDocumentRequests.CountAsync(x =>
            x.AccountId == accountId && !x.IsDeleted && x.Status == FiscalRequestStatus.Issued, ct);
        UnfiscalizedCount = await db.FinancialEntries.CountAsync(x =>
            x.AccountId == accountId && !x.IsDeleted && x.Status != FinancialEntryStatus.Canceled && x.PaidAmount > 0m &&
            !db.FiscalDocumentRequests.Any(r => r.AccountId == accountId && r.ReceivableId == x.Id && r.Status == FiscalRequestStatus.Issued && !r.IsDeleted), ct);

        var paidEntries = await db.FinancialEntries.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status != FinancialEntryStatus.Canceled && x.PaidAmount > 0m)
            .OrderByDescending(x => x.PaidAmount)
            .Take(200)
            .Select(x => new { x.Id, x.Description, x.PaidAmount, x.ClientId })
            .ToListAsync(ct);
        var paidClientIds = paidEntries.Select(p => p.ClientId).Distinct().ToList();
        var paidClientMap = await db.Clients.AsNoTracking()
            .Where(c => paidClientIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        Receivables = paidEntries
            .Select(p => new ReceivableOption(p.Id, $"{p.Description} · {(paidClientMap.TryGetValue(p.ClientId, out var cn) ? cn : "cliente")} · {p.PaidAmount:C} recebido"))
            .OrderBy(r => r.Label, StringComparer.Ordinal)
            .Take(150)
            .ToList();
    }

    private static (string Label, string Badge) Badge(FiscalRequestStatus status) => status switch
    {
        FiscalRequestStatus.Pending => ("Pendente", "of-badge of-badge-warning"),
        FiscalRequestStatus.InReview => ("Em análise", "of-badge of-badge-warning"),
        FiscalRequestStatus.Issued => ("Emitida", "of-badge of-badge-success"),
        FiscalRequestStatus.Canceled => ("Cancelada", "of-badge"),
        FiscalRequestStatus.NotRequired => ("Sem necessidade", "of-badge"),
        _ => (status.ToString(), "of-badge"),
    };
}
