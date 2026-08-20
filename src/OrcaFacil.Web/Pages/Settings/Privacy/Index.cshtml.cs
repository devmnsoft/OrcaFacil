using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Privacy;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Settings.Privacy;

[Authorize(Policy = "Permission:Privacy.View")]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService current,
    ConsentService consentService, DataSubjectRequestService requestService) : PageModel
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

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await current.EnsureAccountAccessAsync(ct);
        if (!current.AccountId.HasValue) return Forbid();
        var accountId = current.AccountId.Value;
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
}
