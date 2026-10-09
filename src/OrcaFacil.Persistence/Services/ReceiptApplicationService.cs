using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
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

            var existingReceipt = await db.Receipts.AsNoTracking().FirstOrDefaultAsync(
                x => x.PaymentId == duplicate.Id && !x.IsDeleted && x.CancelledAt == null, ct);
            if (existingReceipt is null && duplicate.Status == FinancialRecordStatus.Active)
                return await CreateForPaymentAsync(duplicate.Id, request.ServiceDescription, request.City, request.Notes, ct);

            if (existingReceipt is not null)
            {
                return new(true, CreateReceiptCode.DuplicateRequest, "Este recebimento já havia sido registrado.", duplicate.Id,
                    existingReceipt.Id, existingReceipt.Number, RedirectPage, correlationId);
            }

            return Failure(CreateReceiptCode.AccessDenied, "O pagamento associado a esta chave não está ativo.", correlationId);
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

        // Para orçamentos e recibos avulsos: verificação transacional com serialização e retry limitado
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var isolation = db.Database.IsRelational() ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted;
            await using var transaction = await db.Database.BeginTransactionAsync(isolation, ct);
            ManualPayment? payment = null;
            Receipt? receipt = null;

            try
            {
                var duplicateInAttempt = await db.ManualPayments.AsNoTracking().FirstOrDefaultAsync(
                    x => x.AccountId == accountId && x.IdempotencyKey == request.IdempotencyKey, ct);
                if (duplicateInAttempt is not null)
                {
                    if (!IsSameRequest(request, normalizedAmount, canonicalPaymentMethod, paidAtUtc, duplicateInAttempt))
                    {
                        await transaction.RollbackAsync(ct);
                        return Failure(CreateReceiptCode.ConcurrencyConflict,
                            "Esta chave já foi usada para outro recebimento com dados diferentes. O lançamento original foi mantido.", correlationId);
                    }

                    var existingReceipt = await db.Receipts.AsNoTracking().FirstOrDefaultAsync(
                        x => x.PaymentId == duplicateInAttempt.Id && !x.IsDeleted && x.CancelledAt == null, ct);
                    if (existingReceipt is null && duplicateInAttempt.Status == FinancialRecordStatus.Active)
                    {
                        await transaction.RollbackAsync(ct);
                        return await CreateForPaymentAsync(duplicateInAttempt.Id, request.ServiceDescription, request.City, request.Notes, ct);
                    }

                    if (existingReceipt is not null)
                    {
                        await transaction.CommitAsync(ct);
                        return new(true, CreateReceiptCode.DuplicateRequest, "Este recebimento já havia sido registrado.", duplicateInAttempt.Id,
                            existingReceipt.Id, existingReceipt.Number, RedirectPage, correlationId);
                    }

                    await transaction.RollbackAsync(ct);
                    return Failure(CreateReceiptCode.AccessDenied, "O pagamento associado a esta chave não está ativo.", correlationId);
                }

                if (request.OriginType == ReceiptOriginType.Budget)
                {
                    var documentId = request.DocumentId!.Value;
                    var budgetDoc = await db.Documents.AsNoTracking().SingleOrDefaultAsync(
                        x => x.Id == documentId && x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget, ct);
                    if (budgetDoc is null)
                    {
                        await transaction.RollbackAsync(ct);
                        return Failure(CreateReceiptCode.DocumentNotFound, "Orçamento não encontrado nesta conta.", correlationId);
                    }

                    if (budgetDoc.Status is "Cancelled" or "Rejected")
                    {
                        await transaction.RollbackAsync(ct);
                        return Failure(CreateReceiptCode.InvalidOrigin, "Não é permitido registrar recebimento para orçamento cancelado ou recusado.", correlationId);
                    }

                    if (budgetDoc.ClientId.HasValue && budgetDoc.ClientId != request.ClientId)
                    {
                        await transaction.RollbackAsync(ct);
                        return Failure(CreateReceiptCode.InvalidOrigin, "O cliente informado não corresponde ao cliente do orçamento.", correlationId);
                    }

                    var alreadyPaid = await db.ManualPayments.Where(
                        x => x.AccountId == accountId && x.DocumentId == documentId && !x.IsDeleted && x.Status == FinancialRecordStatus.Active)
                        .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

                    var balance = CommercialCalculator.Round(budgetDoc.Total - alreadyPaid);
                    if (balance < 0m) balance = 0m;
                    if (balance == 0m)
                    {
                        await transaction.RollbackAsync(ct);
                        return Failure(CreateReceiptCode.InvalidAmount, "Este orçamento já está totalmente quitado.", correlationId);
                    }
                    if (normalizedAmount > balance)
                    {
                        await transaction.RollbackAsync(ct);
                        return Failure(CreateReceiptCode.InvalidAmount, $"O valor informado supera o saldo restante de {balance:C} do orçamento.", correlationId);
                    }
                }

                payment = new ManualPayment
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
                receipt = new Receipt
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
            catch (Exception ex) when (ShouldRetryPersistenceConflict(ex) && attempt < maxRetries)
            {
                await transaction.RollbackAsync(ct);
                DetachAttemptEntry(payment);
                DetachAttemptEntry(receipt);
                await Task.Delay(50 * attempt, ct);
            }
            catch (Exception ex) when (ShouldHandlePersistenceConflict(ex))
            {
                await transaction.RollbackAsync(ct);
                DetachAttemptEntry(payment);
                DetachAttemptEntry(receipt);
                var duplicateRecover = await db.ManualPayments.AsNoTracking().FirstOrDefaultAsync(
                    x => x.AccountId == accountId && x.IdempotencyKey == request.IdempotencyKey, ct);
                if (duplicateRecover is not null)
                {
                    if (!IsSameRequest(request, normalizedAmount, canonicalPaymentMethod, paidAtUtc, duplicateRecover))
                    {
                        return Failure(CreateReceiptCode.ConcurrencyConflict,
                            "Esta chave já foi usada para outro recebimento com dados diferentes. O lançamento original foi mantido.", correlationId);
                    }

                    var existingReceipt = await db.Receipts.AsNoTracking().FirstOrDefaultAsync(
                        x => x.PaymentId == duplicateRecover.Id && !x.IsDeleted && x.CancelledAt == null, ct);
                    if (existingReceipt is null && duplicateRecover.Status == FinancialRecordStatus.Active)
                        return await CreateForPaymentAsync(duplicateRecover.Id, request.ServiceDescription, request.City, request.Notes, ct);

                    if (existingReceipt is not null)
                    {
                        return new(true, CreateReceiptCode.DuplicateRequest, "Este recebimento já havia sido registrado.", duplicateRecover.Id,
                            existingReceipt.Id, existingReceipt.Number, RedirectPage, correlationId);
                    }

                    return Failure(CreateReceiptCode.AccessDenied, "O pagamento associado a esta chave não está ativo.", correlationId);
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

        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var isolation = db.Database.IsRelational() ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted;
            await using var transaction = await db.Database.BeginTransactionAsync(isolation, ct);
            Receipt? receipt = null;

            try
            {
                var activePayment = await db.ManualPayments.SingleOrDefaultAsync(
                    x => x.Id == paymentId && x.AccountId == accountId && !x.IsDeleted, ct);
                if (activePayment is null || activePayment.Status != FinancialRecordStatus.Active)
                {
                    await transaction.RollbackAsync(ct);
                    return Failure(CreateReceiptCode.AccessDenied, "Pagamento ativo não encontrado nesta conta.", correlationId);
                }

                var existingInLoop = await db.Receipts.FirstOrDefaultAsync(
                    x => x.AccountId == accountId && x.PaymentId == paymentId && !x.IsDeleted && x.CancelledAt == null, ct);
                if (existingInLoop is not null)
                {
                    await transaction.CommitAsync(ct);
                    return new(true, CreateReceiptCode.DuplicateRequest, "Este pagamento já possui recibo.", activePayment.Id,
                        existingInLoop.Id, existingInLoop.Number, RedirectPage, correlationId);
                }

                var client = await db.Clients.AsNoTracking().SingleOrDefaultAsync(
                    x => x.Id == activePayment.ClientId && x.AccountId == accountId && !x.IsDeleted, ct);
                if (client is null)
                {
                    await transaction.RollbackAsync(ct);
                    return Failure(CreateReceiptCode.ClientNotFound, "Cliente não encontrado nesta conta.", correlationId);
                }

                receipt = new Receipt
                {
                    AccountId = accountId,
                    PaymentId = activePayment.Id,
                    ClientId = activePayment.ClientId,
                    WorkOrderId = activePayment.WorkOrderId,
                    DocumentId = activePayment.DocumentId,
                    OriginType = activePayment.WorkOrderId.HasValue ? ReceiptOriginType.WorkOrder : activePayment.DocumentId.HasValue ? ReceiptOriginType.Budget : ReceiptOriginType.Standalone,
                    Number = await ReceiptNumberAllocator.NextAsync(db, accountId, ct),
                    Amount = activePayment.Amount,
                    AmountInWords = numberToWords.ToCurrencyWords(activePayment.Amount),
                    PaymentMethod = activePayment.PaymentMethod,
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
                return new(true, CreateReceiptCode.None, "Recibo emitido com sucesso.", activePayment.Id, receipt.Id,
                    receipt.Number, RedirectPage, correlationId);
            }
            catch (Exception ex) when (ShouldRetryPersistenceConflict(ex) && attempt < maxRetries)
            {
                await transaction.RollbackAsync(ct);
                DetachAttemptEntry(receipt);
                await Task.Delay(50 * attempt, ct);
            }
            catch (Exception ex) when (ShouldHandlePersistenceConflict(ex))
            {
                await transaction.RollbackAsync(ct);
                DetachAttemptEntry(receipt);
                var existingFinal = await db.Receipts.AsNoTracking().FirstOrDefaultAsync(
                    x => x.AccountId == accountId && x.PaymentId == paymentId && !x.IsDeleted && x.CancelledAt == null, ct);
                if (existingFinal is not null)
                    return new(true, CreateReceiptCode.DuplicateRequest, "Este pagamento já possui recibo.", paymentId,
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
        var isolation = db.Database.IsRelational() ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted;
        await using var transaction = await db.Database.BeginTransactionAsync(isolation, ct);
        var payment = await db.ManualPayments.SingleOrDefaultAsync(x => x.Id == paymentId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (payment is null || payment.Status == FinancialRecordStatus.Reversed)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
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

    private static bool IsSameRequest(CreateReceiptRequest request, decimal amount, string methodCode, DateTime paidAtUtc, ManualPayment existing) =>
        ManualPaymentIdempotency.Matches(
            request.ClientId, request.DocumentId, request.WorkOrderId,
            amount, methodCode, paidAtUtc,
            existing.ClientId, existing.DocumentId, existing.WorkOrderId,
            existing.Amount, existing.PaymentMethod, existing.PaidAt);

    private void DetachAttemptEntry(object? entity)
    {
        if (entity is null) return;
        var entry = db.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }

    private static bool ShouldRetryPersistenceConflict(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException) return true;
        var sqlState = FindSqlState(exception);
        return sqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected;
    }

    private static bool ShouldHandlePersistenceConflict(Exception exception)
    {
        if (ShouldRetryPersistenceConflict(exception)) return true;
        if (FindSqlState(exception) != PostgresErrorCodes.UniqueViolation) return false;
        var constraint = FindConstraintName(exception);
        if (string.IsNullOrWhiteSpace(constraint)) return true;
        return constraint.Contains("idempotency", StringComparison.OrdinalIgnoreCase) ||
               constraint.Contains("receipt", StringComparison.OrdinalIgnoreCase) ||
               constraint.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
               constraint.Contains("number", StringComparison.OrdinalIgnoreCase) ||
               constraint.Contains("year", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindSqlState(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres) return postgres.SqlState;
            if (current.GetType().GetProperty("SqlState")?.GetValue(current) is string state) return state;
        }
        return null;
    }

    private static string? FindConstraintName(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres) return postgres.ConstraintName;
            if (current.GetType().GetProperty("ConstraintName")?.GetValue(current) is string constraint) return constraint;
        }
        return null;
    }
}
