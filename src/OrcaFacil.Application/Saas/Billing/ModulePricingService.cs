using OrcaFacil.Application.Saas.Modules;

namespace OrcaFacil.Application.Saas.Billing;

public sealed record ModuleInvoicePreviewLine(string ModuleCode, string DisplayName, decimal UnitPrice, decimal Discount, decimal Total);
public sealed record SubscriptionInvoicePreview(IReadOnlyList<ModuleInvoicePreviewLine> Lines, decimal Subtotal, decimal Discount, decimal Total, string Currency);

public sealed class ModulePricingService(SaasModuleRegistryService registry)
{
    public decimal Resolve(string moduleCode, bool annual)
    {
        var module = registry.Find(moduleCode) ?? throw new KeyNotFoundException("Módulo não encontrado.");
        return annual ? module.AnnualPrice : module.MonthlyPrice;
    }
}

public sealed class SubscriptionInvoicePreviewService(SaasModuleRegistryService registry)
{
    public SubscriptionInvoicePreview Preview(IEnumerable<string> moduleCodes, bool annual, decimal discount = 0)
    {
        var modules = moduleCodes.Distinct(StringComparer.OrdinalIgnoreCase).Select(code => registry.Find(code) ?? throw new KeyNotFoundException($"Módulo {code} não encontrado.")).ToArray();
        var prices = modules.Select(x => annual ? x.AnnualPrice : x.MonthlyPrice).ToArray();
        var subtotal = prices.Sum();
        var appliedDiscount = Math.Clamp(discount, 0, subtotal);
        var lines = modules.Select((module, index) => new ModuleInvoicePreviewLine(module.Code, module.DisplayName, prices[index], 0, prices[index])).ToArray();
        return new(lines, subtotal, appliedDiscount, subtotal - appliedDiscount, "BRL");
    }
}
