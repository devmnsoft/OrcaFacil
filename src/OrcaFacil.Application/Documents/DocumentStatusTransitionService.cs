using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Documents;

public interface IDocumentStatusTransitionService
{
    bool CanTransition(DocumentStatus current, DocumentStatus next);
    void EnsureCanTransition(DocumentStatus current, DocumentStatus next);
}

public sealed class DocumentStatusTransitionService : IDocumentStatusTransitionService
{
    private static readonly IReadOnlyDictionary<DocumentStatus, HashSet<DocumentStatus>> Allowed =
        new Dictionary<DocumentStatus, HashSet<DocumentStatus>>
        {
            [DocumentStatus.Draft] = [DocumentStatus.Ready, DocumentStatus.Cancelled],
            [DocumentStatus.Ready] = [DocumentStatus.Sent, DocumentStatus.Draft, DocumentStatus.Cancelled],
            [DocumentStatus.Sent] = [DocumentStatus.Viewed, DocumentStatus.Approved, DocumentStatus.Rejected, DocumentStatus.Expired, DocumentStatus.Cancelled],
            [DocumentStatus.Viewed] = [DocumentStatus.InNegotiation, DocumentStatus.Approved, DocumentStatus.Rejected, DocumentStatus.Expired, DocumentStatus.Cancelled],
            [DocumentStatus.InNegotiation] = [DocumentStatus.Sent, DocumentStatus.Cancelled],
            [DocumentStatus.Approved] = [DocumentStatus.ConvertedToWorkOrder, DocumentStatus.Cancelled]
        };

    public bool CanTransition(DocumentStatus current, DocumentStatus next) =>
        current == next || Allowed.TryGetValue(current, out var destinations) && destinations.Contains(next);

    public void EnsureCanTransition(DocumentStatus current, DocumentStatus next)
    {
        if (!CanTransition(current, next))
            throw new InvalidOperationException($"A mudança de {current} para {next} não é permitida.");
    }
}
