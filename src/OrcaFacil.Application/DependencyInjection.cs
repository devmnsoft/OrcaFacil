using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrcaFacil.Application.Security;
using OrcaFacil.Application.Localization;
using OrcaFacil.Application.Payments;

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

        return services;
    }
}
