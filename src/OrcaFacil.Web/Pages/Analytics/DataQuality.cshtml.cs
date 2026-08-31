using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.DataGovernance;
using OrcaFacil.Application.Security;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Analytics;

[Authorize(Policy = "Permission:" + PermissionCodes.DataQualityView)]
public sealed class DataQualityModel(AnalyticsV21Service analytics, OrcaFacilDbContext db, ICurrentAccountService currentAccount, DataQualityScoreService scores) : PageModel
{
    public IReadOnlyList<QualityFindingView> Findings { get; private set; } = [];
    public QualityScore Score { get; private set; } = QualityScore.Empty;
    public IReadOnlyDictionary<string, int> Modules { get; private set; } = new Dictionary<string, int>();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (currentAccount.AccountId is not { } accountId) return Forbid();
        Findings = await analytics.DataQualityAsync(ct);
        var clients = await db.Clients.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive, ct);
        var documents = await db.Documents.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        var workOrders = await db.WorkOrders.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        var weighted = Findings.Select((finding, index) => new QualityFinding(accountId, Guid.Empty, "Cadastro", $"LIVE_{index}", Parse(finding.Severity), finding.Title, finding.Description));
        Score = scores.Calculate(clients + documents + workOrders, weighted);
        Modules = Findings.GroupBy(x => ModuleOf(x.ActionUrl)).ToDictionary(x => x.Key, x => x.Count());
        return Page();
    }

    private static QualitySeverity Parse(string value) => Enum.TryParse<QualitySeverity>(value, true, out var parsed) ? parsed : QualitySeverity.Medium;
    private static string ModuleOf(string url) => url.StartsWith("/Clients", StringComparison.OrdinalIgnoreCase) ? "Clientes" : url.StartsWith("/WorkOrders", StringComparison.OrdinalIgnoreCase) ? "OS" : "Comercial";
}
