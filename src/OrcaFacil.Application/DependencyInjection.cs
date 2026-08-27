using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrcaFacil.Application.Security;
using OrcaFacil.Application.Localization;
using OrcaFacil.Application.Payments;
using OrcaFacil.Application.Field;

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

        return services;
    }
}
