using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

/// <summary>
/// Loads the financial graph of one account (or a capped slice of the platform) and delegates the
/// classification to <see cref="FinancialIntegrityAnalyzer"/>. Read-only: it never changes data.
/// </summary>
public sealed class FinancialIntegrityDiagnosticService(OrcaFacilDbContext db) : IFinancialIntegrityDiagnosticService
{
    public Task<FinancialIntegrityReport> ScanAccountAsync(Guid accountId, CancellationToken ct = default) =>
        ScanAsync(accountId, int.MaxValue, ct);

    public Task<FinancialIntegrityReport> ScanAllAsync(int maxPayments = 5000, CancellationToken ct = default) =>
        ScanAsync(null, Math.Max(1, maxPayments), ct);

    private async Task<FinancialIntegrityReport> ScanAsync(Guid? accountId, int maxPayments, CancellationToken ct)
    {
        var paymentQuery = db.ManualPayments.AsNoTracking().Where(x => !x.IsDeleted);
        var receiptQuery = db.Receipts.AsNoTracking().Where(x => !x.IsDeleted);
        if (accountId is { } scopedAccountId)
        {
            paymentQuery = paymentQuery.Where(x => x.AccountId == scopedAccountId);
            receiptQuery = receiptQuery.Where(x => x.AccountId == scopedAccountId);
        }
        else
        {
            paymentQuery = paymentQuery.OrderByDescending(x => x.PaidAt).Take(maxPayments);
            receiptQuery = receiptQuery.OrderByDescending(x => x.IssuedAt).Take(maxPayments);
        }

        var payments = await paymentQuery
            .Select(x => new FinancialPaymentRow(x.Id, x.AccountId, x.ClientId, x.Amount,
                x.Status == FinancialRecordStatus.Reversed, x.DocumentId, x.WorkOrderId, x.PaidAt))
            .ToListAsync(ct);

        var receipts = await receiptQuery
            .Select(x => new FinancialReceiptRow(x.Id, x.AccountId, x.Amount, x.PaymentId, x.Number,
                x.CancelledAt != null, false))
            .ToListAsync(ct);

        if (payments.Count == 0 && receipts.Count == 0)
            return new FinancialIntegrityReport([], 0, DateTime.UtcNow);

        var workOrderIds = payments.Where(x => x.WorkOrderId.HasValue).Select(x => x.WorkOrderId!.Value).Distinct().ToArray();
        var workOrderQuery = db.WorkOrders.AsNoTracking().Where(x => !x.IsDeleted);
        workOrderQuery = accountId is { } workOrderAccountId
            ? workOrderQuery.Where(x => x.AccountId == workOrderAccountId)
            : workOrderQuery.Where(x => workOrderIds.Contains(x.Id));

        var workOrders = await workOrderQuery
            .Select(x => new FinancialWorkOrderRow(x.Id, x.AccountId, x.ClientId, x.SourceDocumentId, x.Number,
                x.Status == WorkOrderStatus.Cancelled, false))
            .ToListAsync(ct);

        var documentIds = payments.Where(x => x.DocumentId.HasValue).Select(x => x.DocumentId!.Value)
            .Concat(workOrders.Where(x => x.SourceDocumentId.HasValue).Select(x => x.SourceDocumentId!.Value))
            .Distinct()
            .ToArray();

        var documents = await db.Documents.AsNoTracking()
            .Where(x => documentIds.Contains(x.Id))
            .Select(x => new FinancialDocumentRow(x.Id, x.AccountId ?? Guid.Empty, x.ClientId, x.Number, x.Total, x.IsDeleted))
            .ToListAsync(ct);

        var accountIds = payments.Select(x => x.AccountId)
            .Concat(receipts.Select(x => x.AccountId))
            .Concat(workOrders.Select(x => x.AccountId))
            .Distinct()
            .ToArray();
        var accountNames = await db.BusinessAccounts.AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .Select(x => new { x.Id, x.DisplayName })
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);

        var findings = FinancialIntegrityAnalyzer.Analyze(payments, documents, workOrders, receipts, accountNames);
        var truncated = accountId is null && (payments.Count == maxPayments || receipts.Count == maxPayments);
        return new FinancialIntegrityReport(findings, accountIds.Length, DateTime.UtcNow, truncated);
    }
}
