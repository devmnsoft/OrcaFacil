using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.SuperAdmin.Finance;

/// <summary>
/// Restricted, read-only investigation screen for historical financial link divergences.
/// It never corrects data: a SuperAdministrator records an audited review decision per occurrence.
/// </summary>
[Authorize(Policy = "SuperAdminOnly")]
public sealed class DivergencesModel(
    IFinancialIntegrityDiagnosticService diagnostics,
    OrcaFacilDbContext db) : PageModel
{
    public static readonly string[] ReviewDecisions =
        ["Divergência confirmada", "Falso positivo", "Correção manual aplicada", "Em análise"];

    [BindProperty(SupportsGet = true)] public Guid? AccountId { get; set; }
    [BindProperty] public string FindingId { get; set; } = string.Empty;
    [BindProperty] public Guid OccurrenceId { get; set; }
    [BindProperty] public Guid FindingAccountId { get; set; }
    [BindProperty] public string Decision { get; set; } = ReviewDecisions[0];
    [BindProperty] public string Note { get; set; } = string.Empty;

    public FinancialIntegrityReport Report { get; private set; } = new([], 0, DateTime.UtcNow);
    public IReadOnlyDictionary<string, string> ReviewedFindings { get; private set; } =
        new Dictionary<string, string>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Report = AccountId is { } accountId
            ? await diagnostics.ScanAccountAsync(accountId, ct)
            : await diagnostics.ScanAllAsync(ct: ct);

        var reviewed = await db.AccountModuleAuditLogs.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Action == "FinancialDivergenceReviewed" && x.EntityId != null)
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .Select(x => new { x.EntityId, x.Summary })
            .ToListAsync(ct);
        ReviewedFindings = reviewed
            .GroupBy(x => x.EntityId!)
            .ToDictionary(g => g.Key, g => g.First().Summary);
    }

    public async Task<IActionResult> OnPostReviewAsync(CancellationToken ct)
    {
        if (!ReviewDecisions.Contains(Decision))
            ModelState.AddModelError(nameof(Decision), "Selecione uma decisão de revisão válida.");
        if (string.IsNullOrWhiteSpace(Note) || Note.Trim().Length < 10)
            ModelState.AddModelError(nameof(Note), "Registre uma justificativa com pelo menos 10 caracteres.");

        if (!ModelState.IsValid)
        {
            await OnGetAsync(ct);
            return Page();
        }

        var actorId = Guid.TryParse(User.FindFirstValue("user_id"), out var parsed) ? parsed : Guid.Empty;
        db.AccountModuleAuditLogs.Add(new AccountModuleAuditLog
        {
            AccountId = FindingAccountId == Guid.Empty ? null : FindingAccountId,
            ActorUserId = actorId,
            Action = "FinancialDivergenceReviewed",
            EntityType = "FinancialIntegrityFinding",
            EntityId = FindingId,
            ModuleCode = "finance",
            Summary = $"{Decision}: {Note.Trim()}",
            CorrelationId = HttpContext.TraceIdentifier
        });
        await db.SaveChangesAsync(ct);

        TempData["Success"] = "Revisão registrada na auditoria.";
        return RedirectToPage(new { AccountId });
    }
}
