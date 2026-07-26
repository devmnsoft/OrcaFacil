using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Clients;

[Authorize]
public class IndexModel : PageModel
{
    private const int PageSize = 10;
    private readonly ICurrentUserService _current;
    private readonly OrcaFacilDbContext _db;

    public IndexModel(OrcaFacilDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public List<Client> Clients { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Document { get; set; }
    [BindProperty(SupportsGet = true)] public PersonType? PersonType { get; set; }
    [BindProperty(SupportsGet = true)] public string? City { get; set; }
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "name";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync(CancellationToken ct)
    {
        var query = BuildQuery();
        var total = await query.CountAsync(ct);
        CurrentPage = Math.Max(1, PageNumber);
        TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        Clients = await ApplySort(query)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var client = await _db.Clients.SingleOrDefaultAsync(x => x.Id == id && x.UserId == _current.UserId && !x.IsDeleted, ct);
        if (client is null) return NotFound();
        client.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Cliente excluído com sucesso.";
        return RedirectToPage(new { Search, Document, PersonType, City, Sort, PageNumber });
    }

    public static string Mask(Client client) => BrazilianDocument.Mask(client.DocumentType, client.DocumentNumber);

    private IQueryable<Client> BuildQuery()
    {
        var query = _db.Clients.AsNoTracking().Where(x => x.UserId == _current.UserId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(Search)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{Search.Trim()}%") || (x.TradeName != null && EF.Functions.ILike(x.TradeName, $"%{Search.Trim()}%")));
        var document = BrazilianDocument.Normalize(Document);
        if (!string.IsNullOrWhiteSpace(document)) query = query.Where(x => x.DocumentNumber != null && x.DocumentNumber.Contains(document));
        if (PersonType.HasValue) query = query.Where(x => x.PersonType == PersonType.Value);
        if (!string.IsNullOrWhiteSpace(City)) query = query.Where(x => x.City != null && EF.Functions.ILike(x.City, $"%{City.Trim()}%"));
        return query;
    }

    private IQueryable<Client> ApplySort(IQueryable<Client> query) => Sort switch
    {
        "city" => query.OrderBy(x => x.City).ThenBy(x => x.Name),
        "created_desc" => query.OrderByDescending(x => x.CreatedAt),
        "name_desc" => query.OrderByDescending(x => x.Name),
        _ => query.OrderBy(x => x.Name),
    };
}
