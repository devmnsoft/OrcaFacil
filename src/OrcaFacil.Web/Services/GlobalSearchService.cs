using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public interface IGlobalSearchService
{
    Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(string query, int limit = 12, CancellationToken cancellationToken = default);
}

public sealed record GlobalSearchResult(string Type, string Title, string Subtitle, string Status, string Icon, string Url, string Action);

public sealed class GlobalSearchService(ICurrentAccountService currentAccount, OrcaFacilDbContext db) : IGlobalSearchService
{
    public async Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(string query, int limit = 12, CancellationToken cancellationToken = default)
    {
        if (currentAccount.AccountId is not { } accountId)
            throw new UnauthorizedAccessException("Selecione uma conta para pesquisar.");

        var term = query.Trim();
        if (term.Length < 2) return [];
        limit = Math.Clamp(limit, 1, 20);
        var pattern = $"%{term.Replace("%", "\\%").Replace("_", "\\_")}%";

        var clients = await db.Clients.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted &&
                (EF.Functions.ILike(x.Name, pattern) || (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                 (x.Phone != null && EF.Functions.ILike(x.Phone, pattern))))
            .OrderBy(x => x.Name).Take(limit)
            .Select(x => new GlobalSearchResult("Cliente", x.Name,
                (x.City ?? "Sem cidade") + (x.Email == null ? "" : " · " + x.Email), "Ativo", "client",
                "/Clients/Details/" + x.Id, "Abrir cliente"))
            .ToListAsync(cancellationToken);

        var documents = await db.Documents.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted &&
                (EF.Functions.ILike(x.Number, pattern) || EF.Functions.ILike(x.ClientName, pattern)))
            .OrderByDescending(x => x.UpdatedAt).Take(limit)
            .Select(x => new GlobalSearchResult("Orçamento", x.Number, x.ClientName, x.Status, "budget",
                "/Documents/Details/" + x.Id, "Abrir orçamento"))
            .ToListAsync(cancellationToken);

        var orders = await db.WorkOrders.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted &&
                (EF.Functions.ILike(x.Number, pattern) || EF.Functions.ILike(x.Title, pattern)))
            .OrderByDescending(x => x.UpdatedAt).Take(limit)
            .Select(x => new GlobalSearchResult("Ordem de serviço", x.Number, x.Title, x.Status.ToString(), "service",
                "/WorkOrders/Details/" + x.Id, "Abrir ordem"))
            .ToListAsync(cancellationToken);

        var receipts = await db.Receipts.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && EF.Functions.ILike(x.Number, pattern))
            .OrderByDescending(x => x.IssuedAt).Take(limit)
            .Select(x => new GlobalSearchResult("Recibo", x.Number, "Valor registrado: " + x.Amount, "Emitido", "receipt",
                "/Receipts/Details/" + x.Id, "Abrir recibo"))
            .ToListAsync(cancellationToken);

        return clients.Concat(documents).Concat(orders).Concat(receipts)
            .OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase).Take(limit).ToArray();
    }
}
