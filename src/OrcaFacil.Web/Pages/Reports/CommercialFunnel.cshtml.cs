using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class CommercialFunnelModel(IIntelligenceReportService reports, ICurrentAccountService account, OrcaFacilDbContext db) : ReportPageModel(reports, account, db)
{ protected override string FilePrefix => "funil-comercial"; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.CommercialFunnelAsync(f, ct); }
