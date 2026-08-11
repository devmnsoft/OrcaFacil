using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class CommercialFunnelModel(IIntelligenceReportService reports) : ReportPageModel(reports)
{ protected override string FilePrefix => "funil-comercial"; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.CommercialFunnelAsync(f, ct); }
