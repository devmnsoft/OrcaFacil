using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class ReceiptApplicationService(
    OrcaFacilDbContext db,
    ICurrentAccountService currentAccount,
    INumberToWordsService numberToWords,
    IManualPaymentRegistrationService payments) : IReceiptApplicationService
{
    private const string RedirectPage = "/Receipts/Details";

    public async Task<CreateReceiptResult> CreateAsync(CreateReceiptRequest request, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        if (currentAccount.AccountId is not Guid accountId || request.AccountId != accountId)
            return Failure(CreateReceiptCode.AccessDenied, "A conta ativa não permite esta operação.", correlationId);
        await currentAccount.EnsureAccountAccessAsync(ct);

        var normalizedAmount = CommercialCalculator.Round(request.Amount);
        if (normalizedAmount <= 0) return Failure(CreateReceiptCode.InvalidAmount, "Informe um valor maior que zero.", correlationId);
        if (!PaymentMethodCodes.TryParse(request.PaymentMethod, out var paymentMethod))
            return Failure(CreateReceiptCode.InvalidPaymentMethod, "Escolha uma forma de pagamento válida.", correlationId);
        var canonicalPaymentMethod = paymentMethod.ToCode();

        if (string.IsNullOrWhiteSpace(request.ServiceDescription))
            return Failure(CreateReceiptCode.InvalidOrigin, "Descreva o serviço para emissão do recibo.", correlationId);

        var paidAtUtc = CommercialClock.NormalizeToUtc(request.PaidAt);
        if (paidAtUtc > DateTime.UtcNow.AddMinutes(5))
            return Failure(CreateReceiptCode.InvalidDate, "A data do recebimento não pode estar no futuro.", correlationId);

        // Validação estrita de combinações de origem
        if (request.OriginType == ReceiptOriginType.WorkOrder)
        {
            if (request.WorkOrderId is not Guid)
                return Failure(CreateReceiptCode.WorkOrderNotFound, "Ordem de serviço não informada.", correlationId);
            if (request.DocumentId is not null)
                return Failure(CreateReceiptCode.InvalidOrigin, "Origem 'Ordem de serviço' não permite vincular orçamento avulso.", correlationId);
        }
        else if (request.OriginType == ReceiptOriginType.Budget)
        {
            if (request.DocumentId is not Guid)
                return Failure(CreateReceiptCode.DocumentNotFound, "Orçamento não informado.", correlationId);
            if (request.WorkOrderId is not null)
                return Failure(CreateReceiptCode.InvalidOrigin, "Origem 'Orçamento' não permite vincular ordem de serviço.", correlationId);
        }
        else if (request.OriginType == ReceiptOriginType.Standalone)
        {
            if (request.WorkOrderId is not null || request.DocumentId is not null)
                return Failure(CreateReceiptCode.InvalidOrigin, "Recibo avulso não deve conter vínculo com orçamento ou ordem de serviço.", correlationId);
        }
        else
        {
            return Failure(CreateReceiptCode.InvalidOrigin, "Selecione uma origem válida.", correlationId);
        }

        // Validação de idempotência abrangente
        var duplicate = await db.ManualPayments.AsNoTracking().FirstOrDefaultAsync(
            x => x.AccountId == accountId && x.IdempotencyKey == request.IdempotencyKey, ct);
        if (duplicate is not null)
        {
            var same = ManualPaymentIdempotency.Matches(
                request.ClientId, request.DocumentId, request.WorkOrderId,
                normalizedAmount, canonicalPaymentMethod, paidAtUtc,
                duplicate.ClientId, duplicate.DocumentId, duplicate.WorkOrderId,
                duplicate.Amount, duplicate.PaymentMethod, duplicate.PaidAt);

            if (!same)
            {
                return Failure(CreateReceiptCode.ConcurrencyConflict,
                    "Esta chave já foi usada para outro recebimento com dados diferentes. O lançamento original foi mantido.", correlationId);
            }

            var existingReceipt = await db.Receipts.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentId == duplicate.Id && !x.IsDeleted, ct);
            if (existingReceipt is null && duplicate.Status == FinancialRecordStatus.Active)
                return await CreateForPaymentAsync(duplicate.Id, request.ServiceDescription, request.City, request.Notes, ct);

            return new(true, CreateReceiptCode.DuplicateRequest, "Este recebimento já havia sido registrado.", duplicate.Id,
                existingReceipt?.Id, existingReceipt?.Number, RedirectPage, correlationId);
        }

        var client = await db.Clients.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.ClientId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (client is null) return Failure(CreateReceiptCode.ClientNotFound, "Cliente não encontrado nesta conta.", correlationId);

        if (request.OriginType == ReceiptOriginType.WorkOrder)
        {
            var workOrderId = request.WorkOrderId!.Value;
            var order = await db.WorkOrders.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == workOrderId && x.AccountId == accountId && !x.IsDeleted, ct);
            if (order is null)
                return Failure(CreateReceiptCode.WorkOrderNotFound, "Ordem de serviço não encontrada nesta conta.", correlationId);

            if (order.ClientId != Guid.Empty && order.ClientId != request.ClientId)
                return Failure(CreateReceiptCode.InvalidOrigin, "O cliente informado não corresponde ao cliente da ordem de serviço.", correlationId);

            var registered = await payments.RegisterAsync(new ManualPaymentRequest(
                workOrderId, normalizedAmount, canonicalPaymentMethod, request.PaidAt, request.Notes, request.IdempotencyKey), ct);
            if (!registered.Succeeded || registered.EntityId is not Guid paymentId)
                return Failure(registered.Code == "IdempotencyConflict" ? CreateReceiptCode.ConcurrencyConflict : CreateReceiptCode.InvalidAmount, registered.Message, correlationId);

            return await CreateForPaymentAsync(paymentId, request.ServiceDescription, request.City, request.Notes, ct);
        }

        // Para orçamentos e recibos avulsos: verificação transacional com lock/serialização e retry limitado
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var duplicateInAttempt = await db.ManualPayments.AsNoTracking().FirstOrDefaultAsync(
                    x => x.AccountId == accountId && x.IdempotencyKey == request.IdempotencyKey, ct);
                if (duplicateInAttempt is not null)
                {
                    var existingReceipt = await db.Receipts.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentId == duplicateInAttempt.Id && !x.IsDeleted, ct);
                    if (existingReceipt is null && duplicateInAttempt.Status == FinancialRecordStatus.Active)
                        return await CreateForPaymentAsync(duplicateInAttempt.Id, request.ServiceDescription, request.City, request.Notes, ct);

                    return new(true, CreateReceiptCode.DuplicateRequest, "Este recebimento já havia sido registrado.", duplicateInAttempt.Id,
                        existingReceipt?.Id, existingReceipt?.Number, RedirectPage, correlationId);
                }

                var isolation = db.Database.IsRelational() ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted;
                await using var transaction = await db.Database.BeginTransactionAsync(isolation, ct);

                if (request.OriginType == ReceiptOriginType.Budget)
                {
                    var documentId = request.DocumentId!.Value;
                    var budgetDoc = await db.Documents.SingleOrDefaultAsync(
                        x => x.Id == documentId && x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget, ct);
                    if (budgetDoc is null)
                        return Failure(CreateReceiptCode.DocumentNotFound, "Orçamento não encontrado nesta conta.", correlationId);

                    if (budgetDoc.Status is "Cancelled" or "Rejected")
                        return Failure(CreateReceiptCode.InvalidOrigin, "Não é permitido registrar recebimento para orçamento cancelado ou recusado.", correlationId);

                    if (budgetDoc.ClientId.HasValue && budgetDoc.ClientId != request.ClientId)
                        return Failure(CreateReceiptCode.InvalidOrigin, "O cliente informado não corresponde ao cliente do orçamento.", correlationId);

                    var alreadyPaid = await db.ManualPayments.Where(
                        x => x.AccountId == accountId && x.DocumentId == documentId && !x.IsDeleted && x.Status == FinancialRecordStatus.Active)
                        .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

                    var balance = CommercialCalculator.Round(budgetDoc.Total - alreadyPaid);
                    if (balance < 0m) balance = 0m;
                    if (balance == 0m)
                        return Failure(CreateReceiptCode.InvalidAmount, "Este orçamento já está totalmente quitado.", correlationId);
                    if (normalizedAmount > balance)
                        return Failure(CreateReceiptCode.InvalidAmount, $"O valor informado supera o saldo restante de {balance:C} do orçamento.", correlationId);
                }

                var payment = new ManualPayment
                {
                    AccountId = accountId,
                    ClientId = client.Id,
                    WorkOrderId = request.WorkOrderId,
                    DocumentId = request.DocumentId,
                    Amount = normalizedAmount,
                    PaymentMethod = canonicalPaymentMethod,
                    PaidAt = paidAtUtc,
                    Notes = request.Notes?.Trim(),
                    RegisteredByUserId = currentAccount.UserId,
                    IdempotencyKey = request.IdempotencyKey
                };
                db.ManualPayments.Add(payment);

                var number = await ReceiptNumberAllocator.NextAsync(db, accountId, ct);
                var receipt = new Receipt
                {
                    AccountId = accountId,
                    PaymentId = payment.Id,
                    ClientId = client.Id,
                    WorkOrderId = request.WorkOrderId,
                    DocumentId = request.DocumentId,
                    LegacyDocumentId = request.LegacyDocumentId,
                    OriginType = request.OriginType,
                    Number = number,
                    Amount = normalizedAmount,
                    AmountInWords = numberToWords.ToCurrencyWords(normalizedAmount),
                    PaymentMethod = canonicalPaymentMethod,
                    IssuedAt = DateTime.UtcNow,
                    City = request.City?.Trim(),
                    Notes = request.Notes?.Trim(),
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
            catch (Exception ex) when (IsPersistenceConflict(ex) && attempt < maxRetries)
            {
                await Task.Delay(50 * attempt, ct);
            }
            catch (Exception ex) when (IsPersistenceConflict(ex))
            {
                var duplicateRecover = await db.ManualPayments.AsNoTracking().FirstOrDefaultAsync(
                    x => x.AccountId == accountId && x.IdempotencyKey == request.IdempotencyKey, ct);
                if (duplicateRecover is not null)
                {
                    var existingReceipt = await db.Receipts.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentId == duplicateRecover.Id && !x.IsDeleted, ct);
                    return new(true, CreateReceiptCode.DuplicateRequest, "Este recebimento já havia sido registrado.", duplicateRecover.Id,
                        existingReceipt?.Id, existingReceipt?.Number, RedirectPage, correlationId);
                }
                return Failure(CreateReceiptCode.ConcurrencyConflict, "Conflito de concorrência ao processar recebimento. Tente novamente.", correlationId);
            }
        }

        return Failure(CreateReceiptCode.ConcurrencyConflict, "Não foi possível concluir o recebimento devido a concorrência.", correlationId);
    }

    public async Task<CreateReceiptResult> CreateForPaymentAsync(Guid paymentId, string serviceDescription, string? city, string? notes, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        if (currentAccount.AccountId is not Guid accountId)
            return Failure(CreateReceiptCode.AccessDenied, "A conta ativa não permite esta operação.", correlationId);
        await currentAccount.EnsureAccountAccessAsync(ct);
        if (string.IsNullOrWhiteSpace(serviceDescription))
            return Failure(CreateReceiptCode.InvalidOrigin, "Descreva o serviço recebido.", correlationId);

        var payment = await db.ManualPayments.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == paymentId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (payment is null || payment.Status != FinancialRecordStatus.Active)
            return Failure(CreateReceiptCode.AccessDenied, "Pagamento ativo não encontrado nesta conta.", correlationId);

        var existing = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(
            x => x.AccountId == accountId && x.PaymentId == paymentId && !x.IsDeleted, ct);
        if (existing is not null)
            return new(true, CreateReceiptCode.DuplicateRequest, "Este pagamento já possui recibo.", payment.Id,
                existing.Id, existing.Number, RedirectPage, correlationId);

        var client = await db.Clients.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == payment.ClientId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (client is null) return Failure(CreateReceiptCode.ClientNotFound, "Cliente não encontrado nesta conta.", correlationId);

        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var existingInLoop = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(
                    x => x.AccountId == accountId && x.PaymentId == paymentId && !x.IsDeleted, ct);
                if (existingInLoop is not null)
                    return new(true, CreateReceiptCode.DuplicateRequest, "Este pagamento já possui recibo.", payment.Id,
                        existingInLoop.Id, existingInLoop.Number, RedirectPage, correlationId);

                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                var receipt = new Receipt
                {
                    AccountId = accountId,
                    PaymentId = payment.Id,
                    ClientId = payment.ClientId,
                    WorkOrderId = payment.WorkOrderId,
                    DocumentId = payment.DocumentId,
                    OriginType = payment.WorkOrderId.HasValue ? ReceiptOriginType.WorkOrder : payment.DocumentId.HasValue ? ReceiptOriginType.Budget : ReceiptOriginType.Standalone,
                    Number = await ReceiptNumberAllocator.NextAsync(db, accountId, ct),
                    Amount = payment.Amount,
                    AmountInWords = numberToWords.ToCurrencyWords(payment.Amount),
                    PaymentMethod = payment.PaymentMethod,
                    IssuedAt = DateTime.UtcNow,
                    City = city?.Trim(),
                    Notes = notes?.Trim(),
                    ServiceDescription = serviceDescription.Trim(),
                    ClientSnapshot = JsonSerializer.Serialize(new { client.Id, client.Name, client.DocumentNumber, client.Email, client.Phone, client.City }),
                    ServiceSnapshot = JsonSerializer.Serialize(new { description = serviceDescription.Trim() })
                };
                db.Receipts.Add(receipt);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return new(true, CreateReceiptCode.None, "Recibo emitido com sucesso.", payment.Id, receipt.Id,
                    receipt.Number, RedirectPage, correlationId);
            }
            catch (Exception ex) when (IsPersistenceConflict(ex) && attempt < maxRetries)
            {
                await Task.Delay(50 * attempt, ct);
            }
            catch (Exception ex) when (IsPersistenceConflict(ex))
            {
                var existingFinal = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(
                    x => x.AccountId == accountId && x.PaymentId == paymentId && !x.IsDeleted, ct);
                if (existingFinal is not null)
                    return new(true, CreateReceiptCode.DuplicateRequest, "Este pagamento já possui recibo.", payment.Id,
                        existingFinal.Id, existingFinal.Number, RedirectPage, correlationId);

                return Failure(CreateReceiptCode.ConcurrencyConflict, "Conflito ao emitir recibo para o pagamento.", correlationId);
            }
        }

        return Failure(CreateReceiptCode.ConcurrencyConflict, "Não foi possível concluir a emissão do recibo.", correlationId);
    }

    public async Task<bool> CancelAsync(Guid receiptId, string reason, CancellationToken ct = default)
    {
        if (currentAccount.AccountId is not Guid accountId || string.IsNullOrWhiteSpace(reason)) return false;
        var receipt = await db.Receipts.SingleOrDefaultAsync(x => x.Id == receiptId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (receipt is null || receipt.CancelledAt.HasValue) return false;
        receipt.CancelledAt = DateTime.UtcNow;
        receipt.CancelledByUserId = currentAccount.UserId;
        receipt.CancellationReason = reason.Trim();
        receipt.Touch();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MarkSharedAsync(Guid receiptId, CancellationToken ct = default)
    {
        if (currentAccount.AccountId is not Guid accountId) return false;
        var receipt = await db.Receipts.SingleOrDefaultAsync(x => x.Id == receiptId && x.AccountId == accountId && !x.IsDeleted && x.CancelledAt == null, ct);
        if (receipt is null) return false;
        receipt.LastSharedAt = DateTime.UtcNow;
        receipt.SentAt ??= receipt.LastSharedAt;
        receipt.Touch();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReversePaymentAsync(Guid paymentId, string reason, CancellationToken ct = default)
    {
        if (currentAccount.AccountId is not Guid accountId || string.IsNullOrWhiteSpace(reason)) return false;
        await currentAccount.EnsureAccountAccessAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var payment = await db.ManualPayments.SingleOrDefaultAsync(x => x.Id == paymentId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (payment is null || payment.Status == FinancialRecordStatus.Reversed) return false;
        payment.Status = FinancialRecordStatus.Reversed;
        payment.ReversedAt = DateTime.UtcNow;
        payment.ReversedByUserId = currentAccount.UserId;
        payment.ReversalReason = reason.Trim();
        payment.Touch();

        var receipts = await db.Receipts.Where(x => x.AccountId == accountId && x.PaymentId == payment.Id && !x.IsDeleted && x.CancelledAt == null).ToListAsync(ct);
        foreach (var receipt in receipts)
            receipt.CancelForReversedPayment(currentAccount.UserId, DateTime.UtcNow);

        if (payment.WorkOrderId is Guid workOrderId)
        {
            var order = await db.WorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId && x.AccountId == accountId && !x.IsDeleted, ct);
            if (order is not null)
            {
                var remainingActive = await db.ManualPayments.Where(x => x.AccountId == accountId && x.WorkOrderId == workOrderId &&
                    x.Id != payment.Id && !x.IsDeleted && x.Status == FinancialRecordStatus.Active).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
                order.PaymentReceived = remainingActive >= order.TotalSnapshot;
            }
        }

        db.ActivityEvents.Add(new ActivityEvent
        {
            AccountId = accountId,
            ActorUserId = currentAccount.UserId,
            Action = "PaymentReversed",
            EntityType = "ManualPayment",
            EntityId = payment.Id,
            Summary = "Estorno lógico de pagamento registrado."
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private static CreateReceiptResult Failure(CreateReceiptCode code, string message, string correlationId) =>
        new(false, code, message, null, null, null, RedirectPage, correlationId);

    private static bool IsPersistenceConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var name = current.GetType().Name;
            if (name is "DbUpdateConcurrencyException") return true;
            var state = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
            if (state is "23505" or "40001") return true;
        }
        return false;
    }
}
