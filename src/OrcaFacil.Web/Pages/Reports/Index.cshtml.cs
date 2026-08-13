using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Reports;

[Authorize]
public sealed class IndexModel(
    IIntelligenceReportService reports,
    IOperationalAlertService alerts,
    ICurrentAccountService account,
    OrcaFacilDbContext db) : PageModel
{
    public IntelligenceReport Funnel { get; private set; } = new("Funil comercial", [], []);
    public IntelligenceReport Financial { get; private set; } = new("Financeiro", [], []);
    public IntelligenceReport Clients { get; private set; } = new("Clientes", [], []);
    public IntelligenceReport Services { get; private set; } = new("Serviços", [], []);
    public int OpenAlerts { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var filter = new ReportFilter(today.AddDays(1 - today.Day), today);

        // Execute sequentially because EF Core DbContext does not support concurrent operations.
        Funnel = await reports.CommercialFunnelAsync(filter, ct);
        Financial = await reports.FinancialAsync(filter, ct);
        Clients = await reports.ClientsAsync(filter, ct);
        Services = await reports.ServicesAsync(filter, ct);

        await alerts.GenerateAsync(ct);
        var accountId = account.AccountId ?? throw new UnauthorizedAccessException("Selecione uma conta para consultar relatórios.");
        OpenAlerts = await db.Notifications.AsNoTracking().CountAsync(x =>
            x.AccountId == accountId && !x.IsDeleted && !x.IsRead && x.Message.Contains("[alert:"), ct);
    }

    public Metric? FindMetric(IntelligenceReport report, string label) =>
        report.Metrics.FirstOrDefault(x => string.Equals(x.Label, label, StringComparison.OrdinalIgnoreCase));
}
