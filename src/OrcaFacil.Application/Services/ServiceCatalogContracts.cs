using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Services;

public sealed record ServiceCatalogQuery(string? Search = null, Guid? CategoryId = null, string? Unit = null,
    decimal? MinimumPrice = null, decimal? MaximumPrice = null, decimal? MinimumMargin = null,
    bool? Favorite = null, bool? Active = true, bool? Used = null, string Sort = "favorite", int Page = 1, int PageSize = 20);
public sealed record ServiceCatalogPage(IReadOnlyList<ServiceCatalogItem> Items, int Total, int Page, int PageSize,
    int Active, int Favorites, int Used, int WithoutPrice, int NegativeMargin, int Inactive)
{ public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)); }
public sealed record ServiceCatalogInput(string Name, string? Code, string? Description, Guid? CategoryId, string UnitCode,
    decimal StandardPrice, decimal EstimatedCost, int? SuggestedDurationMinutes, string? InternalNotes, bool IsFavorite, bool IsActive);
public sealed record ServiceCatalogDetails(ServiceCatalogItem Item, IReadOnlyList<ServicePriceHistory> PriceHistory);
public enum ServiceCatalogResultCode { Success, AccountRequired, NotFound, InvalidInput, ConcurrencyConflict }
public sealed record ServiceCatalogResult(ServiceCatalogResultCode Code, Guid? Id = null, string? Message = null);

public interface IServiceCatalogApplicationService
{
    Task<ServiceCatalogPage?> ListAsync(ServiceCatalogQuery query, CancellationToken ct = default);
    Task<ServiceCatalogDetails?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ServiceCatalogResult> CreateAsync(ServiceCatalogInput input, CancellationToken ct = default);
    Task<ServiceCatalogResult> UpdateAsync(Guid id, uint version, ServiceCatalogInput input, string? priceChangeReason, CancellationToken ct = default);
    Task<ServiceCatalogResult> ToggleFavoriteAsync(Guid id, CancellationToken ct = default);
}
