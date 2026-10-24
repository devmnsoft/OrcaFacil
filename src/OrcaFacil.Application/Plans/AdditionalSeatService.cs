using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Plans;

/// <summary>Situação dos assentos adicionais da conta (base incluída no plano + contratados).</summary>
public sealed record AdditionalSeatStatus(int? BaseLimit, int PurchasedSeats, int? EffectiveLimit, int RemainingPurchasable, decimal SeatPriceMonthly);

public sealed record SeatPurchaseResult(bool Succeeded, string Message, int PurchasedSeats, int? EffectiveLimit);

/// <summary>
/// Assentos de equipe adicionais pagos no plano Negócio: seed idempotente do addon no catálogo,
/// compra com teto por conta e auditoria. O teto efetivo cresce via PlanAccessService (incrementos de addon).
/// </summary>
public interface IAdditionalSeatService
{
    Task<AdditionalSeatStatus> GetStatusAsync(Guid accountId, CancellationToken ct = default);
    Task<SeatPurchaseResult> PurchaseAsync(Guid accountId, Guid userId, int quantity, CancellationToken ct = default);
}

public sealed class AdditionalSeatService(IRepository<AccountAddon> accountAddons, IRepository<PlanAddon> planAddons, IUnitOfWork uow,
    IPlanAccessService plans, IAuditService audit, ILogger<AdditionalSeatService> logger) : IAdditionalSeatService
{
    public async Task<AdditionalSeatStatus> GetStatusAsync(Guid accountId, CancellationToken ct = default)
    {
        var addon = await FindOrCreateAddonAsync(ct);
        var purchased = Purchased(accountId, addon.Id);
        // O limite efetivo já inclui os assentos comprados (aplicado em PlanAccessService);
        // a base incluída no plano é efetivo menos o já contratado.
        var effective = await plans.GetLimitAsync(accountId, AdditionalSeatRules.FeatureCode, ct);
        int? baseLimit = effective is null ? null : Math.Max(0, effective.Value - purchased);
        return new(baseLimit, purchased, effective, Math.Max(0, AdditionalSeatRules.MaxAdditionalSeats - purchased), AdditionalSeatRules.SeatPriceMonthly);
    }

    public async Task<SeatPurchaseResult> PurchaseAsync(Guid accountId, Guid userId, int quantity, CancellationToken ct = default)
    {
        var addon = await FindOrCreateAddonAsync(ct);
        var plan = await plans.GetEffectivePlanAsync(accountId, DateTime.UtcNow, ct);
        if (!AdditionalSeatRules.IsEligiblePlan(plan?.Code))
            return new(false, "Assentos adicionais podem ser contratados apenas no plano Negócio.", Purchased(accountId, addon.Id), null);

        var purchased = Purchased(accountId, addon.Id);
        var error = AdditionalSeatRules.ValidatePurchase(purchased, quantity);
        if (error is not null)
            return new(false, error, purchased, null);

        var existing = accountAddons.Query().SingleOrDefault(x => x.AccountId == accountId && x.AddonId == addon.Id && !x.IsDeleted);
        if (existing is null)
        {
            existing = new AccountAddon { AccountId = accountId, AddonId = addon.Id, Quantity = quantity, ActivatedAt = DateTime.UtcNow };
            await accountAddons.AddAsync(existing, ct);
        }
        else
        {
            existing.DeactivatedAt = null;
            existing.Quantity += quantity;
        }

        var totalPurchased = purchased + quantity;
        await audit.RegisterAsync(userId, "Subscription.AdditionalSeatsPurchased", nameof(AccountAddon), existing.Id.ToString(),
            null, new { TotalQuantity = totalPurchased },
            new { AddonCode = AdditionalSeatRules.AddonCode, QuantityBought = quantity, MonthlyCost = AdditionalSeatRules.MonthlyCost(totalPurchased) },
            ct, accountId);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("ADDITIONAL_SEATS_PURCHASED {AccountId} {UserId} {Quantity} {TotalPurchased}", accountId, userId, quantity, totalPurchased);

        var effective = await plans.GetLimitAsync(accountId, AdditionalSeatRules.FeatureCode, ct);
        var total = effective is null ? "sem limite" : $"até {effective.Value} pessoa(s)";
        return new(true,
            $"{quantity} usuário(s) adicional(is) contratado(s); a equipe agora permite {total}. Custo mensal dos assentos: {AdditionalSeatRules.MonthlyCost(totalPurchased):C}.",
            totalPurchased, effective);
    }

    /// <summary>Garante a existência do addon "usuário adicional" no catálogo (seed idempotente por código).</summary>
    private async Task<PlanAddon> FindOrCreateAddonAsync(CancellationToken ct)
    {
        var addon = planAddons.Query().FirstOrDefault(x => x.Code == AdditionalSeatRules.AddonCode && !x.IsDeleted);
        if (addon is not null) return addon;

        addon = new PlanAddon
        {
            Code = AdditionalSeatRules.AddonCode,
            Name = "Usuário adicional",
            Description = "Libera mais uma pessoa na equipe da conta.",
            PriceMonthly = AdditionalSeatRules.SeatPriceMonthly,
            PriceAnnual = AdditionalSeatRules.SeatPriceMonthly * 12m,
            LimitType = AdditionalSeatRules.FeatureCode,
            LimitIncrement = 1,
            IsActive = true
        };
        await planAddons.AddAsync(addon, ct);
        await uow.SaveChangesAsync(ct);
        return addon;
    }

    private int Purchased(Guid accountId, Guid addonId) =>
        accountAddons.Query()
            .Where(x => x.AccountId == accountId && x.AddonId == addonId && !x.IsDeleted && x.DeactivatedAt == null)
            .Sum(x => x.Quantity);
}
