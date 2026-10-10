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

        var payments = await db.ManualPayments.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted &&
                (x.WorkOrderId == order.Id ||
                 (order.SourceDocumentId != null && x.DocumentId == order.SourceDocumentId && x.WorkOrderId == null)))
            .ToListAsync(ct);

        var warnings = new List<string>();
        if (order.SourceDocumentId is { } documentId)
        {
            var foreignOrderPayments = await db.ManualPayments.AsNoTracking().AnyAsync(
                x => x.AccountId == accountId && !x.IsDeleted && x.DocumentId == documentId &&
                     x.WorkOrderId != null && x.WorkOrderId != order.Id, ct);
            if (foreignOrderPayments)
                warnings.Add("Há pagamentos de outra ordem vinculados ao mesmo orçamento; eles não compõem este saldo.");
        }

        return Build(order.SourceDocumentId, order.Id, "WorkOrder", order.TotalSnapshot, payments, warnings);
    }

    public async Task<CommercialBalance?> GetForDocumentAsync(Guid accountId, Guid documentId, Guid? preferredWorkOrderId = null, CancellationToken ct = default)
    {
        var document = await db.Documents.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == documentId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (document is null) return null;

        WorkOrder? order = null;
        if (preferredWorkOrderId.HasValue)
        {
            order = await db.WorkOrders.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == preferredWorkOrderId && x.AccountId == accountId && x.SourceDocumentId == documentId && !x.IsDeleted, ct);
        }

        order ??= await db.WorkOrders.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.SourceDocumentId == documentId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (order is not null)
            return await GetForWorkOrderAsync(accountId, order.Id, ct);

        var revision = await db.DocumentRevisions.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.DocumentId == documentId && x.IsCurrent)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(ct);

        var payments = await db.ManualPayments.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.DocumentId == documentId && x.WorkOrderId == null)
            .ToListAsync(ct);

        var source = revision is null ? "Document" : "CurrentRevision";
        var contracted = revision?.Total ?? document.Total;
        return Build(documentId, null, source, contracted, payments, []);
    }

    private static CommercialBalance Build(Guid? documentId, Guid? workOrderId, string source, decimal contractedAmount,
        IReadOnlyCollection<ManualPayment> payments, IReadOnlyList<string> warnings)
        => CommercialBalanceCalculator.Calculate(documentId, workOrderId, source, contractedAmount,
            payments.Select(x => new CommercialPaymentAmount(x.Amount, x.Status == FinancialRecordStatus.Reversed)),
            warnings);
}
