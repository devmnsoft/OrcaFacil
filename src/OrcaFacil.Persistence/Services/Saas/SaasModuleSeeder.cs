using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrcaFacil.Application.Saas.Modules;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Persistence.Services.Saas;

public static class SaasModuleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrcaFacilDbContext>();
        var registry = scope.ServiceProvider.GetRequiredService<SaasModuleRegistryService>();
        if ((await db.Database.GetPendingMigrationsAsync(ct)).Contains(DatabaseSchemaContractService.SaasEnterpriseV66Migration)) return;
        var existingCodes = await db.SaasModules.Select(x => x.Code).ToHashSetAsync(StringComparer.OrdinalIgnoreCase, ct);
        foreach (var definition in registry.GetAll().Where(x => !existingCodes.Contains(x.Code)))
        {
            var module = new SaasModule { Code = definition.Code, DisplayName = definition.DisplayName, Description = definition.DisplayName, Category = definition.Category, IconKey = definition.IconKey, MenuGroup = definition.MenuGroup, RoutePrefix = definition.RoutePrefix, RequiredPermissionCode = definition.RequiredPermissionCode, BaseMonthlyPrice = definition.MonthlyPrice, BaseAnnualPrice = definition.AnnualPrice, IsActive = definition.IsActive, IsPublic = definition.IsPublic, DisplayOrder = definition.DisplayOrder };
            db.SaasModules.Add(module);
            db.SaasModulePrices.AddRange(
                new SaasModulePrice { ModuleId = module.Id, BillingPeriod = SaasBillingPeriod.Monthly, Amount = definition.MonthlyPrice, ValidFrom = DateTime.UtcNow },
                new SaasModulePrice { ModuleId = module.Id, BillingPeriod = SaasBillingPeriod.Annual, Amount = definition.AnnualPrice, ValidFrom = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(ct);
    }
}
