using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Security;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Reports;

[Authorize(Policy = "Permission:" + PermissionCodes.ExecutiveReportsView)]
public sealed class ExecutiveModel(IIntelligenceReportService reports) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    public IReadOnlyList<IntelligenceReport> Sections { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct)
    {
        var filter = new ReportFilter(From, To);
        Sections = [await reports.CommercialFunnelAsync(filter, ct), await reports.FinancialAsync(filter, ct), await reports.ClientsAsync(filter, ct), await reports.ServicesAsync(filter, ct)];
    }
}
