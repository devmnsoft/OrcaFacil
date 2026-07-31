using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public interface IPlanExperienceService
{
    Task<PlanExperienceViewModel> GetAsync(CancellationToken cancellationToken = default);
}
public sealed class PlanExperienceService(ICurrentAccountService currentAccount,IPlanAccessService access,OrcaFacilDbContext db) : IPlanExperienceService
{
    public async Task<PlanExperienceViewModel> GetAsync(CancellationToken ct = default)
    {
        if (currentAccount.AccountId is not { } accountId) throw new UnauthorizedAccessException("Selecione uma conta.");
        var now=DateTime.UtcNow; var subscription=await access.GetCurrentSubscriptionAsync(accountId,ct);
        var selected=await access.GetSelectedPlanAsync(accountId,ct); var effective=await access.GetEffectivePlanAsync(accountId,now,ct);
        var settings=await access.GetPlanFeaturesAsync(accountId,now,ct);
        var names=await db.Features.AsNoTracking().Where(x=>settings.Keys.Contains(x.Code)).ToDictionaryAsync(x=>x.Code,x=>x.DisplayName,ct);
        var benefits=settings.Where(x=>x.Value.Enabled==true).Select(x=>names.GetValueOrDefault(x.Key,x.Key)).Order().ToArray();
        var usage=new List<PlanExperienceUsageItem>();
        foreach(var feature in settings.Where(x=>x.Value.Limit.HasValue || x.Value.IsUnlimited).Take(5))
        { var used=await access.GetUsageAsync(accountId,feature.Key,ct); usage.Add(new(names.GetValueOrDefault(feature.Key,"Uso do recurso"),used,feature.Value.IsUnlimited?null:feature.Value.Limit)); }
        string[] paused=selected?.Id!=effective?.Id && selected is not null ? new[]{"Identidade visual personalizada","Modelos e recursos avançados","Novos usos acima dos limites disponíveis"} : [];
        var plans=await (from p in db.Plans.AsNoTracking() join v in db.PlanVersions.AsNoTracking() on p.Id equals v.PlanId where p.IsActive && p.IsPublic && v.Status==PlanVersionStatus.Published orderby p.DisplayOrder select new PlanComparisonItem(p.Code,p.DisplayName,p.ShortDescription,v.MonthlyPrice,v.AnnualPrice,p.IsRecommended)).ToListAsync(ct);
        return new(selected?.Code??"FREE",selected?.DisplayName??"Grátis",effective?.Code??"FREE",effective?.DisplayName??"Grátis",HumanStatus(subscription?.Status,effective?.DisplayName),effective?.IsFree==false,selected?.Id!=effective?.Id,subscription?.NextDueAt,subscription?.PastDueSince?.AddDays(7),benefits,paused,usage,["Clientes e serviços","Documentos, PDFs e histórico","Modelos, marca e configurações","Membros, vínculos e auditoria"],plans.FirstOrDefault(x=>x.IsRecommended),selected?.Id!=effective?.Id?"Regularize para restaurar automaticamente os benefícios salvos.":"Conheça benefícios que simplificam sua rotina.","Falar com a MNSOFT",plans);
    }
    private static string HumanStatus(SubscriptionStatus? status,string? plan)=>status switch{SubscriptionStatus.Active=>$"Seu plano {plan} está ativo.",SubscriptionStatus.PastDue=>"Seu pagamento está pendente, mas os benefícios continuam ativos durante o período de regularização.",SubscriptionStatus.Suspended=>"Os benefícios pagos estão pausados. Sua conta está usando o plano Grátis.",SubscriptionStatus.Cancelled=>"O plano pago foi encerrado. Seus dados continuam preservados.",SubscriptionStatus.ManualRelease=>"Benefícios liberados temporariamente pela MNSOFT.",_=>"Você está usando o plano Grátis."};
}
public sealed record PlanExperienceViewModel(string SelectedPlanCode,string SelectedPlanName,string EffectivePlanCode,string EffectivePlanName,string Status,bool IsPaidAccessActive,bool IsUsingFreeFallback,DateTime? DueAt,DateTime? GraceEndsAt,IReadOnlyList<string> ActiveBenefits,IReadOnlyList<string> PausedBenefits,IReadOnlyList<PlanExperienceUsageItem> UsageItems,IReadOnlyList<string> PreservedDataItems,PlanComparisonItem? RecommendedPlan,string ContextualRecommendation,string RegularizationAction,IReadOnlyList<PlanComparisonItem> Plans);
public sealed record PlanExperienceUsageItem(string Label,int Used,int? Limit);
public sealed record PlanComparisonItem(string Code,string Name,string Description,decimal MonthlyPrice,decimal AnnualPrice,bool IsRecommended);
