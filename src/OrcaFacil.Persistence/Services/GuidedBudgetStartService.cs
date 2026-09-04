using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class GuidedBudgetStartService(
    OrcaFacilDbContext db,
    ICurrentUserService currentUser,
    ICurrentAccountService currentAccount) : IGuidedBudgetStartService
{
    public async Task<GuidedBudgetStartView> GetAsync(CancellationToken ct = default)
    {
        if (currentAccount.AccountId is not Guid accountId)
            return Empty();

        var userId = currentUser.UserId;
        var clients = await db.Clients.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name).Take(50)
            .Select(x => new BudgetStartClient(x.Id, x.Name,
                x.Phone ?? x.Email ?? x.DocumentNumber ?? "Contato não informado",
                (x.Name + " " + x.Phone + " " + x.Email + " " + x.DocumentNumber).ToLower()))
            .ToListAsync(ct);
        var services = await db.ServiceCatalogItems.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive)
            .OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name).Take(50)
            .Select(x => new BudgetStartService(x.Id, x.Name, x.Description, x.UnitCode, x.StandardPrice,
                (x.Name + " " + x.Code + " " + x.Description).ToLower()))
            .ToListAsync(ct);
        var templates = await db.BudgetTemplates.AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted &&
                        (x.IsSystemTemplate ||
                         (x.AccountId == accountId && (x.UserId == null || x.UserId == userId))))
            .OrderBy(x => x.Profession).ThenBy(x => x.Title).Take(20)
            .Select(x => new BudgetStartTemplate(x.Id, x.Title, x.Profession, x.Items.Count(i => !i.IsDeleted)))
            .ToListAsync(ct);
        var drafts = await db.Documents.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.UserId == userId && !x.IsDeleted &&
                        x.Type == DocumentType.Budget && x.Status == "Draft")
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(5)
            .Select(x => new BudgetStartDraft(x.Id, x.Number,
                string.IsNullOrEmpty(x.ClientName) ? "Cliente ainda não escolhido" : x.ClientName,
                x.Total, x.UpdatedAt ?? x.CreatedAt)).ToListAsync(ct);
        return new(clients, services, templates, drafts, EmptyStates());
    }

    private static GuidedBudgetStartView Empty() => new([], [], [], [], EmptyStates());
    private static IReadOnlyDictionary<string, BudgetStartEmptyState> EmptyStates() =>
        new Dictionary<string, BudgetStartEmptyState>
        {
            ["clients"] = new("Sua carteira começa aqui", "Cadastre um cliente para preencher a proposta automaticamente.", "Cadastrar cliente", "/Clients/Create"),
            ["services"] = new("Monte seu catálogo", "Cadastre serviços com unidade e preço para adicioná-los sem retrabalho.", "Criar serviço", "/Services/Create"),
            ["templates"] = new("Nenhum modelo ativo", "Comece em branco ou prepare uma estrutura reutilizável.", "Ver templates", "/Templates/Index"),
            ["drafts"] = new("Tudo em dia", "Seus próximos rascunhos aparecerão aqui.", "Iniciar orçamento", "/Documents/CreateBudget")
        };
}
