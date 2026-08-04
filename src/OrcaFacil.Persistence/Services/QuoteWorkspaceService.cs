using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class QuoteWorkspaceService(OrcaFacilDbContext db, ICurrentAccountService currentAccount)
    : IQuoteWorkspaceService
{
    public async Task<OperationResult<PagedResult<QuoteWorkspaceItem>>> ListAsync(QuoteWorkspaceQuery request,
        CancellationToken cancellationToken = default)
    {
        await currentAccount.EnsureAccountAccessAsync(cancellationToken);
        if (currentAccount.AccountId is not { } accountId ||
            !await currentAccount.HasPermissionAsync("documents.read", cancellationToken))
            return OperationResult<PagedResult<QuoteWorkspaceItem>>.Failure("access_denied", "Você não tem acesso aos orçamentos desta conta.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 10, 50);
        var query = db.Documents.AsNoTracking().Where(document =>
            document.AccountId == accountId && document.Type == DocumentType.Budget && !document.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(document => EF.Functions.ILike(document.Number, $"%{search}%") ||
                                             EF.Functions.ILike(document.ClientName, $"%{search}%"));
        }
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(document => document.Status == request.Status);
        if (request.ClientId is { } clientId) query = query.Where(document => document.ClientId == clientId);
        if (request.From is { } from) query = query.Where(document => document.IssueDate >= from);
        if (request.To is { } to) query = query.Where(document => document.IssueDate < to.Date.AddDays(1));
        if (request.Minimum is { } minimum) query = query.Where(document => document.Total >= minimum);
        if (request.Maximum is { } maximum) query = query.Where(document => document.Total <= maximum);

        query = request.Sort switch
        {
            "oldest" => query.OrderBy(document => document.CreatedAt),
            "value-desc" => query.OrderByDescending(document => document.Total),
            "value-asc" => query.OrderBy(document => document.Total),
            "validity" => query.OrderBy(document => document.ValidUntil),
            _ => query.OrderByDescending(document => document.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(document => new { document.Id, document.Number, document.Status, document.ClientName,
                document.Total, document.IssueDate, document.ValidUntil, document.CreatedAt })
            .ToListAsync(cancellationToken);
        var items = rows.Select(row => new QuoteWorkspaceItem(row.Id, row.Number, row.Status, row.ClientName,
            row.Total, row.IssueDate, row.ValidUntil, row.CreatedAt, NextAction(row.Id, row.Status))).ToArray();
        return OperationResult<PagedResult<QuoteWorkspaceItem>>.Success(new(items, total, page, pageSize));
    }

    private static NextActionDescriptor NextAction(Guid id, string status) => status.ToUpperInvariant() switch
    {
        "DRAFT" => Action("continue", "Continuar orçamento", "Complete os dados antes de compartilhar.", id),
        "ISSUED" or "READY" => Action("share", "Criar acesso", "Envie uma versão segura ao cliente.", id, "sharing"),
        "SENT" or "VIEWED" => Action("follow-up", "Programar retorno", "Mantenha a negociação avançando.", id, "negotiation"),
        "APPROVED" => Action("work-order", "Criar ordem", "Transforme a aprovação em execução.", id, "summary"),
        _ => Action("review", "Revisar proposta", "Consulte o histórico e defina o próximo passo.", id)
    };

    private static NextActionDescriptor Action(string code, string title, string description, Guid id, string tab = "summary") =>
        new(code, title, description, "/Documents/Details", new Dictionary<string, string> { ["id"] = id.ToString(), ["tab"] = tab });
}
