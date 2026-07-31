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
        var categoryLimit = Math.Max(1, (int)Math.Ceiling(limit / 4d));
        var pattern = $"%{term.Replace("%", "\\%").Replace("_", "\\_")}%";
        var startsWith = $"{term.Replace("%", "\\%").Replace("_", "\\_")}%";

        var clients = await (currentAccount.HasPermissionAsync("clients.read", cancellationToken)) ? await db.Clients.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted &&
                (EF.Functions.ILike(x.Name, pattern) ||
                 (x.Phone != null && EF.Functions.ILike(x.Phone, pattern))))
            .OrderByDescending(x => x.Name.ToLower() == term.ToLower())
            .ThenByDescending(x => EF.Functions.ILike(x.Name, startsWith))
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt).ThenBy(x => x.Name).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Cliente", x.Name,
                x.City ?? "Sem cidade informada", "Ativo", "client",
                "/Clients/Details/" + x.Id, "Abrir cliente"))
            .ToListAsync(cancellationToken) : [];

        var documents = await (currentAccount.HasPermissionAsync("documents.read", cancellationToken)) ? await db.Documents.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted &&
                (EF.Functions.ILike(x.Number, pattern) || EF.Functions.ILike(x.ClientName, pattern)))
            .OrderByDescending(x => x.Number.ToLower() == term.ToLower() || x.ClientName.ToLower() == term.ToLower())
            .ThenByDescending(x => EF.Functions.ILike(x.Number, startsWith) || EF.Functions.ILike(x.ClientName, startsWith))
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Orçamento", x.Number, x.ClientName, x.Status, "budget",
                "/Documents/Details/" + x.Id, "Abrir orçamento"))
            .ToListAsync(cancellationToken) : [];

        var orders = await (currentAccount.HasPermissionAsync("work_orders.read", cancellationToken)) ? await db.WorkOrders.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted &&
                (EF.Functions.ILike(x.Number, pattern) || EF.Functions.ILike(x.Title, pattern)))
            .OrderByDescending(x => x.Number.ToLower() == term.ToLower() || x.Title.ToLower() == term.ToLower())
            .ThenByDescending(x => EF.Functions.ILike(x.Number, startsWith) || EF.Functions.ILike(x.Title, startsWith))
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Ordem de serviço", x.Number, x.Title, x.Status.ToString(), "service",
                "/WorkOrders/Details/" + x.Id, "Abrir ordem"))
            .ToListAsync(cancellationToken) : [];

        var receipts = await (currentAccount.HasPermissionAsync("receipts.read", cancellationToken)) ? await db.Receipts.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && EF.Functions.ILike(x.Number, pattern))
            .OrderByDescending(x => x.Number.ToLower() == term.ToLower())
            .ThenByDescending(x => EF.Functions.ILike(x.Number, startsWith))
            .ThenByDescending(x => x.IssuedAt).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Recibo", x.Number, "Valor registrado: " + x.Amount, "Emitido", "receipt",
                "/Receipts/Details/" + x.Id, "Abrir recibo"))
            .ToListAsync(cancellationToken) : [];

        return clients.Concat(documents).Concat(orders).Concat(receipts)
            .Take(limit).ToArray();
    }
}
