using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Commercial;

public record CommercialResult(bool Succeeded, string Code, string Message, Guid? EntityId,
    string? CurrentStatus, string CorrelationId, string? NextAction, string? RedirectPage);
public sealed record QuoteLifecycleResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);
public sealed record PublicQuoteResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage, string? Token = null) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);
public sealed record PublicDecisionResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);
public sealed record RevisionResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);
public sealed record WorkOrderResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);
public sealed record PaymentRegistrationResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);
public sealed record ReceiptGenerationResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);

public sealed record ManualPaymentRequest(Guid WorkOrderId, decimal Amount, string PaymentMethod, DateTime PaidAt, string? Notes, string IdempotencyKey);

public interface IManualPaymentRegistrationService
{
    Task<PaymentRegistrationResult> RegisterAsync(ManualPaymentRequest request, CancellationToken ct = default);
}

public interface ICommercialJourneyService
{
    Task<RevisionResult> CreateRevisionAsync(Guid documentId, string templateCode, CancellationToken ct = default);
    Task<PublicQuoteResult> CreatePublicAccessAsync(Guid documentId, TimeSpan validity, CancellationToken ct = default);
    Task<PublicDecisionResult> DecideAsync(string token, PublicDocumentDecisionType decision, string customerName,
        string? reason, string? comment, string idempotencyKey, string ip, string userAgent, CancellationToken ct = default);
    Task<WorkOrderResult> ConvertToWorkOrderAsync(Guid documentId, CancellationToken ct = default);
    Task<WorkOrderResult> ScheduleAsync(Guid workOrderId, DateTime start, DateTime end, Guid? assigneeId, CancellationToken ct = default);
    Task<WorkOrderResult> StartAsync(Guid workOrderId, CancellationToken ct = default);
    Task<WorkOrderResult> CompleteAsync(Guid workOrderId, string? notes, CancellationToken ct = default);
    Task<ReceiptGenerationResult> GenerateReceiptAsync(Guid paymentId, CancellationToken ct = default);
}
