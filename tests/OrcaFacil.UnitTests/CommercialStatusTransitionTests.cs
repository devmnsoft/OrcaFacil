using OrcaFacil.Application.Documents;
using OrcaFacil.Application.WorkOrders;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialStatusTransitionTests
{
    private readonly DocumentStatusTransitionService _documents = new();

    [Theory]
    [InlineData(DocumentStatus.Draft, DocumentStatus.Ready)]
    [InlineData(DocumentStatus.Ready, DocumentStatus.Sent)]
    [InlineData(DocumentStatus.Sent, DocumentStatus.Viewed)]
    [InlineData(DocumentStatus.Sent, DocumentStatus.InNegotiation)]
    [InlineData(DocumentStatus.Viewed, DocumentStatus.InNegotiation)]
    [InlineData(DocumentStatus.InNegotiation, DocumentStatus.Sent)]
    [InlineData(DocumentStatus.Viewed, DocumentStatus.Approved)]
    [InlineData(DocumentStatus.InNegotiation, DocumentStatus.Approved)]
    [InlineData(DocumentStatus.InNegotiation, DocumentStatus.Rejected)]
    [InlineData(DocumentStatus.Approved, DocumentStatus.ConvertedToWorkOrder)]
    public void Allows_supported_document_transition(DocumentStatus current, DocumentStatus next) =>
        Assert.True(_documents.CanTransition(current, next));

    [Theory]
    [InlineData(DocumentStatus.Rejected, DocumentStatus.Approved)]
    [InlineData(DocumentStatus.Cancelled, DocumentStatus.Sent)]
    [InlineData(DocumentStatus.Expired, DocumentStatus.Approved)]
    [InlineData(DocumentStatus.ConvertedToWorkOrder, DocumentStatus.Draft)]
    public void Rejects_unsafe_document_transition(DocumentStatus current, DocumentStatus next) =>
        Assert.Throws<InvalidOperationException>(() => _documents.EnsureCanTransition(current, next));

    [Fact]
    public void Completed_and_cancelled_work_orders_are_terminal()
    {
        var service = new WorkOrderStatusTransitionService();
        Assert.False(service.CanTransition(WorkOrderStatus.Completed, WorkOrderStatus.InProgress));
        Assert.False(service.CanTransition(WorkOrderStatus.Cancelled, WorkOrderStatus.Completed));
    }
}
