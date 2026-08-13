using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class ServicesModel(IIntelligenceReportService reports, ICurrentAccountService account, OrcaFacilDbContext db) : ReportPageModel(reports, account, db)
{ protected override string FilePrefix => "servicos"; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.ServicesAsync(f, ct); }
