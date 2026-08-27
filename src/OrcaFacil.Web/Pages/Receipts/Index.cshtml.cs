using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.ViewModels.Receipts;

namespace OrcaFacil.Web.Pages.Receipts;

[Authorize]
public sealed class IndexModel(
    IReceiptQueryService receipts,
    ICurrentAccountService account,
    OrcaFacilDbContext db) : PageModel
{
    private const string DefaultSort = "recent";

    private static readonly HashSet<string> AllowedSorts =
    [
        DefaultSort,
        "oldest",
        "amount_desc",
        "amount_asc",
        "client"
    ];

    private static readonly HashSet<string> AllowedStatuses =
    [
        "active",
        "cancelled"
    ];

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ClientId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PaymentMethod { get; set; }

    [BindProperty(SupportsGet = true)]
    public ReceiptOriginType? OriginType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinimumAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaximumAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = DefaultSort;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    public ReceiptIndexFilterState Filters { get; private set; } = new();

    public ReceiptListResult Result { get; private set; } = new([], 0, 1, 25, 1, 0, 0, 0, 0);

    public IReadOnlyList<ReceiptListItem> Receipts => Result.Items;

    public IReadOnlyList<SelectListItem> Clients { get; private set; } = [];

    public int TotalPages => Result.TotalPages;

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public int PreviousPage => Math.Max(1, PageNumber - 1);

    public int NextPage => Math.Min(TotalPages, PageNumber + 1);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!account.HasAccount)
        {
            return Forbid();
        }

        Clients = await db.Clients
            .AsNoTracking()
            .Where(client => client.AccountId == account.AccountId && client.IsActive && !client.IsDeleted)
            .OrderBy(client => client.Name)
            .Select(client => new SelectListItem(client.Name, client.Id.ToString()))
            .ToListAsync(ct);

        ValidateFilters();

        PageNumber = Math.Max(1, PageNumber);
        PageSize = Math.Clamp(PageSize, 10, 100);
        Filters = CreateFilterState();

        var result = await receipts.ListAsync(
            new ReceiptListQuery(
                From,
                To,
                ClientId,
                PaymentMethod,
                OriginType,
                Status,
                MinimumAmount,
                MaximumAmount,
                Sort,
                PageNumber,
                PageSize),
            ct);

        if (result is null)
        {
            return Forbid();
        }

        Result = result;
        PageNumber = result.Page;
        PageSize = result.PageSize;
        Filters = CreateFilterState();

        return Page();
    }

    public IDictionary<string, string> GetRouteValues(int pageNumber)
    {
        return CreateRouteValues(pageNumber, excludedFilter: null);
    }

    public IDictionary<string, string> GetRouteValuesWithout(string filterName)
    {
        return CreateRouteValues(1, filterName);
    }

    private Dictionary<string, string> CreateRouteValues(int pageNumber, string? excludedFilter)
    {
        var values = new Dictionary<string, string>
        {
            [nameof(PageNumber)] = Math.Max(1, pageNumber).ToString(CultureInfo.InvariantCulture),
            [nameof(PageSize)] = Filters.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        AddRouteValue(values, nameof(Sort), Filters.Sort, excludedFilter);
        AddRouteValue(values, nameof(From), Filters.From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), excludedFilter);
        AddRouteValue(values, nameof(To), Filters.To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), excludedFilter);
        AddRouteValue(values, nameof(ClientId), Filters.ClientId?.ToString(), excludedFilter);
        AddRouteValue(values, nameof(PaymentMethod), Filters.PaymentMethod, excludedFilter);
        AddRouteValue(values, nameof(OriginType), Filters.OriginType?.ToString(), excludedFilter);
        AddRouteValue(values, nameof(Status), Filters.Status, excludedFilter);
        AddRouteValue(values, nameof(MinimumAmount), Filters.MinimumAmount?.ToString(CultureInfo.InvariantCulture), excludedFilter);
        AddRouteValue(values, nameof(MaximumAmount), Filters.MaximumAmount?.ToString(CultureInfo.InvariantCulture), excludedFilter);

        return values;
    }

    private static void AddRouteValue(
        IDictionary<string, string> values,
        string key,
        string? value,
        string? excludedFilter)
    {
        var isPeriodExcluded = string.Equals(excludedFilter, "Period", StringComparison.OrdinalIgnoreCase)
            && (key == nameof(From) || key == nameof(To));

        if (!isPeriodExcluded
            && !string.Equals(key, excludedFilter, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(value))
        {
            values[key] = value;
        }
    }

    private ReceiptIndexFilterState CreateFilterState()
    {
        return new ReceiptIndexFilterState
        {
            From = From,
            To = To,
            ClientId = ClientId,
            PaymentMethod = PaymentMethod,
            OriginType = OriginType,
            Status = Status,
            MinimumAmount = MinimumAmount,
            MaximumAmount = MaximumAmount,
            Sort = Sort,
            PageNumber = PageNumber,
            PageSize = PageSize
        };
    }

    private void ValidateFilters()
    {
        if (From.HasValue && To.HasValue && From.Value.Date > To.Value.Date)
        {
            ModelState.AddModelError(nameof(To), "A data final deve ser igual ou posterior à data inicial.");
        }

        if (MinimumAmount < 0)
        {
            ModelState.AddModelError(nameof(MinimumAmount), "O valor mínimo não pode ser negativo.");
        }

        if (MaximumAmount < 0)
        {
            ModelState.AddModelError(nameof(MaximumAmount), "O valor máximo não pode ser negativo.");
        }

        if (MinimumAmount.HasValue && MaximumAmount.HasValue && MinimumAmount > MaximumAmount)
        {
            ModelState.AddModelError(nameof(MaximumAmount), "O valor máximo deve ser igual ou maior que o mínimo.");
        }

        if (!string.IsNullOrWhiteSpace(PaymentMethod)
            && (!PaymentMethodCodes.TryParse(PaymentMethod, out var method)
                || !string.Equals(PaymentMethod, method.ToCode(), StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(PaymentMethod), "Selecione um método de pagamento válido.");
            PaymentMethod = null;
        }

        if (!string.IsNullOrWhiteSpace(Status) && !AllowedStatuses.Contains(Status))
        {
            ModelState.AddModelError(nameof(Status), "Selecione uma situação conhecida.");
            Status = null;
        }

        if (!AllowedSorts.Contains(Sort))
        {
            ModelState.AddModelError(nameof(Sort), "Selecione uma ordenação válida.");
            Sort = DefaultSort;
        }

        if (!ModelState.IsValid)
        {
            PageNumber = 1;
        }
    }
}
