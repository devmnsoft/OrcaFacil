using OrcaFacil.Application.Commercial;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialContractOriginSelectorTests
{
    [Fact]
    public void Existing_work_order_remains_the_contract_origin_even_when_current_revision_changes()
    {
        var documentId = Guid.NewGuid();
        var approvedRevisionId = Guid.NewGuid();
        var draftRevisionId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        var origin = CommercialContractOriginSelector.Select(documentId, 1500m,
        [
            new CommercialRevisionCandidate(approvedRevisionId, documentId, 1, 1000m, IsCurrent: false),
            new CommercialRevisionCandidate(draftRevisionId, documentId, 2, 1500m, IsCurrent: true)
        ],
        [
            new CommercialWorkOrderCandidate(workOrderId, documentId, approvedRevisionId, "OS-001", 1000m, DateTime.UtcNow.AddDays(-2))
        ]);

        Assert.Equal(workOrderId, origin.WorkOrderId);
        Assert.Equal(approvedRevisionId, origin.RevisionId);
        Assert.Equal(1000m, origin.ContractedAmount);
        Assert.Equal("WorkOrder", origin.Source);
    }

    [Fact]
    public void Preferred_work_order_wins_when_a_specific_contract_is_requested()
    {
        var documentId = Guid.NewGuid();
        var olderOrderId = Guid.NewGuid();
        var newerOrderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var origin = CommercialContractOriginSelector.Select(documentId, 1000m, [],
        [
            new CommercialWorkOrderCandidate(olderOrderId, documentId, null, "OS-001", 800m, now.AddDays(-2)),
            new CommercialWorkOrderCandidate(newerOrderId, documentId, null, "OS-002", 1200m, now)
        ], olderOrderId);

        Assert.Equal(olderOrderId, origin.WorkOrderId);
        Assert.Equal(800m, origin.ContractedAmount);
    }
}
