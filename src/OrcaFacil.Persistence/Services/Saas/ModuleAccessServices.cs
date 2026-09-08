using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Saas.Modules;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services.Saas;

public sealed class ModuleEntitlementService(OrcaFacilDbContext db) : IModuleEntitlementService
{
    public async Task<ModuleAccessDecision> CheckAsync(Guid? accountId, string moduleCode, bool isSuperAdmin, CancellationToken ct = default)
    {
        if (isSuperAdmin) return new(true, moduleCode);
        if (accountId is null) return ModuleAccessDecision.Denied(moduleCode, "Selecione uma conta ativa.");
        if (!await db.BusinessAccounts.AsNoTracking().AnyAsync(x => x.Id == accountId && !x.IsDeleted && x.Status == AccountStatus.Active, ct))
            return ModuleAccessDecision.Denied(moduleCode, "A conta está bloqueada ou inativa.");

        var module = await db.SaasModules.AsNoTracking().SingleOrDefaultAsync(x => x.Code == moduleCode && x.IsActive && !x.IsDeleted, ct);
        if (module is null) return ModuleAccessDecision.Denied(moduleCode, "O módulo não está disponível.");
        if (moduleCode is "CORE" or "ACCOUNT_ADMIN") return new(true, moduleCode);

        var now = DateTime.UtcNow;
        var subscribed = await db.AccountModuleSubscriptions.AsNoTracking().AnyAsync(x => x.AccountId == accountId && x.ModuleId == module.Id && !x.IsDeleted &&
            (x.Status == SaasModuleSubscriptionStatus.Active || x.Status == SaasModuleSubscriptionStatus.Trial) &&
            (x.TrialEndsAt == null || x.TrialEndsAt > now) && (x.ManualGrantEndsAt == null || x.ManualGrantEndsAt > now), ct);
        var entitled = await db.AccountModuleEntitlements.AsNoTracking().AnyAsync(x => x.AccountId == accountId && x.ModuleId == module.Id && x.FeatureCode == null &&
            x.IsEnabled && !x.IsDeleted && (x.ValidUntil == null || x.ValidUntil > now), ct);
        return subscribed && entitled ? new(true, moduleCode) : ModuleAccessDecision.Denied(moduleCode, "Este módulo não está contratado ou está suspenso.");
    }
}

public sealed class FeatureAccessService(OrcaFacilDbContext db) : IFeatureAccessService
{
    public async Task<bool> HasAccessAsync(Guid accountId, string moduleCode, string featureCode, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entitled = await (from module in db.SaasModules.AsNoTracking()
            join entitlement in db.AccountModuleEntitlements.AsNoTracking() on module.Id equals entitlement.ModuleId
            where module.Code == moduleCode && module.IsActive && !module.IsDeleted && entitlement.AccountId == accountId &&
                  entitlement.IsEnabled && !entitlement.IsDeleted &&
                  (entitlement.FeatureCode == null || entitlement.FeatureCode == featureCode) &&
                  (entitlement.ValidUntil == null || entitlement.ValidUntil > now)
            select entitlement.Id).AnyAsync(ct);
        if (!entitled) return false;
        var moduleId = await db.SaasModules.AsNoTracking().Where(x => x.Code == moduleCode && x.IsActive && !x.IsDeleted).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (moduleId is null) return false;
        var limit = await db.AccountModuleFeatureLimits.AsNoTracking().SingleOrDefaultAsync(x =>
            x.AccountId == accountId && x.ModuleId == moduleId && x.FeatureCode == featureCode && !x.IsDeleted, ct);
        if (limit is null || limit.LimitValue < 0) return true;
        var periodStart = limit.Period.Equals("Daily", StringComparison.OrdinalIgnoreCase) ? now.Date : new DateTime(now.Year, now.Month, 1);
        var consumed = await db.AccountModuleUsageEvents.AsNoTracking().LongCountAsync(x =>
            x.AccountId == accountId && x.ModuleCode == moduleCode && x.FeatureCode == featureCode &&
            x.OccurredAt >= periodStart && !x.IsDeleted, ct);
        return consumed < limit.LimitValue;
    }
}

public sealed class ModuleAccessService(IModuleEntitlementService entitlements, IFeatureAccessService features) : IModuleAccessService
{
    public async Task<ModuleAccessDecision> CheckAsync(Guid? accountId, string moduleCode, string? featureCode, bool hasPermission, bool isSuperAdmin, CancellationToken ct = default)
    {
        if (isSuperAdmin) return new(true, moduleCode);
        if (!hasPermission) return ModuleAccessDecision.Denied(moduleCode, "Seu perfil não possui a permissão necessária.");
        var module = await entitlements.CheckAsync(accountId, moduleCode, false, ct);
        if (!module.Allowed || accountId is null || string.IsNullOrWhiteSpace(featureCode)) return module;
        return await features.HasAccessAsync(accountId.Value, moduleCode, featureCode, ct)
            ? module : ModuleAccessDecision.Denied(moduleCode, "Esta funcionalidade não está liberada no plano.");
    }
}

public sealed class AccountModuleSubscriptionService(OrcaFacilDbContext db) : IAccountModuleSubscriptionService
{
    public async Task RequestAsync(Guid accountId, string moduleCode, string billingPeriod, Guid actorUserId, CancellationToken ct = default)
    {
        var module = await db.SaasModules.SingleOrDefaultAsync(x => x.Code == moduleCode && x.IsActive && !x.IsDeleted, ct) ?? throw new KeyNotFoundException("Módulo não encontrado.");
        if (await db.AccountModuleSubscriptions.AnyAsync(x => x.AccountId == accountId && x.ModuleId == module.Id && !x.IsDeleted, ct)) throw new InvalidOperationException("Já existe uma contratação para este módulo.");
        var annual = billingPeriod.Equals("Annual", StringComparison.OrdinalIgnoreCase);
        db.AccountModuleSubscriptions.Add(new AccountModuleSubscription { AccountId = accountId, ModuleId = module.Id, BillingPeriod = annual ? SaasBillingPeriod.Annual : SaasBillingPeriod.Monthly, ContractedPrice = annual ? module.BaseAnnualPrice : module.BaseMonthlyPrice });
        AddAudit(accountId, actorUserId, "ModuleSubscriptionRequested", module, "Solicitação de contratação registrada.");
        await db.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(Guid accountId, Guid moduleId, decimal contractedPrice, Guid actorUserId, CancellationToken ct = default)
    {
        var subscription = await db.AccountModuleSubscriptions.SingleOrDefaultAsync(x => x.AccountId == accountId && x.ModuleId == moduleId && !x.IsDeleted, ct) ?? throw new KeyNotFoundException("Contratação não encontrada.");
        var module = await db.SaasModules.SingleAsync(x => x.Id == moduleId, ct);
        subscription.ContractedPrice = contractedPrice >= 0 ? contractedPrice : throw new ArgumentOutOfRangeException(nameof(contractedPrice));
        subscription.Activate(actorUserId);
        var entitlement = await db.AccountModuleEntitlements.SingleOrDefaultAsync(x => x.AccountId == accountId && x.ModuleId == moduleId && x.FeatureCode == null && !x.IsDeleted, ct);
        if (entitlement is null) db.AccountModuleEntitlements.Add(new() { AccountId = accountId, ModuleId = moduleId, GrantedByUserId = actorUserId, Source = SaasModuleGrantSource.Subscription });
        else { entitlement.IsEnabled = true; entitlement.ValidUntil = null; entitlement.Touch(); }
        AddAudit(accountId, actorUserId, "ModuleSubscriptionActivated", module, $"Módulo ativado por {contractedPrice:C2}.");
        await db.SaveChangesAsync(ct);
    }

    public async Task SuspendAsync(Guid accountId, Guid moduleId, Guid actorUserId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Informe o motivo da suspensão.");
        var subscription = await db.AccountModuleSubscriptions.SingleOrDefaultAsync(x => x.AccountId == accountId && x.ModuleId == moduleId && !x.IsDeleted, ct) ?? throw new KeyNotFoundException("Contratação não encontrada.");
        var module = await db.SaasModules.SingleAsync(x => x.Id == moduleId, ct);
        subscription.Suspend(actorUserId);
        foreach (var entitlement in await db.AccountModuleEntitlements.Where(x => x.AccountId == accountId && x.ModuleId == moduleId && !x.IsDeleted).ToListAsync(ct)) { entitlement.IsEnabled = false; entitlement.Touch(); }
        AddAudit(accountId, actorUserId, "ModuleSubscriptionSuspended", module, reason.Trim());
        await db.SaveChangesAsync(ct);
    }

    public async Task StartTrialAsync(Guid accountId, Guid moduleId, DateTime endsAt, Guid actorUserId, CancellationToken ct = default)
    {
        var subscription = await Subscription(accountId, moduleId, ct);
        var module = await db.SaasModules.SingleAsync(x => x.Id == moduleId && x.IsActive && !x.IsDeleted, ct);
        subscription.StartTrial(actorUserId, DateTime.SpecifyKind(endsAt, DateTimeKind.Utc));
        await UpsertEntitlement(accountId, moduleId, actorUserId, SaasModuleGrantSource.Trial, subscription.TrialEndsAt, "Trial aprovado", ct);
        AddAudit(accountId, actorUserId, "ModuleTrialStarted", module, $"Trial liberado até {subscription.TrialEndsAt:O}.");
        await db.SaveChangesAsync(ct);
    }

    public async Task GrantManualAsync(Guid accountId, Guid moduleId, DateTime endsAt, string reason, Guid actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) throw new ArgumentException("Informe o motivo da liberação manual.");
        var utcEnd = DateTime.SpecifyKind(endsAt, DateTimeKind.Utc);
        if (utcEnd <= DateTime.UtcNow) throw new ArgumentException("A liberação deve terminar no futuro.");
        var subscription = await Subscription(accountId, moduleId, ct);
        var module = await db.SaasModules.SingleAsync(x => x.Id == moduleId && x.IsActive && !x.IsDeleted, ct);
        subscription.Activate(actorUserId); subscription.ManualGrantEndsAt = utcEnd; subscription.ManualGrantReason = reason.Trim();
        await UpsertEntitlement(accountId, moduleId, actorUserId, SaasModuleGrantSource.Manual, utcEnd, reason.Trim(), ct);
        AddAudit(accountId, actorUserId, "ModuleManualGrantCreated", module, $"Liberação manual até {utcEnd:O}: {reason.Trim()}");
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdatePriceAsync(Guid accountId, Guid moduleId, decimal contractedPrice, decimal discountAmount, Guid actorUserId, CancellationToken ct = default)
    {
        if (contractedPrice < 0 || discountAmount < 0 || discountAmount > contractedPrice) throw new ArgumentOutOfRangeException(nameof(contractedPrice), "Preço e desconto precisam formar um total não negativo.");
        var subscription = await Subscription(accountId, moduleId, ct);
        var module = await db.SaasModules.SingleAsync(x => x.Id == moduleId && !x.IsDeleted, ct);
        var previous = subscription.ContractedPrice - subscription.DiscountAmount;
        subscription.ContractedPrice = contractedPrice; subscription.DiscountAmount = discountAmount; subscription.Touch();
        AddAudit(accountId, actorUserId, "ModuleContractPriceChanged", module, $"Valor efetivo alterado de {previous:C2} para {contractedPrice - discountAmount:C2}.");
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid accountId, Guid moduleId, Guid actorUserId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) throw new ArgumentException("Informe o motivo do cancelamento.");
        var subscription = await Subscription(accountId, moduleId, ct);
        var module = await db.SaasModules.SingleAsync(x => x.Id == moduleId && !x.IsDeleted, ct);
        subscription.Cancel(actorUserId);
        foreach (var entitlement in await db.AccountModuleEntitlements.Where(x => x.AccountId == accountId && x.ModuleId == moduleId && !x.IsDeleted).ToListAsync(ct)) { entitlement.IsEnabled = false; entitlement.Touch(); }
        AddAudit(accountId, actorUserId, "ModuleSubscriptionCancelled", module, reason.Trim());
        await db.SaveChangesAsync(ct);
    }

    private async Task<AccountModuleSubscription> Subscription(Guid accountId, Guid moduleId, CancellationToken ct) =>
        await db.AccountModuleSubscriptions.SingleOrDefaultAsync(x => x.AccountId == accountId && x.ModuleId == moduleId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Contratação não encontrada.");

    private async Task UpsertEntitlement(Guid accountId, Guid moduleId, Guid actor, SaasModuleGrantSource source, DateTime? validUntil, string reason, CancellationToken ct)
    {
        var entitlement = await db.AccountModuleEntitlements.SingleOrDefaultAsync(x => x.AccountId == accountId && x.ModuleId == moduleId && x.FeatureCode == null && !x.IsDeleted, ct);
        if (entitlement is null) db.AccountModuleEntitlements.Add(new() { AccountId = accountId, ModuleId = moduleId, GrantedByUserId = actor, Source = source, ValidUntil = validUntil, Reason = reason });
        else { entitlement.IsEnabled = true; entitlement.Source = source; entitlement.ValidUntil = validUntil; entitlement.Reason = reason; entitlement.GrantedByUserId = actor; entitlement.Touch(); }
    }

    private void AddAudit(Guid accountId, Guid actor, string action, SaasModule module, string summary) => db.AccountModuleAuditLogs.Add(new() { AccountId = accountId, ActorUserId = actor, Action = action, EntityType = nameof(AccountModuleSubscription), ModuleCode = module.Code, EntityId = module.Id.ToString(), Summary = summary, CorrelationId = Guid.NewGuid().ToString("N") });
}

public sealed class ModuleUsageTracker(OrcaFacilDbContext db, ILogger<ModuleUsageTracker> logger) : IModuleUsageTracker
{
    public async Task TrackAsync(Guid accountId, Guid? userId, string moduleCode, string eventCode, string? featureCode, string? route, string correlationId, CancellationToken ct = default)
    {
        try
        {
            db.AccountModuleUsageEvents.Add(new() { AccountId = accountId, UserId = userId, ModuleCode = moduleCode, FeatureCode = featureCode, EventCode = eventCode, Route = route, CorrelationId = correlationId, OccurredAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) { logger.LogWarning("MODULE_USAGE_TRACKING_FAILED ModuleCode {ModuleCode} FailureType {FailureType}", moduleCode, ex.GetType().Name); }
    }
}
