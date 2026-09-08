using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence.Diagnostics;
using OrcaFacil.Application.Saas.Modules;
using OrcaFacil.Persistence.Services.Saas;
using OrcaFacil.Application.Saas.Billing;
using OrcaFacil.Application.Saas.Usage;

namespace OrcaFacil.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence services shared by every ASP.NET composition root.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.TryAddScoped<IDatabaseSchemaContractService, DatabaseSchemaContractService>();
        return services;
    }

    public static IServiceCollection AddSaasPersistence(this IServiceCollection services)
    {
        services.TryAddScoped<IModuleEntitlementService, ModuleEntitlementService>();
        services.TryAddScoped<IFeatureAccessService, FeatureAccessService>();
        services.TryAddScoped<IModuleAccessService, ModuleAccessService>();
        services.TryAddScoped<IAccountModuleSubscriptionService, AccountModuleSubscriptionService>();
        services.TryAddScoped<IModuleUsageTracker, ModuleUsageTracker>();
        services.TryAddScoped<IAccountModuleBillingService, AccountModuleBillingService>();
        services.TryAddScoped<SaasBillingService>();
        services.TryAddScoped<IModuleUsageEventService, ModuleUsageEventService>();
        services.TryAddScoped<IModuleUsageSnapshotService, ModuleUsageSnapshotService>();
        return services;
    }
}
