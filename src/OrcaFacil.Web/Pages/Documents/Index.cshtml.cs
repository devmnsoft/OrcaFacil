using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Documents;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class IndexModel(IQuoteWorkspaceService quotes, OrcaFacilDbContext db, ICurrentAccountService currentAccount) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ClientId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? AssignedToUserId { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? Minimum { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? Maximum { get; set; }
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "newest";
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public PagedResult<QuoteWorkspaceItem> Quotes { get; private set; } = new([], 0, 1, 20);
    public IReadOnlyList<ClientFilterOption> ClientsList { get; private set; } = [];
    public IReadOnlyList<AssigneeFilterOption> AssigneesList { get; private set; } = [];

    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(Search) 
        || !string.IsNullOrWhiteSpace(Status) 
        || ClientId.HasValue 
        || AssignedToUserId.HasValue 
        || From.HasValue 
        || To.HasValue 
        || Minimum.HasValue 
        || Maximum.HasValue 
        || (Sort != "newest" && !string.IsNullOrEmpty(Sort));

    public sealed record ClientFilterOption(Guid Id, string Name);
    public sealed record AssigneeFilterOption(Guid Id, string Name);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Minimum is < 0) ModelState.AddModelError(nameof(Minimum), "Informe um valor mínimo válido.");
        if (Maximum is < 0) ModelState.AddModelError(nameof(Maximum), "Informe um valor máximo válido.");
        if (Minimum.HasValue && Maximum.HasValue && Minimum > Maximum)
            ModelState.AddModelError(nameof(Maximum), "O valor máximo deve ser maior ou igual ao mínimo.");
        if (!ModelState.IsValid) return Page();

        if (currentAccount.AccountId is Guid accountId)
        {
            ClientsList = await db.Clients.AsNoTracking()
                .Where(c => c.AccountId == accountId && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new ClientFilterOption(c.Id, c.Name))
                .ToListAsync(cancellationToken);

            AssigneesList = await (from m in db.AccountMembers.AsNoTracking()
                                   join u in db.Users.AsNoTracking() on m.UserId equals u.Id
                                   where m.AccountId == accountId && !m.IsDeleted && !u.IsDeleted
                                   orderby u.Name
                                   select new AssigneeFilterOption(u.Id, u.Name))
                                   .ToListAsync(cancellationToken);
        }

        var result = await quotes.ListAsync(new(
            Search: Search,
            Status: Status,
            ClientId: ClientId,
            AssignedToUserId: AssignedToUserId,
            From: From,
            To: To,
            Minimum: Minimum,
            Maximum: Maximum,
            Sort: Sort,
            Page: CurrentPage), cancellationToken);

        if (!result.Succeeded || result.Value is null)
            return result.Code == "access_denied" ? Forbid() : StatusCode(StatusCodes.Status503ServiceUnavailable);

        Quotes = result.Value;
        return Page();
    }
}
