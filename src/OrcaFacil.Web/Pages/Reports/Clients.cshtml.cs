using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class ClientsModel(IIntelligenceReportService reports) : ReportPageModel(reports)
{ protected override string FilePrefix => "clientes"; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.ClientsAsync(f, ct); }
