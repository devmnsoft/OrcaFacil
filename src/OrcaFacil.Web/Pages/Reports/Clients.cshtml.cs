using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class ClientsModel(IIntelligenceReportService reports, ICurrentAccountService account, OrcaFacilDbContext db) : ReportPageModel(reports, account, db)
{ protected override string FilePrefix => "clientes"; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.ClientsAsync(f, ct); }
