using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class ReceiptApplicationService(
    OrcaFacilDbContext db,
    ICurrentAccountService currentAccount,
    INumberToWordsService numberToWords) : IReceiptApplicationService
{
    private const string RedirectPage = "/Receipts/Details";

    public async Task<CreateReceiptResult> CreateAsync(CreateReceiptRequest request, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        if (currentAccount.AccountId is not Guid accountId || request.AccountId != accountId)
            return Failure(CreateReceiptCode.AccessDenied, "A conta ativa não permite esta operação.", correlationId);
        if (request.Amount <= 0) return Failure(CreateReceiptCode.InvalidAmount, "Informe um valor maior que zero.", correlationId);
        if (request.PaidAt > DateTime.UtcNow.AddDays(1)) return Failure(CreateReceiptCode.InvalidDate, "A data do recebimento não pode estar no futuro.", correlationId);
        if (string.IsNullOrWhiteSpace(request.PaymentMethod)) return Failure(CreateReceiptCode.InvalidPaymentMethod, "Escolha a forma de pagamento.", correlationId);

        var duplicate = await db.ManualPayments.AsNoTracking().FirstOrDefaultAsync(
            x => x.AccountId == accountId && x.IdempotencyKey == request.IdempotencyKey, ct);
        if (duplicate is not null)
        {
            var existingReceipt = await db.Receipts.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentId == duplicate.Id, ct);
            return new(true, CreateReceiptCode.DuplicateRequest, "Este recebimento já havia sido registrado.", duplicate.Id,
                existingReceipt?.Id, existingReceipt?.Number, RedirectPage, correlationId);
        }

        var client = await db.Clients.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.ClientId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (client is null) return Failure(CreateReceiptCode.ClientNotFound, "Cliente não encontrado nesta conta.", correlationId);

        if (request.OriginType == ReceiptOriginType.WorkOrder && (request.WorkOrderId is not Guid workOrderId ||
            !await db.WorkOrders.AnyAsync(x => x.Id == workOrderId && x.AccountId == accountId && !x.IsDeleted, ct)))
            return Failure(CreateReceiptCode.WorkOrderNotFound, "Ordem de serviço não encontrada nesta conta.", correlationId);
        if (request.OriginType == ReceiptOriginType.Budget && (request.DocumentId is not Guid documentId ||
            !await db.Documents.AnyAsync(x => x.Id == documentId && x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget, ct)))
            return Failure(CreateReceiptCode.DocumentNotFound, "Orçamento não encontrado nesta conta.", correlationId);
        if (!Enum.IsDefined(request.OriginType)) return Failure(CreateReceiptCode.InvalidOrigin, "Selecione uma origem válida.", correlationId);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var payment = new ManualPayment
        {
            AccountId = accountId, ClientId = client.Id, WorkOrderId = request.WorkOrderId,
            DocumentId = request.DocumentId, Amount = request.Amount,
            PaymentMethod = request.PaymentMethod.Trim(), PaidAt = request.PaidAt.ToUniversalTime(),
            Notes = request.Notes?.Trim(), RegisteredByUserId = currentAccount.UserId,
            IdempotencyKey = request.IdempotencyKey
        };
        db.ManualPayments.Add(payment);

        var sequence = await db.Receipts.CountAsync(x => x.AccountId == accountId, ct) + 1;
        var receipt = new Receipt
        {
            AccountId = accountId, PaymentId = payment.Id, ClientId = client.Id,
            WorkOrderId = request.WorkOrderId, DocumentId = request.DocumentId,
            LegacyDocumentId = request.LegacyDocumentId, OriginType = request.OriginType,
            Number = $"REC-{DateTime.UtcNow:yyyy}-{sequence:00000}", Amount = request.Amount,
            AmountInWords = numberToWords.ToCurrencyWords(request.Amount), PaymentMethod = request.PaymentMethod.Trim(),
            IssuedAt = DateTime.UtcNow, City = request.City?.Trim(), Notes = request.Notes?.Trim(),
            ServiceDescription = request.ServiceDescription.Trim(),
            ClientSnapshot = JsonSerializer.Serialize(new { client.Id, client.Name, client.DocumentNumber, client.Email, client.Phone, client.City }),
            ServiceSnapshot = JsonSerializer.Serialize(new { description = request.ServiceDescription.Trim() })
        };
        db.Receipts.Add(receipt);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, CreateReceiptCode.None, "Recibo emitido com sucesso.", payment.Id, receipt.Id,
            receipt.Number, RedirectPage, correlationId);
    }

    public async Task<bool> CancelAsync(Guid receiptId, string reason, CancellationToken ct = default)
    {
        if (currentAccount.AccountId is not Guid accountId || string.IsNullOrWhiteSpace(reason)) return false;
        var receipt = await db.Receipts.SingleOrDefaultAsync(x => x.Id == receiptId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (receipt is null || receipt.CancelledAt.HasValue) return false;
        receipt.CancelledAt = DateTime.UtcNow; receipt.CancelledByUserId = currentAccount.UserId; receipt.CancellationReason = reason.Trim(); receipt.Touch();
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> MarkSharedAsync(Guid receiptId, CancellationToken ct = default)
    {
        if (currentAccount.AccountId is not Guid accountId) return false;
        var receipt = await db.Receipts.SingleOrDefaultAsync(x => x.Id == receiptId && x.AccountId == accountId && !x.IsDeleted && x.CancelledAt == null, ct);
        if (receipt is null) return false;
        receipt.LastSharedAt = DateTime.UtcNow; receipt.SentAt ??= receipt.LastSharedAt; receipt.Touch(); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> ReversePaymentAsync(Guid paymentId, string reason, CancellationToken ct = default)
    {
        if (currentAccount.AccountId is not Guid accountId || string.IsNullOrWhiteSpace(reason)) return false;
        var payment = await db.ManualPayments.SingleOrDefaultAsync(x => x.Id == paymentId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (payment is null || payment.Status == FinancialRecordStatus.Reversed) return false;
        payment.Status = FinancialRecordStatus.Reversed; payment.ReversedAt = DateTime.UtcNow;
        payment.ReversedByUserId = currentAccount.UserId; payment.ReversalReason = reason.Trim(); payment.Touch();
        await db.SaveChangesAsync(ct); return true;
    }

    private static CreateReceiptResult Failure(CreateReceiptCode code, string message, string correlationId) =>
        new(false, code, message, null, null, null, RedirectPage, correlationId);
}
