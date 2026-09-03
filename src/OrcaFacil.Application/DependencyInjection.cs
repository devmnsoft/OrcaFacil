using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrcaFacil.Application.Security;
using OrcaFacil.Application.Localization;
using OrcaFacil.Application.Payments;
using OrcaFacil.Application.Field;
using OrcaFacil.Application.Quality;
using OrcaFacil.Application.Automation;
using OrcaFacil.Application.DataGovernance;
using OrcaFacil.Application.Bi;
using OrcaFacil.Application.CustomerSuccess;

namespace OrcaFacil.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        services.TryAddSingleton<ISensitiveDataSanitizer, SensitiveDataSanitizer>();
        services.TryAddSingleton<LocalePreferenceService>();
        services.TryAddSingleton<RegionalFormatService>();
        services.TryAddSingleton<TranslationImportService>();
        services.TryAddSingleton<TranslationExportService>();
        services.TryAddSingleton<HreflangService>();
        services.TryAddSingleton<IPaymentWebhookVerifier, HmacPaymentWebhookVerifier>();
        services.TryAddSingleton<PaymentReconciliationService>();
        services.TryAddScoped<FieldTeamService>();
        services.TryAddScoped<FieldDispatchService>();
        services.TryAddScoped<FieldScheduleService>();
        services.TryAddScoped<FieldRouteService>();
        services.TryAddScoped<FieldVisitSessionService>();
        services.TryAddScoped<FieldVisitEvidenceService>();
        services.TryAddScoped<FieldVisitSignatureService>();
        services.TryAddScoped<FieldMaterialUsageService>();
        services.TryAddScoped<FieldTimeEntryService>();
        services.TryAddScoped<FieldOfflineSyncService>();
        services.TryAddScoped<FieldPortalIsolationService>();
        services.TryAddScoped<FieldQualityReviewService>();
        services.TryAddScoped<FieldVisitExpenseService>();
        services.TryAddScoped<FieldReportService>();
        services.TryAddScoped<BusinessRuleAuditService>();
        services.TryAddSingleton<BusinessStatusCatalogService>();
        services.TryAddSingleton<BusinessTransitionRuleService>();
        services.TryAddSingleton<DueDatePolicyService>();
        services.TryAddSingleton<PortalIsolationGuardService>();
        services.TryAddScoped(_ => new SourceCodeFindingService(repositoryRoot));
        services.TryAddScoped(serviceProvider => new ModuleReadinessService(
            repositoryRoot, serviceProvider.GetRequiredService<BusinessRuleAuditService>()));
        services.TryAddScoped<FunctionalQualityService>();
        services.TryAddScoped<QualityGateService>();
        services.TryAddScoped<ModuleRefinementScoreService>();
        services.TryAddScoped<UserJourneyReviewService>();
        services.TryAddSingleton<FriendlyErrorMessageService>();
        services.TryAddSingleton<AutomationTriggerCatalogService>();
        services.TryAddSingleton<AutomationConditionCatalogService>();
        services.TryAddSingleton<AutomationActionCatalogService>();
        services.TryAddSingleton<AutomationConditionEvaluator>();
        services.TryAddSingleton<AutomationSafetyPolicyService>();
        services.TryAddSingleton<AutomationRuleBuilderService>();
        services.TryAddSingleton<AutomationDryRunService>();
        services.TryAddSingleton<AutomationEventQueueService>();
        services.TryAddSingleton<AutomationApprovalService>();
        services.TryAddSingleton<AutomationTemplateService>();
        services.TryAddSingleton<DataQualityRuleService>();
        services.TryAddSingleton<DataQualityEngine>();
        services.TryAddSingleton<DataQualityCheckService>();
        services.TryAddSingleton<DataQualityFindingService>();
        services.TryAddSingleton<DataQualityScoreService>();
        services.TryAddSingleton<DuplicateDetectionService>();
        services.TryAddSingleton<MasterDataMergeService>();
        services.TryAddSingleton<ClientMergeService>();
        services.TryAddSingleton<DataNormalizationService>();
        services.TryAddSingleton<DataImportPreviewService>();
        services.TryAddSingleton<DataImportService>();
        services.TryAddSingleton<DataImportCommitService>();
        services.TryAddSingleton<DataImportRollbackService>();
        services.TryAddSingleton<DataIntegrityService>();
        services.TryAddSingleton<SensitiveDataChangeReviewService>();
        services.TryAddSingleton<ModuleDataQualityService>();
        services.TryAddSingleton<DataQualityFixService>();
        services.TryAddSingleton<DataQualityAutomationIntegrationService>();
        services.TryAddSingleton<GovernedDataQualityAiService>();
        services.TryAddSingleton<BiMetricPermissionService>();
        services.TryAddSingleton<BiTrendAnalysisService>();
        services.TryAddSingleton<BiGoalService>();
        services.TryAddSingleton<OkrService>();
        services.TryAddSingleton<BiAlertService>();
        services.TryAddSingleton<BiDataMartService>();
        services.TryAddSingleton<BiForecastService>();
        services.TryAddSingleton<BiDashboardService>();
        services.TryAddSingleton<BiInsightService>();
        services.TryAddSingleton<CustomerHealthRuleService>();
        services.TryAddSingleton<CustomerHealthScoreService>();
        services.TryAddSingleton<CustomerChurnRiskService>();
        services.TryAddSingleton<CustomerRetentionPlanService>();
        services.TryAddSingleton<CustomerExpansionOpportunityService>();
        services.TryAddSingleton<CustomerRenewalService>();
        services.TryAddSingleton<CustomerNpsService>();
        services.TryAddSingleton<CustomerQbrService>();
        services.TryAddSingleton<CustomerSuccessPlaybookService>();
        services.TryAddSingleton<CustomerSuccessPlaybookRunService>();
        services.TryAddSingleton<CustomerTouchpointService>();
        services.TryAddSingleton<CustomerSuccessAlertService>();
        services.TryAddSingleton<CustomerSuccessTenantIsolationService>();
        services.TryAddSingleton<CustomerSuccessAccountService>();
        services.TryAddSingleton<CustomerSuccessPlanService>();
        services.TryAddSingleton<CustomerAdoptionAnalyticsService>();
        services.TryAddSingleton<CustomerSuccessEscalationService>();
        services.TryAddSingleton<CustomerSuccessReportService>();

        return services;
    }
}
