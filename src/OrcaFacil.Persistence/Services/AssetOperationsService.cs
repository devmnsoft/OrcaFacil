using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services;

/// <summary>Tenant-scoped asset, maintenance and quality operations. Every lookup repeats the account boundary.</summary>
public sealed class AssetOperationsService(OrcaFacilDbContext db)
{
    public async Task<CustomerAsset> CreateAssetAsync(Guid accountId, Guid clientId, Guid categoryId, string name,
        Guid? modelId = null, Guid? locationId = null, string? serialNumber = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("O nome do ativo é obrigatório.", nameof(name));
        if (!await db.Clients.AnyAsync(x => x.Id == clientId && x.AccountId == accountId && !x.IsDeleted, ct))
            throw new InvalidOperationException("O cliente não pertence à conta atual.");
        if (!await db.AssetCategories.AnyAsync(x => x.Id == categoryId && !x.IsDeleted && x.IsActive && (x.AccountId == null || x.AccountId == accountId), ct))
            throw new InvalidOperationException("A categoria não está disponível para esta conta.");
        if (modelId.HasValue && !await db.AssetModels.AnyAsync(x => x.Id == modelId && !x.IsDeleted && x.IsActive && (x.AccountId == null || x.AccountId == accountId), ct))
            throw new InvalidOperationException("O modelo não está disponível para esta conta.");
        if (locationId.HasValue && !await db.CustomerAssetLocations.AnyAsync(x => x.Id == locationId && x.AccountId == accountId && x.ClientId == clientId && !x.IsDeleted, ct))
            throw new InvalidOperationException("A localização não pertence ao cliente selecionado.");
        var serial = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        if (serial is not null && await db.CustomerAssets.AnyAsync(x => x.AccountId == accountId && x.ClientId == clientId && x.SerialNumber == serial && !x.IsDeleted, ct))
            throw new InvalidOperationException("Já existe um ativo com este número de série para o cliente.");
        var asset = new CustomerAsset { AccountId = accountId, ClientId = clientId, CategoryId = categoryId, ModelId = modelId, LocationId = locationId, Name = name.Trim(), SerialNumber = serial };
        db.CustomerAssets.Add(asset); await db.SaveChangesAsync(ct); return asset;
    }

    public async Task AddWarrantyAsync(Guid accountId, Guid assetId, string type, DateOnly start, DateOnly end, string? conditions, CancellationToken ct = default)
    {
        if (end < start) throw new ArgumentException("A data final da garantia não pode ser anterior à inicial.");
        _ = await Asset(accountId, assetId, ct);
        db.CustomerAssetWarranties.Add(new() { AccountId = accountId, AssetId = assetId, Type = type.Trim(), StartsOn = start, EndsOn = end, Conditions = conditions?.Trim() });
        await db.SaveChangesAsync(ct);
    }

    public async Task<MaintenancePlan> CreatePlanAsync(Guid accountId, Guid clientId, string name, MaintenanceFrequency frequency, IReadOnlyCollection<Guid> assetIds, DateTime firstDueAt, CancellationToken ct = default)
    {
        if (assetIds.Count == 0) throw new ArgumentException("O plano precisa ter pelo menos um ativo.", nameof(assetIds));
        var distinct = assetIds.Distinct().ToArray();
        var assets = await db.CustomerAssets.Where(x => distinct.Contains(x.Id) && x.AccountId == accountId && x.ClientId == clientId && !x.IsDeleted).ToListAsync(ct);
        if (assets.Count != distinct.Length) throw new InvalidOperationException("Um ou mais ativos não pertencem ao cliente e à conta selecionados.");
        if (assets.Any(x => x.Status is AssetStatus.Inactive or AssetStatus.Disposed or AssetStatus.Replaced))
            throw new InvalidOperationException("Ativos inativos, substituídos ou descartados não podem entrar em plano ativo.");
        var plan = new MaintenancePlan { AccountId = accountId, ClientId = clientId, Name = name.Trim(), Frequency = frequency };
        db.MaintenancePlans.Add(plan);
        foreach (var id in distinct) db.MaintenancePlanAssets.Add(new() { AccountId = accountId, PlanId = plan.Id, AssetId = id, NextDueAt = firstDueAt });
        await db.SaveChangesAsync(ct); return plan;
    }

    public async Task<int> GeneratePreventiveAsync(Guid accountId, Guid actorUserId, DateTime now, CancellationToken ct = default)
    {
        var due = await (from link in db.MaintenancePlanAssets
                         join plan in db.MaintenancePlans on link.PlanId equals plan.Id
                         join asset in db.CustomerAssets on link.AssetId equals asset.Id
                         where link.AccountId == accountId && plan.AccountId == accountId && asset.AccountId == accountId && plan.IsActive && !plan.IsDeleted && !link.IsDeleted && !asset.IsDeleted && link.NextDueAt <= now && asset.Status != AssetStatus.Inactive && asset.Status != AssetStatus.Disposed && asset.Status != AssetStatus.Replaced
                         select new { Link = link, Plan = plan, Asset = asset }).ToListAsync(ct);
        var generated = 0;
        foreach (var row in due)
        {
            var start = DateOnly.FromDateTime(row.Link.NextDueAt); var end = Next(start, row.Plan.Frequency, row.Plan.Interval).AddDays(-1);
            if (await db.MaintenanceGeneratedWorkOrders.AnyAsync(x => x.AccountId == accountId && x.PlanId == row.Plan.Id && x.AssetId == row.Asset.Id && x.PeriodStart == start && !x.IsDeleted, ct)) continue;
            var order = new WorkOrder { AccountId = accountId, ClientId = row.Asset.ClientId, ContractId = row.Plan.ContractId, Number = $"PM-{now:yyyyMMdd}-{row.Asset.Id.ToString("N")[..6].ToUpperInvariant()}", Title = $"Preventiva — {row.Asset.Name}", Description = $"Gerada pelo plano {row.Plan.Name}.", ScheduledStart = row.Link.NextDueAt, AssignedUserId = row.Plan.ResponsibleUserId, CreatedByUserId = actorUserId };
            db.WorkOrders.Add(order);
            db.MaintenanceGeneratedWorkOrders.Add(new() { AccountId = accountId, PlanId = row.Plan.Id, AssetId = row.Asset.Id, WorkOrderId = order.Id, PeriodStart = start, PeriodEnd = end, Status = "Generated" });
            row.Link.NextDueAt = Next(row.Link.NextDueAt, row.Plan.Frequency, row.Plan.Interval); row.Link.Touch(); generated++;
        }
        await db.SaveChangesAsync(ct); return generated;
    }

    public async Task CompleteInspectionAsync(Guid accountId, Guid inspectionId, CancellationToken ct = default)
    {
        var inspection = await db.AssetInspections.SingleOrDefaultAsync(x => x.Id == inspectionId && x.AccountId == accountId && !x.IsDeleted, ct) ?? throw new KeyNotFoundException("Inspeção não encontrada.");
        var required = await db.InspectionTemplateItems.Where(x => x.TemplateId == inspection.TemplateId && x.IsRequired && !x.IsDeleted).ToListAsync(ct);
        var answers = await db.AssetInspectionAnswers.Where(x => x.AccountId == accountId && x.InspectionId == inspectionId && !x.IsDeleted).ToListAsync(ct);
        if (required.Any(item => !answers.Any(a => a.TemplateItemId == item.Id && !string.IsNullOrWhiteSpace(a.Value)))) throw new InvalidOperationException("Responda todos os itens obrigatórios antes de concluir.");
        foreach (var answer in answers.Where(a => a.IsNonConforming))
        {
            var item = required.FirstOrDefault(x => x.Id == answer.TemplateItemId) ?? await db.InspectionTemplateItems.SingleAsync(x => x.Id == answer.TemplateItemId, ct);
            if (item.IsCritical && !await db.NonConformities.AnyAsync(x => x.AccountId == accountId && x.InspectionId == inspectionId && x.Description == item.Prompt && !x.IsDeleted, ct))
                db.NonConformities.Add(new() { AccountId = accountId, ClientId = inspection.ClientId, AssetId = inspection.AssetId, InspectionId = inspection.Id, Description = item.Prompt, Severity = NonConformitySeverity.Critical });
        }
        inspection.Status = InspectionStatus.Completed; inspection.Touch(); await db.SaveChangesAsync(ct);
    }

    public async Task PublishReportAsync(Guid accountId, Guid reportId, CancellationToken ct = default)
    {
        var report = await db.TechnicalReports.SingleOrDefaultAsync(x => x.Id == reportId && x.AccountId == accountId && !x.IsDeleted, ct) ?? throw new KeyNotFoundException("Laudo não encontrado.");
        if (string.IsNullOrWhiteSpace(report.Conclusion)) throw new InvalidOperationException("Um laudo vazio não pode ser publicado.");
        if (report.Status == TechnicalReportStatus.Published) throw new InvalidOperationException("Crie uma nova versão para alterar um laudo publicado.");
        report.Status = TechnicalReportStatus.Published; report.PublishedAt = DateTime.UtcNow; report.Touch(); await db.SaveChangesAsync(ct);
    }

    public async Task<(string Token, DateTime ExpiresAt)> CreateQrTokenAsync(Guid accountId, Guid assetId, TimeSpan lifetime, CancellationToken ct = default)
    {
        _ = await Asset(accountId, assetId, ct); if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(); var expires = DateTime.UtcNow.Add(lifetime);
        db.AssetQrCodes.Add(new() { AccountId = accountId, AssetId = assetId, TokenHash = Hash(token), ExpiresAt = expires }); await db.SaveChangesAsync(ct); return (token, expires);
    }

    public async Task<CustomerAsset?> ResolvePublicQrAsync(string token, string? ip, CancellationToken ct = default)
    {
        var hash = Hash(token); var qr = await db.AssetQrCodes.SingleOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null && (!x.ExpiresAt.HasValue || x.ExpiresAt > DateTime.UtcNow) && !x.IsDeleted, ct);
        if (qr is null) return null;
        db.AssetQrAccessLogs.Add(new() { AccountId = qr.AccountId, QrCodeId = qr.Id, IpHash = string.IsNullOrWhiteSpace(ip) ? null : Hash(ip) }); await db.SaveChangesAsync(ct);
        return await db.CustomerAssets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == qr.AssetId && x.AccountId == qr.AccountId && !x.IsDeleted, ct);
    }

    public async Task<(int? Score, IReadOnlyList<string> Factors)> HealthAsync(Guid accountId, Guid assetId, DateTime now, CancellationToken ct = default)
    {
        var asset = await Asset(accountId, assetId, ct); var factors = new List<string>(); var score = 100; var hasData = asset.AcquiredOn.HasValue;
        if (asset.Criticality == AssetCriticality.Critical) { score -= 20; factors.Add("Criticidade operacional crítica"); hasData = true; }
        var open = await db.NonConformities.CountAsync(x => x.AccountId == accountId && x.AssetId == assetId && x.Status != NonConformityStatus.Validated && x.Status != NonConformityStatus.Cancelled && !x.IsDeleted, ct);
        if (open > 0) { score -= Math.Min(30, open * 10); factors.Add($"{open} não conformidade(s) aberta(s)"); hasData = true; }
        var overdue = await db.MaintenancePlanAssets.CountAsync(x => x.AccountId == accountId && x.AssetId == assetId && x.NextDueAt < now && !x.IsDeleted, ct);
        if (overdue > 0) { score -= Math.Min(30, overdue * 15); factors.Add($"{overdue} preventiva(s) vencida(s)"); hasData = true; }
        return hasData ? (Math.Max(0, score), factors) : (null, ["Dados insuficientes para calcular a saúde do ativo"]);
    }

    private async Task<CustomerAsset> Asset(Guid accountId, Guid assetId, CancellationToken ct) => await db.CustomerAssets.SingleOrDefaultAsync(x => x.Id == assetId && x.AccountId == accountId && !x.IsDeleted, ct) ?? throw new KeyNotFoundException("Ativo não encontrado nesta conta.");
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static DateTime Next(DateTime value, MaintenanceFrequency f, int interval) => f switch { MaintenanceFrequency.Daily => value.AddDays(interval), MaintenanceFrequency.Weekly => value.AddDays(7 * interval), MaintenanceFrequency.Fortnightly => value.AddDays(14 * interval), MaintenanceFrequency.Monthly => value.AddMonths(interval), MaintenanceFrequency.Bimonthly => value.AddMonths(2 * interval), MaintenanceFrequency.Quarterly => value.AddMonths(3 * interval), MaintenanceFrequency.Semiannual => value.AddMonths(6 * interval), MaintenanceFrequency.Annual => value.AddYears(interval), _ => value.AddMonths(interval) };
    private static DateOnly Next(DateOnly value, MaintenanceFrequency f, int interval) => DateOnly.FromDateTime(Next(value.ToDateTime(TimeOnly.MinValue), f, interval));
}
