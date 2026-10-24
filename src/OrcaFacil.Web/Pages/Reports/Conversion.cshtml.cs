using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class ConversionModel(IIntelligenceReportService reports, ICurrentAccountService account, IPlanAccessService plans, OrcaFacilDbContext db) : ReportPageModel(reports, account, plans, db)
{ protected override string FilePrefix => "conversao-comercial"; protected override string? RequiredFeature => PlanFeatureCodes.CommercialMetricsEnabled; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.ConversionAsync(f, ct); }
