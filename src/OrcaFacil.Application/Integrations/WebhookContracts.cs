namespace OrcaFacil.Application.Integrations;

public static class WebhookEventTypes
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    { "proposal.created", "proposal.sent", "proposal.viewed", "proposal.approved", "proposal.rejected", "proposal.change_requested", "work_order.created", "work_order.completed", "payment.registered", "receipt.issued", "client.created", "support_ticket.created" };
}

public sealed record OutgoingWebhookPayload(Guid EventId, string EventType, DateTime OccurredAt, Guid AccountId, string EntityId, string EntityType, object Data);
