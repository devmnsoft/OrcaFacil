using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class NewModel(OrcaFacilDbContext db, ICurrentUserService current, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<ClientStartChoice> Clients { get; private set; } = [];
    public IReadOnlyList<ServiceStartChoice> Services { get; private set; } = [];
    public IReadOnlyList<TemplateStartChoice> Templates { get; private set; } = [];
    public IReadOnlyList<DraftStartChoice> Drafts { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var userId = current.UserId;
        var accountId = account.AccountId;
        Clients = await db.Clients.AsNoTracking().Where(x => x.UserId == userId && x.AccountId == accountId && !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name).Take(50).Select(x => new ClientStartChoice(x.Id, x.Name, x.Phone ?? x.Email ?? x.DocumentNumber ?? "Contato não informado", (x.Name + " " + x.Phone + " " + x.Email + " " + x.DocumentNumber).ToLower())).ToListAsync(ct);
        Services = await db.ServiceCatalogItems.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive)
            .OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name).Take(50).Select(x => new ServiceStartChoice(x.Id, x.Name, x.UnitCode, x.StandardPrice, (x.Name + " " + x.Code + " " + x.Description).ToLower())).ToListAsync(ct);
        Templates = await db.BudgetTemplates.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted && (x.IsSystemTemplate || (x.AccountId == accountId && x.UserId == userId)))
            .OrderBy(x => x.Profession).ThenBy(x => x.Title).Take(20).Select(x => new TemplateStartChoice(x.Id, x.Title, x.Profession, x.Items.Count(i => !i.IsDeleted))).ToListAsync(ct);
        Drafts = await db.Documents.AsNoTracking().Where(x => x.UserId == userId && x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.Status == "Draft")
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(5).Select(x => new DraftStartChoice(x.Id, x.Number, string.IsNullOrEmpty(x.ClientName) ? "Cliente ainda não escolhido" : x.ClientName, x.Total, x.UpdatedAt ?? x.CreatedAt)).ToListAsync(ct);
    }
}

public sealed record ClientStartChoice(Guid Id, string Name, string Detail, string SearchText);
public sealed record ServiceStartChoice(Guid Id, string Name, string Unit, decimal Price, string SearchText);
public sealed record TemplateStartChoice(Guid Id, string Title, string Profession, int ItemCount);
public sealed record DraftStartChoice(Guid Id, string Number, string ClientName, decimal Total, DateTime ChangedAt);
