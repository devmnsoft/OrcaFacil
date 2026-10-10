namespace OrcaFacil.Application.Commercial;

public sealed record CommercialRevisionCandidate(Guid Id, Guid DocumentId, int VersionNumber, decimal Total, bool IsCurrent);

public sealed record CommercialWorkOrderCandidate(Guid Id, Guid SourceDocumentId, Guid? SourceRevisionId, string Number,
    decimal TotalSnapshot, DateTime CreatedAt);

public sealed record CommercialContractOrigin(Guid DocumentId, Guid? WorkOrderId, Guid? RevisionId, string Source,
    decimal ContractedAmount, string? WorkOrderNumber)
{
    public bool HasWorkOrder => WorkOrderId.HasValue;
}

public static class CommercialContractOriginSelector
{
    public static CommercialContractOrigin Select(Guid documentId, decimal documentTotal,
        IEnumerable<CommercialRevisionCandidate> revisions, IEnumerable<CommercialWorkOrderCandidate> workOrders,
        Guid? preferredWorkOrderId = null)
    {
        var revisionList = revisions
            .Where(x => x.DocumentId == documentId)
            .OrderByDescending(x => x.VersionNumber)
            .ThenByDescending(x => x.Id)
            .ToArray();
        var orderList = workOrders
            .Where(x => x.SourceDocumentId == documentId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToArray();

        var selectedOrder = preferredWorkOrderId.HasValue
            ? orderList.FirstOrDefault(x => x.Id == preferredWorkOrderId.Value)
            : null;

        selectedOrder ??= orderList.FirstOrDefault();
        if (selectedOrder is not null)
            return new CommercialContractOrigin(documentId, selectedOrder.Id, selectedOrder.SourceRevisionId,
                "WorkOrder", selectedOrder.TotalSnapshot, selectedOrder.Number);

        var currentRevision = revisionList.FirstOrDefault(x => x.IsCurrent) ?? revisionList.FirstOrDefault();
        return currentRevision is null
            ? new CommercialContractOrigin(documentId, null, null, "Document", documentTotal, null)
            : new CommercialContractOrigin(documentId, null, currentRevision.Id, "CurrentRevision", currentRevision.Total, null);
    }
}
