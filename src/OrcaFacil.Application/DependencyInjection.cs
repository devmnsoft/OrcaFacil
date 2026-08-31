using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrcaFacil.Application.Security;
using OrcaFacil.Application.Localization;
using OrcaFacil.Application.Payments;
using OrcaFacil.Application.Field;
using OrcaFacil.Application.Quality;
using OrcaFacil.Application.Automation;
using OrcaFacil.Application.DataGovernance;

namespace OrcaFacil.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
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
        services.TryAddSingleton<BusinessRuleAuditService>();
        services.TryAddSingleton<BusinessStatusCatalogService>();
        services.TryAddSingleton<BusinessTransitionRuleService>();
        services.TryAddSingleton<DueDatePolicyService>();
        services.TryAddSingleton<PortalIsolationGuardService>();
        services.TryAddSingleton<ModuleRefinementScoreService>();
        services.TryAddSingleton<UserJourneyReviewService>();
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

        return services;
    }
}
