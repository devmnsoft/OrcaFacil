using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public interface IGlobalSearchService
{
    Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(string query, int limit = 12, CancellationToken cancellationToken = default);
}

public sealed record GlobalSearchResult(string Type, string Title, string Subtitle, string Status, string Icon, string Url, string Action, DateTime? Date = null, int Priority = 0);

public sealed class GlobalSearchService(ICurrentAccountService currentAccount, INavigationMapService navigationMap, OrcaFacilDbContext db) : IGlobalSearchService
{
    public async Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(string query, int limit = 12, CancellationToken cancellationToken = default)
    {
        if (currentAccount.AccountId is not { } accountId)
            throw new UnauthorizedAccessException("Selecione uma conta para pesquisar.");

        var term = query.Trim();
        if (term.Length < 2) return [];
        limit = Math.Clamp(limit, 1, 20);
        var categoryLimit = Math.Max(2, (int)Math.Ceiling(limit / 6d));
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

        var services = await (currentAccount.HasPermissionAsync("services.read", cancellationToken)) ? await db.ServiceCatalogItems.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive && (EF.Functions.ILike(x.Name, pattern) || (x.Description != null && EF.Functions.ILike(x.Description, pattern))))
            .OrderByDescending(x => EF.Functions.ILike(x.Name, startsWith)).ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Serviço", x.Name, x.Description ?? "Item do catálogo", "Ativo", "service", "/Services/Details/" + x.Id, "Abrir serviço", x.UpdatedAt ?? x.CreatedAt, 2)).ToListAsync(cancellationToken) : [];

        var contracts = await db.RecurringContracts.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && (EF.Functions.ILike(x.Number, pattern) || EF.Functions.ILike(x.Title, pattern)))
            .OrderByDescending(x => EF.Functions.ILike(x.Number, startsWith)).ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Contrato", x.Number, x.Title, x.Status.ToString(), "document", "/Contracts/Details/" + x.Id, "Abrir contrato", x.UpdatedAt ?? x.CreatedAt, 2)).ToListAsync(cancellationToken);

        var messages = await db.CommercialMessageTemplates.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive && (x.AccountId == accountId || x.IsSystem) && (EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.Code, pattern)))
            .OrderByDescending(x => x.AccountId == accountId).ThenBy(x => x.Name).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Template de mensagem", x.Name, x.Channel, "Ativo", "share", "/MessageTemplates/Index", "Abrir templates", x.UpdatedAt ?? x.CreatedAt, 1)).ToListAsync(cancellationToken);

        var alerts = await db.Notifications.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && !x.IsRead && (EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.Message, pattern)))
            .OrderByDescending(x => x.CreatedAt).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Alerta", x.Title, x.Message, x.Type.ToString(), "notification", x.ActionUrl ?? "/Alerts", x.ActionText ?? "Abrir alerta", x.CreatedAt, 3)).ToListAsync(cancellationToken);

        var payments = await db.Payments.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted &&
                ((x.ExternalReference != null && EF.Functions.ILike(x.ExternalReference, pattern)) || (x.PayerEmail != null && EF.Functions.ILike(x.PayerEmail, pattern))))
            .OrderByDescending(x => x.PaidAt ?? x.CreatedAt).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Pagamento", x.ExternalReference ?? "Pagamento", x.PayerEmail ?? "Cobrança da conta", x.Status.ToString(), "payment", "/Payments/Details/" + x.Id, "Abrir pagamento", x.PaidAt ?? x.CreatedAt, 2)).ToListAsync(cancellationToken);

        var budgetTemplates = await (currentAccount.HasPermissionAsync("templates.read", cancellationToken)) ? await db.BudgetTemplates.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && (x.AccountId == accountId || x.IsSystemTemplate) && (EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.Description, pattern)))
            .OrderByDescending(x => x.AccountId == accountId).ThenBy(x => x.Title).Take(categoryLimit)
            .Select(x => new GlobalSearchResult("Template", x.Title, x.Description, "Ativo", "quote-ready", "/Templates/Details/" + x.Id, "Abrir template", x.UpdatedAt ?? x.CreatedAt, 1)).ToListAsync(cancellationToken) : [];

        var permissions = currentAccount.AccountRoleCode is null ? [] : await (from role in db.Roles.AsNoTracking()
            join rolePermission in db.RolePermissions.AsNoTracking() on role.Id equals rolePermission.RoleId
            join permission in db.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
            where role.Code == currentAccount.AccountRoleCode && !role.IsDeleted && !rolePermission.IsDeleted && !permission.IsDeleted
            select permission.Code).ToArrayAsync(cancellationToken);
        var navigation = navigationMap.Search(term, permissions).Select(x => new GlobalSearchResult(
            x.IsAction ? "Ação" : "Módulo", x.Label, x.Description, "Disponível", x.Icon, x.Page, x.IsAction ? "Executar" : "Abrir módulo", null, x.IsAction ? 4 : 1));

        return navigation.Concat(clients).Concat(documents).Concat(orders).Concat(receipts).Concat(services).Concat(contracts).Concat(messages).Concat(alerts).Concat(payments).Concat(budgetTemplates)
            .OrderByDescending(x => x.Priority).ThenByDescending(x => x.Date).Take(limit).ToArray();
    }
}
