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
public sealed class IndexModel : PageModel
{
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;
    private readonly OrcaFacilDbContext _db;

    public IndexModel(OrcaFacilDbContext db, ICurrentUserService current, IAuditService audit)
    {
        _db = db;
        _current = current;
        _audit = audit;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Document { get; set; }

    [BindProperty(SupportsGet = true)]
    public PersonType? PersonType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? City { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "name";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 10;

    public IReadOnlyList<ClientListItemDto> Clients { get; private set; } = Array.Empty<ClientListItemDto>();

    public int TotalItems { get; private set; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        !string.IsNullOrWhiteSpace(Document) ||
        PersonType.HasValue ||
        !string.IsNullOrWhiteSpace(City);

    public async Task OnGetAsync(CancellationToken ct)
    {
        var query = BuildQuery(asNoTracking: true);
        TotalItems = await query.CountAsync(ct);
        PageNumber = Math.Clamp(PageNumber <= 0 ? 1 : PageNumber, 1, TotalPages);

        Clients = await ApplySorting(query)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .Select(client => new ClientListItemDto(
                client.Id,
                client.PersonType,
                client.DocumentType,
                client.DocumentNumber,
                client.Name,
                client.TradeName,
                client.Email,
                client.Phone,
                client.City,
                client.CreatedAt,
                client.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var client = await _db.Clients.SingleOrDefaultAsync(x => x.Id == id && x.UserId == _current.UserId && !x.IsDeleted, ct);
        if (client is null)
        {
            TempData["Error"] = "Cliente não encontrado ou indisponível para este usuário.";
            return RedirectToPage("/Clients/Index", CurrentRouteValues());
        }

        var auditBefore = new { client.Id, client.Name, client.PersonType, client.City };
        client.MarkAsDeleted();
        await _audit.RegisterAsync(_current.UserId, "client.deleted", nameof(Client), client.Id.ToString(), auditBefore, new { client.IsDeleted }, null, ct);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Cliente removido da sua lista.";
        return RedirectToPage("/Clients/Index", CurrentRouteValues());
    }

    public object CurrentRouteValues() => new { Search, Document, PersonType, City, Sort, PageNumber };

    public static string Mask(BrazilianDocumentType? documentType, string? documentNumber) =>
        BrazilianDocument.Mask(documentType, documentNumber);

    public static string GetClientTag(ClientListItemDto client)
    {
        if (client.UpdatedAt.HasValue && client.UpdatedAt.Value < DateTime.UtcNow.AddDays(-180)) return "Inativo";
        if (client.CreatedAt >= DateTime.UtcNow.AddDays(-30)) return "Novo";
        if (!string.IsNullOrWhiteSpace(client.City)) return "Recorrente";
        return "Prioridade";
    }

    private IQueryable<Client> BuildQuery(bool asNoTracking)
    {
        var query = _db.Clients.Where(x => x.UserId == _current.UserId && !x.IsDeleted);
        if (asNoTracking) query = query.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = $"%{Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, search) || (x.TradeName != null && EF.Functions.ILike(x.TradeName, search)));
        }

        var normalizedDocument = BrazilianDocument.Normalize(Document);
        if (!string.IsNullOrWhiteSpace(normalizedDocument)) query = query.Where(x => x.DocumentNumber != null && x.DocumentNumber.Contains(normalizedDocument));
        if (PersonType.HasValue) query = query.Where(x => x.PersonType == PersonType.Value);
        if (!string.IsNullOrWhiteSpace(City)) query = query.Where(x => x.City != null && EF.Functions.ILike(x.City, $"%{City.Trim()}%"));
        return query;
    }

    private IQueryable<Client> ApplySorting(IQueryable<Client> query) => Sort?.ToLowerInvariant() switch
    {
        "city" => query.OrderBy(x => x.City).ThenBy(x => x.Name),
        "created_desc" => query.OrderByDescending(x => x.CreatedAt),
        "name_desc" => query.OrderByDescending(x => x.Name),
        _ => query.OrderBy(x => x.Name)
    };
}

public sealed record ClientListItemDto(
    Guid Id,
    PersonType PersonType,
    BrazilianDocumentType? DocumentType,
    string? DocumentNumber,
    string Name,
    string? TradeName,
    string? Email,
    string? Phone,
    string? City,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public string MaskedDocument => IndexModel.Mask(DocumentType, DocumentNumber);
    public string PersonTypeLabel => PersonType == PersonType.Company ? "PJ" : "PF";
    public string PersonTypeName => PersonType == PersonType.Company ? "Pessoa Jurídica" : "Pessoa Física";
}
