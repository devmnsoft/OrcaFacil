using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class ServicesModel(IIntelligenceReportService reports) : ReportPageModel(reports)
{ protected override string FilePrefix => "servicos"; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.ServicesAsync(f, ct); }
