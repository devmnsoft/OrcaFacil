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
            .Select(r => new { r.DocumentId, r.VersionNumber })
            .ToDictionaryAsync(r => r.DocumentId, r => r.VersionNumber, cancellationToken);

        var workOrders = await db.WorkOrders.AsNoTracking()
            .Where(w => w.AccountId == accountId && w.SourceDocumentId != null && docIds.Contains(w.SourceDocumentId.Value) && !w.IsDeleted)
            .Select(w => new { w.Id, SourceDocumentId = w.SourceDocumentId!.Value, w.Number, w.TotalSnapshot })
            .ToListAsync(cancellationToken);

        var woIds = workOrders.Select(w => w.Id).ToArray();
        var payments = woIds.Length == 0 ? new Dictionary<Guid, decimal>() :
            await db.ManualPayments.AsNoTracking()
                .Where(p => p.AccountId == accountId && p.WorkOrderId != null && woIds.Contains(p.WorkOrderId.Value) && p.Status == FinancialRecordStatus.Active && !p.IsDeleted)
                .GroupBy(p => p.WorkOrderId!.Value)
                .Select(g => new { WorkOrderId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(g => g.WorkOrderId, g => g.TotalPaid, cancellationToken);

        var woMap = workOrders.ToDictionary(w => w.SourceDocumentId, w => w);

        var items = rows.Select(row => {
            var assigneeName = row.AssignedToUserId.HasValue && assignees.TryGetValue(row.AssignedToUserId.Value, out var name) ? name : null;
            var revisionNumber = revisions.TryGetValue(row.Id, out var rev) ? rev : 0;
            woMap.TryGetValue(row.Id, out var wo);
            var paid = wo is not null && payments.TryGetValue(wo.Id, out var p) ? p : 0m;
            var balance = wo is not null ? Math.Max(0, (wo.TotalSnapshot > 0 ? wo.TotalSnapshot : row.Total) - paid) : row.Total;

            return new QuoteWorkspaceItem(
                row.Id,
                row.Number,
                row.Status,
                row.ClientName,
                row.Total,
                row.IssueDate,
                row.ValidUntil,
                row.CreatedAt,
                NextAction(row.Id, row.Status),
                revisionNumber,
                assigneeName,
                paid,
                balance,
                wo?.Id,
                wo?.Number);
        }).ToArray();

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
