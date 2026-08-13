using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class FinancialModel(IIntelligenceReportService reports, ICurrentAccountService account, OrcaFacilDbContext db) : ReportPageModel(reports, account, db)
{ protected override string FilePrefix => "financeiro"; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.FinancialAsync(f, ct); }
