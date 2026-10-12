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

        var needsFinancialMaterialization = request.Minimum.HasValue
            || request.Maximum.HasValue
            || string.Equals(request.Sort, "value-desc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.Sort, "value-asc", StringComparison.OrdinalIgnoreCase);

        var rows = needsFinancialMaterialization
            ? await SelectRows(query).ToListAsync(cancellationToken)
            : await SelectRows(ApplyDocumentSort(query, request.Sort))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        var totalBeforeFinancialFilters = needsFinancialMaterialization ? 0 : await query.CountAsync(cancellationToken);

        var docIds = rows.Select(r => r.Id).ToArray();
        var assigneeIds = rows.Where(r => r.AssignedToUserId.HasValue).Select(r => r.AssignedToUserId!.Value).Distinct().ToArray();
        
        var assignees = assigneeIds.Length == 0 ? new Dictionary<Guid, string>() :
            await db.Users.AsNoTracking().Where(u => assigneeIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var revisions = await db.DocumentRevisions.AsNoTracking()
            .Where(r => r.AccountId == accountId && docIds.Contains(r.DocumentId) && r.IsCurrent)
            .Select(r => new { r.Id, r.DocumentId, r.VersionNumber, r.Total, r.CreatedAt })
            .ToListAsync(cancellationToken);
        var currentRevisionByDocument = revisions
            .GroupBy(r => r.DocumentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.VersionNumber).ThenByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).First());

        var currentRevisionIds = currentRevisionByDocument.Values.Select(r => r.Id).ToArray();
        var latestDecisions = currentRevisionIds.Length == 0 ? [] : await db.PublicDocumentDecisions.AsNoTracking()
            .Where(d => d.AccountId == accountId && currentRevisionIds.Contains(d.DocumentRevisionId))
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .ToListAsync(cancellationToken);
        var decisionByCurrentRevision = latestDecisions
            .GroupBy(d => d.DocumentRevisionId)
            .ToDictionary(g => g.Key, g => g.First());

        var workOrders = await db.WorkOrders.AsNoTracking()
            .Where(w => w.AccountId == accountId && w.SourceDocumentId != null && docIds.Contains(w.SourceDocumentId.Value) && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new { w.Id, SourceDocumentId = w.SourceDocumentId!.Value, w.SourceRevisionId, w.Number, w.TotalSnapshot, w.CreatedAt, w.Status })
            .ToListAsync(cancellationToken);

        var workOrdersById = workOrders.ToDictionary(x => x.Id);
        var balanceMap = await balances.GetForDocumentsAsync(accountId, docIds, null, cancellationToken);
        var workOrderIds = workOrders.Select(x => x.Id).ToArray();
        var payments = await db.ManualPayments.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == FinancialRecordStatus.Active &&
                ((x.DocumentId != null && docIds.Contains(x.DocumentId.Value)) ||
                 (x.WorkOrderId != null && workOrderIds.Contains(x.WorkOrderId.Value))))
            .Select(x => new { x.Id, x.DocumentId, x.WorkOrderId, x.PaidAt, x.Status })
            .ToListAsync(cancellationToken);
        var paymentIds = payments.Select(x => x.Id).ToArray();
        var activeReceiptPaymentIds = paymentIds.Length == 0 ? [] : await db.Receipts.AsNoTracking()
            .Where(x => x.AccountId == accountId && paymentIds.Contains(x.PaymentId) && !x.IsDeleted && x.CancelledAt == null)
            .Select(x => new { x.Id, x.PaymentId, x.IssuedAt })
            .ToListAsync(cancellationToken);
        var receiptByPayment = activeReceiptPaymentIds
            .GroupBy(x => x.PaymentId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.IssuedAt).First());

        var allItems = new List<QuoteWorkspaceItem>(rows.Count);
        foreach (var row in rows)
        {
            var assigneeName = row.AssignedToUserId.HasValue && assignees.TryGetValue(row.AssignedToUserId.Value, out var name) ? name : null;
            currentRevisionByDocument.TryGetValue(row.Id, out var currentRev);
            var revisionNumber = currentRev?.VersionNumber ?? 0;
            balanceMap.TryGetValue(row.Id, out var balance);
            var wo = balance?.WorkOrderId is { } workOrderId && workOrdersById.TryGetValue(workOrderId, out var selectedWorkOrder)
                ? selectedWorkOrder
                : null;
            var relevantPayments = payments
                .Where(x => x.DocumentId == row.Id || (wo is not null && x.WorkOrderId == wo.Id))
                .OrderByDescending(x => x.PaidAt)
                .ToArray();
            var eligiblePayments = relevantPayments
                .Where(x => CommercialPaymentClassifier.Classify(x.DocumentId, x.WorkOrderId,
                    x.Status == FinancialRecordStatus.Reversed, row.Id, balance?.WorkOrderId).ParticipatesInBalance)
                .ToArray();
            var latestReceiptlessPaymentId = relevantPayments
                .Where(x => eligiblePayments.Any(p => p.Id == x.Id))
                .Where(x => !receiptByPayment.ContainsKey(x.Id))
                .Select(x => (Guid?)x.Id)
                .FirstOrDefault();
            var latestReceiptId = relevantPayments
                .Where(x => eligiblePayments.Any(p => p.Id == x.Id))
                .Where(x => receiptByPayment.ContainsKey(x.Id))
                .Select(x => (Guid?)receiptByPayment[x.Id].Id)
                .FirstOrDefault();
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
                CommercialNextActionResolver.ResolveQuoteAction(new CommercialNextActionContext(
                    row.Id,
                    row.Status,
                    currentRev is not null &&
                        decisionByCurrentRevision.TryGetValue(currentRev.Id, out var currentDecision) &&
                        currentDecision.Decision == PublicDocumentDecisionType.ChangeRequested,
                    Balance: balance,
                    WorkOrderId: wo?.Id,
                    WorkOrderStatus: wo?.Status.ToString(),
                    WorkOrderPaid: balance?.ReceivedAmount ?? 0m,
                    WorkOrderBalance: balance?.BalanceAmount ?? 0m,
                    LatestPaymentId: latestReceiptlessPaymentId,
                    LatestReceiptId: latestReceiptId)),
                revisionNumber,
                assigneeName,
                balance?.ReceivedAmount ?? 0m,
                balance?.BalanceAmount ?? 0m,
                wo?.Id,
                wo?.Number,
                balance?.ReversedAmount ?? 0m,
                balance?.OverpaidAmount ?? 0m,
                balance?.ContractSource ?? "Document",
                balance is null ? [] : balance.Warnings.Concat(balance.BlockingWarnings ?? []).ToArray()));
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
        var total = needsFinancialMaterialization ? materialized.Length : totalBeforeFinancialFilters;
        var items = needsFinancialMaterialization
            ? materialized.Skip((page - 1) * pageSize).Take(pageSize).ToArray()
            : materialized;

        return OperationResult<PagedResult<QuoteWorkspaceItem>>.Success(new(items, total, page, pageSize));
    }

    private static IQueryable<DocumentListRow> SelectRows(IQueryable<OrcaFacil.Domain.Entities.Document> query) =>
        query.Select(document => new DocumentListRow(document.Id, document.Number, document.Status, document.ClientName,
            document.Total, document.IssueDate, document.ValidUntil, document.CreatedAt, document.AssignedToUserId));

    private static IQueryable<OrcaFacil.Domain.Entities.Document> ApplyDocumentSort(
        IQueryable<OrcaFacil.Domain.Entities.Document> query, string sort) => sort switch
    {
        "oldest" => query.OrderBy(document => document.CreatedAt).ThenBy(document => document.Id),
        "validity" => query.OrderBy(document => document.ValidUntil ?? DateTime.MaxValue).ThenByDescending(document => document.CreatedAt).ThenBy(document => document.Id),
        _ => query.OrderByDescending(document => document.CreatedAt).ThenBy(document => document.Id)
    };

    private sealed record DocumentListRow(Guid Id, string Number, string Status, string ClientName, decimal Total,
        DateTime IssueDate, DateTime? ValidUntil, DateTime CreatedAt, Guid? AssignedToUserId);
}
