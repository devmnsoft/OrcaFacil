using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.Privacy;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Settings.Privacy;

[Authorize(Policy = "Permission:Privacy.View")]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService current,
    ConsentService consentService, DataSubjectRequestService requestService,
    DataExportService exportService, UserUsageService usage) : PageModel
{
    public int ClientsWithDocument { get; private set; }
    public int ClientsWithEmail { get; private set; }
    public int ClientsWithPhone { get; private set; }
    public int AttachedFiles { get; private set; }
    public int ActivePublicLinks { get; private set; }
    public int Exports { get; private set; }
    public int OpenRequests { get; private set; }
    public IReadOnlyList<PrivacyConsent> Consents { get; private set; } = [];
    public IReadOnlyList<DataSubjectRequest> Requests { get; private set; } = [];
    public bool CanExport { get; private set; }
    public bool CanExportFinancial { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await current.EnsureAccountAccessAsync(ct);
        if (!current.AccountId.HasValue) return Forbid();
        var accountId = current.AccountId.Value;
        CanExport = await current.HasPermissionAsync(PermissionCodes.PrivacyExportData, ct);
        CanExportFinancial = await current.HasPermissionAsync(PermissionCodes.FinanceManage, ct);
        ClientsWithDocument = await db.Clients.CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.DocumentNumber != null, ct);
        ClientsWithEmail = await db.Clients.CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.Email != null, ct);
        ClientsWithPhone = await db.Clients.CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.Phone != null, ct);
        AttachedFiles = await db.FileAssets.CountAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        ActivePublicLinks = await db.PublicQuotes.CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.PublicEnabled &&
            (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow), ct);
        Exports = await db.DataExportJobs.CountAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        OpenRequests = await db.DataSubjectRequests.CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.CompletedAt == null && x.RejectedAt == null, ct);
        Consents = await db.PrivacyConsents.AsNoTracking().Where(x => x.AccountId == accountId && x.UserId == current.UserId && !x.IsDeleted).OrderByDescending(x => x.AcceptedAt).ToListAsync(ct);
        Requests = await db.DataSubjectRequests.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderByDescending(x => x.RequestedAt).Take(20).ToListAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAcceptAsync(PrivacyConsentType type, string version, CancellationToken ct)
    {
        await current.EnsureAccountAccessAsync(ct); if (!current.AccountId.HasValue) return Forbid();
        await consentService.AcceptAsync(current.AccountId.Value, current.UserId, type, version,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", Request.Headers.UserAgent.ToString(), ct);
        TempData["Success"] = "Seu aceite foi registrado com data e versão."; return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRequestAsync(DataSubjectRequestType type, string description, CancellationToken ct)
    {
        await current.EnsureAccountAccessAsync(ct); if (!current.AccountId.HasValue) return Forbid();
        await requestService.OpenAsync(current.AccountId.Value, current.UserId, type, description, null, ct);
        TempData["Success"] = "Solicitação LGPD registrada para análise."; return RedirectToPage();
    }

    public async Task<IActionResult> OnGetExportAsync(bool includeFinancial, CancellationToken ct)
    {
        await current.EnsureAccountAccessAsync(ct);
        if (!current.AccountId.HasValue) return Forbid();
        var accountId = current.AccountId.Value;
        var userId = current.UserId;
        if (!await current.HasPermissionAsync(PermissionCodes.PrivacyExportData, ct)) return Forbid();

        var canExportFinancial = await current.HasPermissionAsync(PermissionCodes.FinanceManage, ct);
        var includeFin = includeFinancial && canExportFinancial;
        try
        {
            var clients = await db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderBy(x => x.Name).ToListAsync(ct);
            var services = await db.ServiceCatalogItems.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderBy(x => x.Name).ToListAsync(ct);
            var documents = await db.Documents.AsNoTracking().Include(d => d.Items).Where(x => x.AccountId == accountId && !x.IsDeleted).OrderByDescending(x => x.IssueDate).ToListAsync(ct);
            var workOrders = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
            var receipts = await db.Receipts.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderByDescending(x => x.IssuedAt).ToListAsync(ct);
            var contracts = await db.RecurringContracts.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderByDescending(x => x.StartDate).ToListAsync(ct);
            var fiscalNotes = await db.FiscalDocumentRequests.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderByDescending(x => x.RequestedAt).ToListAsync(ct);
            var financial = includeFin
                ? await db.FinancialEntries.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderByDescending(x => x.DueDate).ToListAsync(ct)
                : null;

            var bundle = AccountDataBackupShaper.Shape(clients, services, documents, workOrders, receipts, includeFin, financial, contracts, fiscalNotes);
            var result = await exportService.ExportJsonAsync(accountId, userId, AccountDataBackupShaper.Scope(includeFin), bundle, canExportFinancial, includeFin, ct);
            await usage.RegisterBackupExportAsync(userId, accountId, ct);

            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["Error"] = ex.Message; return RedirectToPage();
        }
        catch
        {
            TempData["Error"] = "Não foi possível gerar o backup agora. Tente novamente."; return RedirectToPage();
        }
    }
}
