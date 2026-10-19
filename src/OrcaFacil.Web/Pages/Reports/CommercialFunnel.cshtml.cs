using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Reports;
[Authorize] public sealed class CommercialFunnelModel(IIntelligenceReportService reports, ICurrentAccountService account, IPlanAccessService plans, OrcaFacilDbContext db) : ReportPageModel(reports, account, plans, db)
{ protected override string FilePrefix => "funil-comercial"; protected override string? RequiredFeature => PlanFeatureCodes.CommercialMetricsEnabled; protected override Task<IntelligenceReport> LoadAsync(ReportFilter f, CancellationToken ct) => reports.CommercialFunnelAsync(f, ct); }
