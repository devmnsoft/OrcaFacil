using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Profile.Privacy;

[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentUserService user, ICurrentAccountService account, IAuditService audit) : PageModel
{
    public IReadOnlyList<LegalAcceptance> Acceptances { get; private set; } = [];
    public IReadOnlyList<CommunicationConsent> Consents { get; private set; } = [];
    public IReadOnlyList<DataSubjectRequest> Requests { get; private set; } = [];
    public IReadOnlyList<PrivacyVendor> Vendors { get; private set; } = [];
    public string PrivacyContactEmail { get; private set; } = string.Empty;
    [BindProperty] public DataSubjectRequestType RequestType { get; set; }
    [BindProperty] public string Description { get; set; } = string.Empty;

    public async Task OnGetAsync([FromServices] IConfiguration configuration, CancellationToken ct)
    {
        await account.EnsureAccountAccessAsync(ct);
        PrivacyContactEmail = configuration["Legal:PrivacyContactEmail"] ?? "privacidade@mnsoft.com.br";
        Acceptances = await db.LegalAcceptances.AsNoTracking().Where(x => x.UserId == user.UserId && !x.IsDeleted).OrderByDescending(x => x.AcceptedAt).ToListAsync(ct);
        Consents = await db.CommunicationConsents.AsNoTracking().Where(x => x.UserId == user.UserId && !x.IsDeleted).OrderBy(x => x.Channel).ThenBy(x => x.Purpose).ToListAsync(ct);
        Requests = await db.DataSubjectRequests.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.RequesterUserId == user.UserId && !x.IsDeleted).OrderByDescending(x => x.RequestedAt).ToListAsync(ct);
        Vendors = await db.PrivacyVendors.AsNoTracking().Where(x => x.IsActive && x.ContractStatus == "Approved" && !x.IsDeleted).OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostRequestAsync(CancellationToken ct)
    {
        await account.EnsureAccountAccessAsync(ct);
        if (account.AccountId is not Guid accountId || string.IsNullOrWhiteSpace(Description) || Description.Trim().Length > 4000)
        { ModelState.AddModelError(nameof(Description), "Descreva a solicitação em até 4.000 caracteres."); await OnGetAsync(HttpContext.RequestServices.GetRequiredService<IConfiguration>(), ct); return Page(); }
        var now = DateTime.UtcNow;
        var request = new DataSubjectRequest { RequesterUserId = user.UserId, AccountId = accountId, Type = RequestType, Description = Description.Trim(), RequestedAt = now, DueAt = now.AddDays(15), CorrelationId = Guid.NewGuid() };
        db.DataSubjectRequests.Add(request);
        await audit.RegisterAsync(user.UserId, "PRIVACY_REQUEST_CREATED", nameof(DataSubjectRequest), request.Id.ToString(), null, new { request.Type, request.Status, request.DueAt }, new { request.CorrelationId }, ct, accountId);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"Solicitação recebida. Protocolo {request.CorrelationId:N}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeMarketingAsync(Guid consentId, CancellationToken ct)
    {
        await account.EnsureAccountAccessAsync(ct);
        var consent = await db.CommunicationConsents.SingleOrDefaultAsync(x => x.Id == consentId && x.UserId == user.UserId && !x.IsDeleted, ct);
        if (consent is null) return NotFound();
        consent.Granted = false; consent.RevokedAt = DateTime.UtcNow; consent.Touch();
        await audit.RegisterAsync(user.UserId, "MARKETING_CONSENT_REVOKED", nameof(CommunicationConsent), consent.Id.ToString(), null, new { consent.Channel, consent.Purpose }, null, ct, account.AccountId);
        await db.SaveChangesAsync(ct); TempData["Success"] = "Consentimento de marketing revogado. O uso do sistema continua normalmente."; return RedirectToPage();
    }
}
