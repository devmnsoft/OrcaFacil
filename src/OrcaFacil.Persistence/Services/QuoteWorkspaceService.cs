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

        var woIds = workOrders.Select(w => w.Id).ToArray();
        var woToDoc = workOrders.ToDictionary(w => w.Id, w => w.SourceDocumentId);

        // Busca pagamentos ativos ligados diretamente ao orçamento OU ligados a ordens do orçamento
        var paymentsQuery = db.ManualPayments.AsNoTracking()
            .Where(p => p.AccountId == accountId && !p.IsDeleted && p.Status == FinancialRecordStatus.Active);

        var candidatePayments = await paymentsQuery
            .Where(p => (p.DocumentId != null && docIds.Contains(p.DocumentId.Value)) ||
                        (p.WorkOrderId != null && woIds.Contains(p.WorkOrderId.Value)))
            .Select(p => new { p.Id, p.DocumentId, p.WorkOrderId, p.Amount })
            .ToListAsync(cancellationToken);

        // Agrupa por documento sem duplicar o mesmo pagamento
        var paidByDoc = new Dictionary<Guid, decimal>();
        foreach (var p in candidatePayments)
        {
            Guid? targetDocId = null;
            if (p.DocumentId.HasValue && docIds.Contains(p.DocumentId.Value))
                targetDocId = p.DocumentId.Value;
            else if (p.WorkOrderId.HasValue && woToDoc.TryGetValue(p.WorkOrderId.Value, out var mappedDocId))
                targetDocId = mappedDocId;

            if (targetDocId.HasValue)
            {
                paidByDoc[targetDocId.Value] = paidByDoc.GetValueOrDefault(targetDocId.Value) + p.Amount;
            }
        }

        var items = rows.Select(row => {
            var assigneeName = row.AssignedToUserId.HasValue && assignees.TryGetValue(row.AssignedToUserId.Value, out var name) ? name : null;
            revisions.TryGetValue(row.Id, out var currentRev);
            var revisionNumber = currentRev?.VersionNumber ?? 0;
            woMap.TryGetValue(row.Id, out var wo);

            var paid = paidByDoc.GetValueOrDefault(row.Id, 0m);

            // A base do saldo é o valor contratado na ordem, ou a revisão comercial emitida, ou o total do documento
            var baseTotal = wo is not null && wo.TotalSnapshot > 0 ? wo.TotalSnapshot
                : (currentRev is not null && currentRev.Total > 0 ? currentRev.Total : row.Total);

            var balance = Math.Max(0, baseTotal - paid);

            return new QuoteWorkspaceItem(
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
                paid,
                balance,
                wo?.Id,
                wo?.Number);
        }).ToArray();

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
