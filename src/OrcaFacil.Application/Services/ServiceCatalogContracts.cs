using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Services;

public sealed record ServiceUnitOption(string Code, string Label, string ShortLabel, string IconName, bool AllowsFraction, int SortOrder);
public sealed record ServiceCategoryOption(Guid Id, string Name);
public interface IServiceUnitCatalog { IReadOnlyList<ServiceUnitOption> GetAll(); bool Contains(string code); }
public sealed class ServiceUnitCatalog : IServiceUnitCatalog
{
    private static readonly IReadOnlyList<ServiceUnitOption> Options =
    [
        new("service","Serviço","Serv.","briefcase",true,10), new("hour","Hora","h","clock",true,20), new("day","Dia","dia","calendar",true,30),
        new("unit","Unidade","un.","box",true,40), new("meter","Metro","m","ruler",true,50), new("square_meter","Metro quadrado","m²","ruler",true,60),
        new("kilometer","Quilômetro","km","route",true,70), new("month","Mês","mês","calendar",true,80), new("package","Pacote","pct.","package",true,90), new("other","Outro","outro","circle",true,100)
    ];
    public IReadOnlyList<ServiceUnitOption> GetAll() => Options;
    public bool Contains(string code) => Options.Any(x => x.Code == code);
}

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
