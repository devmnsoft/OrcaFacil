using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Saas.Billing;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services.Saas;

public sealed class AccountModuleBillingService(OrcaFacilDbContext db) : IAccountModuleBillingService
{
    public async Task<AccountModuleBillingPreview> PreviewAsync(Guid accountId, CancellationToken ct = default)
    {
        if (!await db.BusinessAccounts.AsNoTracking().AnyAsync(x => x.Id == accountId && !x.IsDeleted, ct))
            throw new KeyNotFoundException("Conta não encontrada.");
        var lines = await (from subscription in db.AccountModuleSubscriptions.AsNoTracking()
                           join module in db.SaasModules.AsNoTracking() on subscription.ModuleId equals module.Id
                           where subscription.AccountId == accountId && !subscription.IsDeleted && !module.IsDeleted &&
                                 (subscription.Status == SaasModuleSubscriptionStatus.Active || subscription.Status == SaasModuleSubscriptionStatus.Trial)
                           orderby module.DisplayOrder
                           select new AccountModuleBillingLine(module.Code, module.DisplayName, subscription.Status.ToString(),
                               subscription.BillingPeriod.ToString(), subscription.ContractedPrice, subscription.DiscountAmount,
                               subscription.ContractedPrice - subscription.DiscountAmount)).ToListAsync(ct);
        var subtotal = lines.Sum(x => x.ContractedPrice);
        var discount = lines.Sum(x => x.Discount);
        return new(accountId, lines, subtotal, discount, subtotal - discount, "BRL");
    }
}
