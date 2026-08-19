using OrcaFacil.Domain.Enums;
using OrcaFacil.Application.Documents;

namespace OrcaFacil.Application.Commercial;

public record CommercialResult(bool Succeeded, string Code, string Message, Guid? EntityId,
    string? CurrentStatus, string CorrelationId, string? NextAction, string? RedirectPage);
public sealed record WorkOrderResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);
public sealed record PaymentRegistrationResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);
public sealed record ReceiptGenerationResult(bool Succeeded, string Code, string Message, Guid? EntityId, string? CurrentStatus,
    string CorrelationId, string? NextAction, string? RedirectPage) : CommercialResult(Succeeded, Code, Message, EntityId, CurrentStatus, CorrelationId, NextAction, RedirectPage);

public sealed record ManualPaymentRequest(Guid WorkOrderId, decimal Amount, string PaymentMethod, DateTime PaidAt, string? Notes, string IdempotencyKey);
public sealed record FollowUpRequest(Guid DocumentId, DateTime? NextFollowUpAt, string? Note);

public interface IManualPaymentRegistrationService
{
    Task<PaymentRegistrationResult> RegisterAsync(ManualPaymentRequest request, CancellationToken ct = default);
}

public interface ICommercialJourneyService
{
    Task<OrcaFacil.Application.Documents.RevisionResult> CreateRevisionAsync(Guid documentId, string templateCode, CancellationToken ct = default);
    Task<OrcaFacil.Application.Documents.PublicQuoteResult> CreatePublicAccessAsync(Guid documentId, TimeSpan validity, CancellationToken ct = default);
    Task<OrcaFacil.Application.Documents.PublicDecisionResult> DecideAsync(string token, PublicDocumentDecisionType decision, string customerName,
        string? customerContact, string? reason, string? comment, DateTime? desiredDate, bool acceptedTerms,
        string idempotencyKey, string ip, string userAgent, CancellationToken ct = default);
    Task<WorkOrderResult> ConvertToWorkOrderAsync(Guid documentId, CancellationToken ct = default);
    Task<WorkOrderResult> ScheduleAsync(Guid workOrderId, DateTime start, DateTime end, Guid? assigneeId, CancellationToken ct = default);
    Task<WorkOrderResult> StartAsync(Guid workOrderId, CancellationToken ct = default);
    Task<WorkOrderResult> PauseAsync(Guid workOrderId, CancellationToken ct = default);
    Task<WorkOrderResult> ResumeAsync(Guid workOrderId, CancellationToken ct = default);
    Task<WorkOrderResult> CompleteAsync(Guid workOrderId, string? notes, CancellationToken ct = default);
    Task<WorkOrderResult> CancelAsync(Guid workOrderId, string reason, CancellationToken ct = default);
    Task<ReceiptGenerationResult> GenerateReceiptAsync(Guid paymentId, CancellationToken ct = default);
    Task<CommercialResult> ScheduleFollowUpAsync(FollowUpRequest request, CancellationToken ct = default);
    Task<CommercialResult> SnoozeFollowUpAsync(FollowUpRequest request, CancellationToken ct = default);
    Task<CommercialResult> CompleteFollowUpAsync(Guid documentId, string? note, CancellationToken ct = default);
}
