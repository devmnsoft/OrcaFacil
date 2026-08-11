using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Plans;

public sealed class EfPlanCatalogService(OrcaFacilDbContext db) : IPlanCatalogService
{
    public async Task<PlanCatalogView> GetPublishedAsync(CancellationToken ct = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var rows = await (from plan in db.Plans.AsNoTracking()
                join version in db.PlanVersions.AsNoTracking() on plan.Id equals version.PlanId
                where plan.IsActive && plan.IsPublic && !plan.IsDeleted && !version.IsDeleted &&
                      version.Status == PlanVersionStatus.Published && version.ValidFrom <= now &&
                      (version.ValidUntil == null || version.ValidUntil > now)
                orderby plan.DisplayOrder, version.VersionNumber descending
                select new { Plan = plan, Version = version }).ToListAsync(ct);

            var latest = rows.GroupBy(x => x.Plan.Id).Select(x => x.First()).ToArray();
            if (latest.Length == 0) return Fallback(now);
            var versionIds = latest.Select(x => x.Version.Id).ToArray();
            var values = await (from value in db.PlanFeatureValues.AsNoTracking()
                join feature in db.Features.AsNoTracking() on value.FeatureId equals feature.Id
                where versionIds.Contains(value.PlanVersionId) && !value.IsDeleted && feature.IsActive && !feature.IsDeleted
                select new { value.PlanVersionId, Feature = feature, value.BooleanValue, value.IntegerValue, value.IsUnlimited })
                .ToListAsync(ct);
            var cards = latest.Select(x => Build(x.Plan.Code, x.Plan.DisplayName, x.Plan.ShortDescription,
                x.Version.MonthlyPrice, x.Version.AnnualPrice, x.Version.Currency, x.Plan.IsRecommended,
                x.Plan.DisplayOrder, values.Where(v => v.PlanVersionId == x.Version.Id).Select(v =>
                    (v.Feature.Code, v.Feature.DisplayName, v.Feature.Description, v.Feature.Category,
                     v.BooleanValue, v.IntegerValue, v.IsUnlimited)))).ToArray();
            return new(cards, now, false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Public pricing remains honest and available during database startup/outage.
            return Fallback(DateTime.UtcNow);
        }
    }

    private static PlanCardView Build(string code, string name, string description, decimal monthly, decimal annual,
        string currency, bool recommended, int order,
        IEnumerable<(string Code,string Name,string Description,string Category,bool? Enabled,int? Limit,bool Unlimited)> values)
    {
        var included = values.Where(x => x.Enabled != false).ToArray();
        return new(code, name, description, monthly, annual, currency, recommended, order,
            included.Where(x => x.Limit is null && !x.Unlimited).Select(x => new PlanFeatureView(x.Code,x.Name,x.Description,x.Category)).ToArray(),
            included.Where(x => x.Limit.HasValue || x.Unlimited).Select(x => new PlanLimitView(x.Code,x.Name,x.Limit,x.Unlimited)).ToArray());
    }

    private static PlanCatalogView Fallback(DateTime now)
    {
        var cards = PlanCatalogDefinitions.Plans.Select((entry, index) =>
        {
            var settings = PlanCatalogDefinitions.Features[entry.Key];
            var limits = settings.Where(x => x.Value.Enabled != false && (x.Value.Limit.HasValue || x.Value.IsUnlimited))
                .Select(x => new PlanLimitView(x.Key, Humanize(x.Key), x.Value.Limit, x.Value.IsUnlimited)).ToArray();
            var features = settings.Where(x => x.Value.Enabled == true && x.Value.Limit is null && !x.Value.IsUnlimited)
                .Select(x => new PlanFeatureView(x.Key, Humanize(x.Key), string.Empty, "Recursos")).ToArray();
            var description = entry.Key switch { "FREE" => "Para organizar os primeiros atendimentos.", "PROFESSIONAL" => "Para uma operação recorrente e profissional.", _ => "Para equipes e operações com maior volume." };
            return new PlanCardView(entry.Key, entry.Value.Name, description, entry.Value.Monthly, entry.Value.Annual,
                "BRL", entry.Key == "PROFESSIONAL", index, features, limits);
        }).ToArray();
        return new(cards, now, true);
    }

    private static string Humanize(string code) => code switch
    {
        "team.members_limit" => "Usuários", "clients.active_limit" => "Clientes ativos",
        "services.active_limit" => "Serviços ativos", "pdf.monthly_limit" => "PDFs por mês",
        "templates.basic_limit" => "Templates", "history.days_visible" => "Dias de histórico",
        _ => code.Replace('.', ' ').Replace('_', ' ')
    };
}
