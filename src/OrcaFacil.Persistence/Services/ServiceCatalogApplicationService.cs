using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Services;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services;

public sealed class ServiceCatalogApplicationService(OrcaFacilDbContext db, ICurrentAccountService account, ICurrentUserService user) : IServiceCatalogApplicationService
{
    public async Task<ServiceCatalogPage?> ListAsync(ServiceCatalogQuery request, CancellationToken ct = default)
    {
        if (account.AccountId is not Guid accountId) return null;
        var all = db.ServiceCatalogItems.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        var query = all;
        if (!string.IsNullOrWhiteSpace(request.Search)) { var term = $"%{request.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Name, term) || (x.Code != null && EF.Functions.ILike(x.Code, term)) || (x.Description != null && EF.Functions.ILike(x.Description, term)) || (x.Tags != null && EF.Functions.ILike(x.Tags, term))); }
        if (request.CategoryId.HasValue) query = query.Where(x => x.CategoryId == request.CategoryId);
        if (!string.IsNullOrWhiteSpace(request.Unit)) query = query.Where(x => x.UnitCode == request.Unit);
        if (request.MinimumPrice.HasValue) query = query.Where(x => x.StandardPrice >= request.MinimumPrice);
        if (request.MaximumPrice.HasValue) query = query.Where(x => x.StandardPrice <= request.MaximumPrice);
        if (request.MinimumMargin.HasValue) query = query.Where(x => x.StandardPrice - x.EstimatedCost >= request.MinimumMargin);
        if (request.Favorite.HasValue) query = query.Where(x => x.IsFavorite == request.Favorite);
        if (request.Active.HasValue) query = query.Where(x => x.IsActive == request.Active);
        if (request.Used.HasValue) query = query.Where(x => (x.UseCount > 0) == request.Used);
        if (request.Recurring.HasValue) query = query.Where(x => x.IsRecurring == request.Recurring);
        query = request.Sort switch { "name" => query.OrderBy(x => x.Name), "price" => query.OrderBy(x => x.StandardPrice), "margin" => query.OrderByDescending(x => x.StandardPrice - x.EstimatedCost), "used" => query.OrderByDescending(x => x.UseCount), _ => query.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name) };
        var page = Math.Max(1, request.Page); var size = Math.Clamp(request.PageSize, 10, 100); var total = await query.CountAsync(ct);
        return new(await query.Skip((page - 1) * size).Take(size).ToListAsync(ct), total, page, size, await all.CountAsync(x => x.IsActive, ct), await all.CountAsync(x => x.IsFavorite, ct), await all.CountAsync(x => x.UseCount > 0, ct), await all.CountAsync(x => x.StandardPrice == 0, ct), await all.CountAsync(x => x.StandardPrice < x.EstimatedCost, ct), await all.CountAsync(x => !x.IsActive, ct));
    }
    public async Task<ServiceCatalogDetails?> GetAsync(Guid id, CancellationToken ct = default) { if (account.AccountId is not Guid aid) return null; var item = await db.ServiceCatalogItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == aid && !x.IsDeleted, ct); return item is null ? null : new(item, await db.ServicePriceHistories.AsNoTracking().Where(x => x.AccountId == aid && x.ServiceCatalogItemId == id && !x.IsDeleted).OrderByDescending(x => x.ChangedAt).Take(100).ToListAsync(ct)); }
    public async Task<ServiceCatalogResult> CreateAsync(ServiceCatalogInput i, CancellationToken ct = default) { if (account.AccountId is not Guid aid) return new(ServiceCatalogResultCode.AccountRequired); if (string.IsNullOrWhiteSpace(i.Name)) return new(ServiceCatalogResultCode.InvalidInput, Message: "Informe o nome."); var item = new ServiceCatalogItem { AccountId = aid }; Apply(item, i); db.Add(item); await db.SaveChangesAsync(ct); return new(ServiceCatalogResultCode.Success, item.Id); }
    public async Task<ServiceCatalogResult> UpdateAsync(Guid id, uint version, ServiceCatalogInput i, string? reason, CancellationToken ct = default) { if (account.AccountId is not Guid aid) return new(ServiceCatalogResultCode.AccountRequired); var item = await db.ServiceCatalogItems.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == aid && !x.IsDeleted, ct); if (item is null) return new(ServiceCatalogResultCode.NotFound); if (item.Version != version) return new(ServiceCatalogResultCode.ConcurrencyConflict, Message: "Este serviço foi alterado em outra sessão."); if (item.StandardPrice != i.StandardPrice || item.EstimatedCost != i.EstimatedCost) db.Add(new ServicePriceHistory { AccountId = aid, ServiceCatalogItemId = id, PreviousPrice = item.StandardPrice, NewPrice = i.StandardPrice, PreviousCost = item.EstimatedCost, NewCost = i.EstimatedCost, Reason = reason?.Trim(), ChangedByUserId = user.UserId }); Apply(item, i); item.Touch(); await db.SaveChangesAsync(ct); return new(ServiceCatalogResultCode.Success, id); }
    public async Task<ServiceCatalogResult> ToggleFavoriteAsync(Guid id, CancellationToken ct = default) { if (account.AccountId is not Guid aid) return new(ServiceCatalogResultCode.AccountRequired); var item = await db.ServiceCatalogItems.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == aid && !x.IsDeleted, ct); if (item is null) return new(ServiceCatalogResultCode.NotFound); item.IsFavorite = !item.IsFavorite; item.Touch(); await db.SaveChangesAsync(ct); return new(ServiceCatalogResultCode.Success, id); }
    private static void Apply(ServiceCatalogItem x, ServiceCatalogInput i) { x.Name = i.Name.Trim(); x.Code = string.IsNullOrWhiteSpace(i.Code) ? null : i.Code.Trim(); x.Description = i.Description?.Trim(); x.CategoryId = i.CategoryId; x.UnitCode = i.UnitCode; x.StandardPrice = i.StandardPrice; x.EstimatedCost = i.EstimatedCost; x.DesiredMarginPercentage = i.DesiredMarginPercentage; x.SuggestedDurationMinutes = i.SuggestedDurationMinutes; x.DefaultDeliveryTerm = i.DefaultDeliveryTerm?.Trim(); x.DefaultNotes = i.DefaultNotes?.Trim(); x.Tags = i.Tags?.Trim(); x.InternalNotes = i.InternalNotes?.Trim(); x.IsFavorite = i.IsFavorite; x.IsActive = i.IsActive; x.IsRecurring = i.IsRecurring; x.IsRecommended = i.IsRecommended; x.DefaultPeriodicity=i.DefaultPeriodicity; x.SuggestedMonthlyPrice=i.SuggestedMonthlyPrice; x.EstimatedMonthlyCost=i.EstimatedMonthlyCost; x.DefaultResponseSlaHours=i.DefaultResponseSlaHours; x.DefaultExecutionSlaHours=i.DefaultExecutionSlaHours; x.DefaultChecklist=i.DefaultChecklist?.Trim(); }
}
