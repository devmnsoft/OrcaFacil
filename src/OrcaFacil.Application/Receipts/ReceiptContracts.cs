using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Receipts;

public enum CreateReceiptCode
{
    None, AccountRequired, AccessDenied, ClientNotFound, WorkOrderNotFound,
    DocumentNotFound, InvalidOrigin, InvalidAmount, InvalidDate,
    InvalidPaymentMethod, DuplicateRequest, PlanLimitReached,
    ConcurrencyConflict, Unexpected
}

public sealed record CreateReceiptRequest(Guid AccountId, Guid ClientId, ReceiptOriginType OriginType,
    Guid? WorkOrderId, Guid? DocumentId, decimal Amount, string PaymentMethod, DateTime PaidAt,
    string? City, string ServiceDescription, string? Notes, string IdempotencyKey,
    Guid? LegacyDocumentId = null);

public sealed record CreateReceiptResult(bool Succeeded, CreateReceiptCode Code, string Message,
    Guid? PaymentId, Guid? ReceiptId, string? Number, string RedirectPage, string CorrelationId);

public interface IReceiptApplicationService
{
    Task<CreateReceiptResult> CreateAsync(CreateReceiptRequest request, CancellationToken ct = default);
    Task<bool> CancelAsync(Guid receiptId, string reason, CancellationToken ct = default);
    Task<bool> MarkSharedAsync(Guid receiptId, CancellationToken ct = default);
    Task<bool> ReversePaymentAsync(Guid paymentId, string reason, CancellationToken ct = default);
}
