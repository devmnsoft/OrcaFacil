using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class QuoteWorkspaceService(OrcaFacilDbContext db, ICurrentAccountService currentAccount, ICommercialBalanceService balances)
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
        if (string.Equals(request.Status, "AwaitingDecision", StringComparison.OrdinalIgnoreCase))
            query = query.Where(document => document.Status == "Sent" || document.Status == "Viewed");
        else if (string.Equals(request.Status, "ChangeRequested", StringComparison.OrdinalIgnoreCase))
            query = query.Where(document => document.Status == "InNegotiation" || document.Status == "ChangeRequested");
        else if (string.Equals(request.Status, "FollowUpScheduled", StringComparison.OrdinalIgnoreCase))
            query = query.Where(document => document.FollowUpStatus == FollowUpStatus.Scheduled && document.NextFollowUpAt != null);
        else if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(document => document.Status == request.Status);
        if (request.ClientId is { } clientId) query = query.Where(document => document.ClientId == clientId);
        if (request.AssignedToUserId is { } assignedId) query = query.Where(document => document.AssignedToUserId == assignedId);
        if (request.From is { } from) query = query.Where(document => document.IssueDate >= from);
        if (request.To is { } to) query = query.Where(document => document.IssueDate < to.Date.AddDays(1));

        var rows = await query
            .Select(document => new { document.Id, document.Number, document.Status, document.ClientName,
                document.Total, document.IssueDate, document.ValidUntil, document.CreatedAt, document.AssignedToUserId })
            .ToListAsync(cancellationToken);

        var docIds = rows.Select(r => r.Id).ToArray();
        var assigneeIds = rows.Where(r => r.AssignedToUserId.HasValue).Select(r => r.AssignedToUserId!.Value).Distinct().ToArray();
        
        var assignees = assigneeIds.Length == 0 ? new Dictionary<Guid, string>() :
            await db.Users.AsNoTracking().Where(u => assigneeIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var revisions = await db.DocumentRevisions.AsNoTracking()
            .Where(r => r.AccountId == accountId && docIds.Contains(r.DocumentId) && r.IsCurrent)
            .Select(r => new { r.DocumentId, r.VersionNumber, r.Total })
            .ToDictionaryAsync(r => r.DocumentId, r => r, cancellationToken);

        var workOrders = await db.WorkOrders.AsNoTracking()
            .Where(w => w.AccountId == accountId && w.SourceDocumentId != null && docIds.Contains(w.SourceDocumentId.Value) && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new { w.Id, SourceDocumentId = w.SourceDocumentId!.Value, w.Number, w.TotalSnapshot })
            .ToListAsync(cancellationToken);

        // Agrupamento para não quebrar com colisão de chave se houver mais de uma ordem histórica
        var woMap = workOrders
            .GroupBy(w => w.SourceDocumentId)
            .ToDictionary(g => g.Key, g => g.First());

        var allItems = new List<QuoteWorkspaceItem>(rows.Count);
        foreach (var row in rows)
        {
            var assigneeName = row.AssignedToUserId.HasValue && assignees.TryGetValue(row.AssignedToUserId.Value, out var name) ? name : null;
            revisions.TryGetValue(row.Id, out var currentRev);
            var revisionNumber = currentRev?.VersionNumber ?? 0;
            woMap.TryGetValue(row.Id, out var wo);

            var balance = await balances.GetForDocumentAsync(accountId, row.Id, wo?.Id, cancellationToken);
            var baseTotal = balance?.ContractedAmount ?? currentRev?.Total ?? row.Total;

            allItems.Add(new QuoteWorkspaceItem(
                row.Id,
                row.Number,
                row.Status,
                row.ClientName,
                baseTotal,
                row.IssueDate,
                row.ValidUntil,
                row.CreatedAt,
                NextAction(row.Id, row.Status, wo?.Id),
                revisionNumber,
                assigneeName,
                balance?.ReceivedAmount ?? 0m,
                balance?.BalanceAmount ?? 0m,
                wo?.Id,
                wo?.Number,
                balance?.ReversedAmount ?? 0m,
                balance?.OverpaidAmount ?? 0m,
                balance?.ContractSource ?? "Document",
                balance?.Warnings ?? []));
        }

        IEnumerable<QuoteWorkspaceItem> filteredItems = allItems;
        if (request.Minimum is { } minimum) filteredItems = filteredItems.Where(item => item.Total >= minimum);
        if (request.Maximum is { } maximum) filteredItems = filteredItems.Where(item => item.Total <= maximum);

        filteredItems = request.Sort switch
        {
            "oldest" => filteredItems.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            "value-desc" => filteredItems.OrderByDescending(item => item.Total).ThenByDescending(item => item.CreatedAt).ThenBy(item => item.Id),
            "value-asc" => filteredItems.OrderBy(item => item.Total).ThenByDescending(item => item.CreatedAt).ThenBy(item => item.Id),
            "validity" => filteredItems.OrderBy(item => item.ValidUntil ?? DateTime.MaxValue).ThenByDescending(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => filteredItems.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
        };

        var materialized = filteredItems.ToArray();
        var total = materialized.Length;
        var items = materialized.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

        return OperationResult<PagedResult<QuoteWorkspaceItem>>.Success(new(items, total, page, pageSize));
    }

    private static NextActionDescriptor NextAction(Guid documentId, string status, Guid? workOrderId) => status.ToUpperInvariant() switch
    {
        "DRAFT" => Action("continue", "Continuar orçamento", "Complete os dados antes de compartilhar.",
            "/Documents/CreateBudget", documentId),
        "ISSUED" or "READY" => DetailsAction("share", "Criar acesso", "Envie uma versão segura ao cliente.", documentId, "sharing"),
        "SENT" or "VIEWED" => DetailsAction("follow-up", "Programar retorno", "Mantenha a negociação avançando.", documentId, "negotiation"),
        "APPROVED" when !workOrderId.HasValue => DetailsAction("work-order", "Criar ordem", "Transforme a aprovação em execução.", documentId, "summary"),
        "APPROVED" => Action("payment", "Registrar recebimento", "Receba pagamento ou consulte o saldo.",
            "/Payments/Register", workOrderId.Value),
        _ => DetailsAction("review", "Revisar proposta", "Consulte o histórico e defina o próximo passo.", documentId)
    };

    private static NextActionDescriptor DetailsAction(string code, string title, string description, Guid documentId, string tab = "summary") =>
        new(code, title, description, "/Documents/Details", new Dictionary<string, string>
        {
            ["id"] = documentId.ToString(),
            ["tab"] = tab
        });

    private static NextActionDescriptor Action(string code, string title, string description, string page, Guid targetId) =>
        new(code, title, description, page, new Dictionary<string, string>
        {
            ["id"] = targetId.ToString()
        });
}
