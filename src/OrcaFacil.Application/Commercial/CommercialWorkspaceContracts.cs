using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Commercial;

public sealed record CommercialWorkspaceItem(Guid Id, string Description, decimal Quantity, decimal UnitPrice,
    decimal Discount, decimal Subtotal, decimal Total);
public sealed record CommercialRevisionView(Guid Id, int Number, string Status, DateTime CreatedAt, bool IsCurrent);
public sealed record ClientEngagementView(string Status, DateTime CreatedAt, DateTime ExpiresAt, int ViewCount,
    DateTime? LastViewedAt, string? Decision, DateTime? DecidedAt, string? CustomerName, string? Comment);
public sealed record CommercialTimelineEvent(string Action, string Title, string? Description, DateTime OccurredAt,
    string Origin, string Tone, string Icon);
public sealed record CommercialWorkOrderView(Guid Id, string Number, string Status, DateTime? ScheduledStart,
    DateTime? ScheduledEnd, decimal Paid, decimal Balance, Guid? LatestPaymentId, Guid? LatestReceiptId);
public sealed record CommercialNextAction(string Code, string Title, string Description, string Label,
    string? Handler, string? Page, Guid? RouteId, string Icon);

public sealed record CommercialDocumentWorkspaceView(Guid Id, string Type, string Number, string Status,
    string ClientName, DateTime CreatedAt, DateTime IssueDate, DateTime? ValidUntil, decimal Subtotal,
    decimal Discount, decimal Total, string? Notes, string? Conditions, string Origin, int CurrentRevision,
    bool IsExpired, bool HasChangeRequest, IReadOnlyList<CommercialWorkspaceItem> Items,
    IReadOnlyList<CommercialRevisionView> Revisions, ClientEngagementView? Engagement,
    IReadOnlyList<CommercialTimelineEvent> Timeline, CommercialWorkOrderView? WorkOrder,
    CommercialNextAction NextAction);

public sealed record CommercialPipelineCard(Guid DocumentId, string Number, string ClientName, decimal Total,
    DateTime CreatedAt, DateTime? LastInteractionAt, DateTime? ValidUntil, string Context);
public sealed record CommercialPipelineColumn(string Code, string Title, int Count, decimal Total,
    IReadOnlyList<CommercialPipelineCard> Cards);
public sealed record CommercialAttentionItem(Guid DocumentId, string Severity, string Title, string Description,
    DateTime OccurredAt);
public sealed record CommercialDashboardView(IReadOnlyList<CommercialPipelineColumn> Pipeline,
    IReadOnlyList<CommercialAttentionItem> Attention, int Sent, int Viewed, int Approved, decimal ApprovedValue,
    decimal? ApprovalRate, decimal? AverageTicket);

public interface ICommercialWorkspaceQueryService
{
    Task<CommercialDocumentWorkspaceView?> GetAsync(Guid documentId, CancellationToken ct = default);
    Task<CommercialDashboardView> GetDashboardAsync(CancellationToken ct = default);
}
