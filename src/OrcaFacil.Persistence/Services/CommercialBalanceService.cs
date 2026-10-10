using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class CommercialBalanceService(OrcaFacilDbContext db) : ICommercialBalanceService
{
    public async Task<CommercialBalance?> GetForWorkOrderAsync(Guid accountId, Guid workOrderId, CancellationToken ct = default)
    {
        var order = await db.WorkOrders.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == workOrderId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (order is null) return null;

        if (order.SourceDocumentId is { } sourceDocumentId)
        {
            var balances = await GetForDocumentsAsync(accountId, [sourceDocumentId], null, ct);
            if (balances.TryGetValue(sourceDocumentId, out var selected) && selected.WorkOrderId == order.Id)
                return selected;
        }

        var payments = await db.ManualPayments.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.WorkOrderId == order.Id)
            .ToListAsync(ct);

        var warnings = new List<string>();
        if (order.SourceDocumentId is { } documentId)
        {
            warnings.Add("Esta ordem não é a origem contratual selecionada para o orçamento; recebimentos diretos do orçamento não compõem este saldo.");

            if (await db.ManualPayments.AsNoTracking().AnyAsync(
                    x => x.AccountId == accountId && !x.IsDeleted && x.DocumentId == documentId &&
                         x.WorkOrderId == null, ct))
                warnings.Add("Há recebimentos diretos no orçamento vinculados somente à origem contratual selecionada.");
        }

        return Build(order.SourceDocumentId, order.Id, "WorkOrder", order.TotalSnapshot, payments, warnings);
    }

    public async Task<CommercialBalance?> GetForDocumentAsync(Guid accountId, Guid documentId, Guid? preferredWorkOrderId = null, CancellationToken ct = default)
    {
        var document = await db.Documents.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == documentId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (document is null) return null;

        var preferred = preferredWorkOrderId.HasValue
            ? new Dictionary<Guid, Guid> { [documentId] = preferredWorkOrderId.Value }
            : null;
        var balances = await GetForDocumentsAsync(accountId, [documentId], preferred, ct);
        return balances.TryGetValue(documentId, out var balance) ? balance : null;
    }

    public async Task<IReadOnlyDictionary<Guid, CommercialBalance>> GetForDocumentsAsync(Guid accountId,
        IReadOnlyCollection<Guid> documentIds, IReadOnlyDictionary<Guid, Guid>? preferredWorkOrderIds = null, CancellationToken ct = default)
    {
        var ids = documentIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, CommercialBalance>();

        var documents = await db.Documents.AsNoTracking()
            .Where(x => x.AccountId == accountId && ids.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new DocumentBalanceRow(x.Id, x.Total))
            .ToListAsync(ct);
        if (documents.Count == 0) return new Dictionary<Guid, CommercialBalance>();

        var existingDocumentIds = documents.Select(x => x.Id).ToArray();

        var revisions = await db.DocumentRevisions.AsNoTracking()
            .Where(x => x.AccountId == accountId && existingDocumentIds.Contains(x.DocumentId) && x.IsCurrent)
            .Select(x => new RevisionBalanceRow(x.Id, x.DocumentId, x.VersionNumber, x.Total))
            .ToListAsync(ct);
        var revisionsByDocument = revisions
            .GroupBy(x => x.DocumentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.VersionNumber).First());

        var orders = await db.WorkOrders.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.SourceDocumentId != null && existingDocumentIds.Contains(x.SourceDocumentId.Value) && !x.IsDeleted)
            .Select(x => new WorkOrderBalanceRow(x.Id, x.SourceDocumentId!.Value, x.SourceRevisionId, x.Number, x.TotalSnapshot, x.CreatedAt))
            .ToListAsync(ct);

        var ordersByDocument = orders.GroupBy(x => x.SourceDocumentId).ToDictionary(g => g.Key, g => g.ToArray());
        var selectedOrders = new Dictionary<Guid, WorkOrderBalanceRow>();
        foreach (var document in documents)
        {
            if (!ordersByDocument.TryGetValue(document.Id, out var documentOrders)) continue;

            WorkOrderBalanceRow? selected = null;
            if (preferredWorkOrderIds is not null &&
                preferredWorkOrderIds.TryGetValue(document.Id, out var preferredId))
                selected = documentOrders.FirstOrDefault(x => x.Id == preferredId);

            if (selected is null &&
                revisionsByDocument.TryGetValue(document.Id, out var revision))
                selected = documentOrders.FirstOrDefault(x => x.SourceRevisionId == revision.Id);

            selected ??= documentOrders
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .First();
            selectedOrders[document.Id] = selected;
        }

        var relatedOrderIds = orders.Select(x => x.Id).Distinct().ToArray();
        var paymentsQuery = db.ManualPayments.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        var payments = relatedOrderIds.Length == 0
            ? await paymentsQuery.Where(x => x.DocumentId != null && existingDocumentIds.Contains(x.DocumentId.Value) && x.WorkOrderId == null).ToListAsync(ct)
            : await paymentsQuery.Where(x =>
                    (x.DocumentId != null && existingDocumentIds.Contains(x.DocumentId.Value)) ||
                    (x.WorkOrderId != null && relatedOrderIds.Contains(x.WorkOrderId.Value)))
                .ToListAsync(ct);

        var result = new Dictionary<Guid, CommercialBalance>();
        foreach (var document in documents)
        {
            revisionsByDocument.TryGetValue(document.Id, out var revision);
            selectedOrders.TryGetValue(document.Id, out var order);
            ordersByDocument.TryGetValue(document.Id, out var documentOrders);

            var warnings = BuildWarnings(document.Id, order?.Id, documentOrders ?? [], payments);
            var scopedPayments = payments.Where(x => BelongsToSelectedContract(x, document.Id, order?.Id)).ToArray();

            result[document.Id] = order is not null
                ? Build(document.Id, order.Id, "WorkOrder", order.TotalSnapshot, scopedPayments, warnings)
                : Build(document.Id, null, revision is null ? "Document" : "CurrentRevision",
                    revision?.Total ?? document.Total, scopedPayments, warnings);
        }

        return result;
    }

    private static bool BelongsToSelectedContract(ManualPayment payment, Guid documentId, Guid? selectedWorkOrderId)
    {
        if (selectedWorkOrderId.HasValue && payment.WorkOrderId == selectedWorkOrderId.Value) return true;
        return payment.DocumentId == documentId && payment.WorkOrderId is null;
    }

    private static IReadOnlyList<string> BuildWarnings(Guid documentId, Guid? selectedWorkOrderId,
        IReadOnlyCollection<WorkOrderBalanceRow> documentOrders, IReadOnlyCollection<ManualPayment> payments)
    {
        var warnings = new List<string>();
        if (documentOrders.Count > 1)
            warnings.Add("Há mais de uma ordem vinculada a este orçamento; a origem contratual foi selecionada de forma determinística.");

        if (selectedWorkOrderId.HasValue && payments.Any(x => x.DocumentId == documentId && x.WorkOrderId is null))
            warnings.Add("Recebimentos diretos do orçamento foram considerados na origem contratual selecionada.");

        if (selectedWorkOrderId.HasValue && payments.Any(x => x.DocumentId == documentId && x.WorkOrderId != null && x.WorkOrderId != selectedWorkOrderId))
            warnings.Add("Há recebimentos de outra ordem do mesmo orçamento; eles não compõem este saldo.");

        if (selectedWorkOrderId.HasValue && payments.Any(x => x.WorkOrderId == selectedWorkOrderId && x.DocumentId != null && x.DocumentId != documentId))
            warnings.Add("Há recebimento com vínculo incompatível entre orçamento e ordem; revise o histórico financeiro.");

        return warnings;
    }

    private static CommercialBalance Build(Guid? documentId, Guid? workOrderId, string source, decimal contractedAmount,
        IReadOnlyCollection<ManualPayment> payments, IReadOnlyList<string> warnings)
        => CommercialBalanceCalculator.Calculate(documentId, workOrderId, source, contractedAmount,
            payments.Select(x => new CommercialPaymentAmount(x.Amount, x.Status == FinancialRecordStatus.Reversed)),
            warnings);

    private sealed record DocumentBalanceRow(Guid Id, decimal Total);
    private sealed record RevisionBalanceRow(Guid Id, Guid DocumentId, int VersionNumber, decimal Total);
    private sealed record WorkOrderBalanceRow(Guid Id, Guid SourceDocumentId, Guid? SourceRevisionId, string Number, decimal TotalSnapshot, DateTime CreatedAt);
}
