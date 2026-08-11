using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class FinancialModel(IIntelligenceReportService reports) : ReportPageModel(reports)
{ protected override string FilePrefix => "financeiro"; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.FinancialAsync(f, ct); }
