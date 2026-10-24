using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.WorkOrders;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>Covers the wait/overdue states that previously existed in the transition table but were
/// unreachable: nothing in the system could write WaitingCustomer, WaitingMaterial or Overdue.</summary>
public sealed class WorkOrderWaitAndOverdueTests
{
    private readonly WorkOrderStatusTransitionService _transitions = new();

    [Theory]
    [InlineData(WorkOrderWaitReason.WaitingCustomer, WorkOrderStatus.WaitingCustomer)]
    [InlineData(WorkOrderWaitReason.WaitingMaterial, WorkOrderStatus.WaitingMaterial)]
    public void Wait_reason_maps_to_its_status(WorkOrderWaitReason reason, WorkOrderStatus expected)
    {
        Assert.True(WorkOrderWaitRules.TryGetStatus(reason, out var status));
        Assert.Equal(expected, status);
    }

    [Fact]
    public void Wait_reason_none_is_rejected()
    {
        Assert.False(WorkOrderWaitRules.TryGetStatus(WorkOrderWaitReason.None, out var status));
        Assert.Equal(default, status);
    }

    [Theory]
    [InlineData(WorkOrderWaitReason.WaitingCustomer)]
    [InlineData(WorkOrderWaitReason.WaitingMaterial)]
    public void Waiting_states_are_enterable_from_in_progress(WorkOrderWaitReason reason)
    {
        Assert.True(WorkOrderWaitRules.TryGetStatus(reason, out var status));
        Assert.True(_transitions.CanTransition(WorkOrderStatus.InProgress, status));
    }

    [Theory]
    [InlineData(WorkOrderStatus.WaitingCustomer)]
    [InlineData(WorkOrderStatus.WaitingMaterial)]
    public void Waiting_states_can_resume_to_in_progress(WorkOrderStatus waiting) =>
        Assert.True(_transitions.CanTransition(waiting, WorkOrderStatus.InProgress));

    [Theory]
    [InlineData(WorkOrderStatus.WaitingCustomer)]
    [InlineData(WorkOrderStatus.WaitingMaterial)]
    public void Waiting_states_can_be_cancelled(WorkOrderStatus waiting) =>
        Assert.True(_transitions.CanTransition(waiting, WorkOrderStatus.Cancelled));

    [Theory]
    [InlineData(WorkOrderStatus.WaitingCustomer)]
    [InlineData(WorkOrderStatus.WaitingMaterial)]
    public void Waiting_states_cannot_complete_directly(WorkOrderStatus waiting) =>
        Assert.False(_transitions.CanTransition(waiting, WorkOrderStatus.Completed));

    [Fact]
    public void Scheduled_order_can_be_marked_overdue() =>
        Assert.True(_transitions.CanTransition(WorkOrderStatus.Scheduled, WorkOrderStatus.Overdue));

    [Fact]
    public void Overdue_order_can_still_be_started_or_cancelled()
    {
        Assert.True(_transitions.CanTransition(WorkOrderStatus.Overdue, WorkOrderStatus.InProgress));
        Assert.True(_transitions.CanTransition(WorkOrderStatus.Overdue, WorkOrderStatus.Cancelled));
    }

    [Fact]
    public void Completed_order_cannot_be_marked_overdue()
    {
        Assert.False(_transitions.CanTransition(WorkOrderStatus.Completed, WorkOrderStatus.Overdue));
        Assert.False(_transitions.CanTransition(WorkOrderStatus.Cancelled, WorkOrderStatus.Overdue));
    }

    [Theory]
    [InlineData(WorkOrderWaitReason.WaitingCustomer, "aguardando o cliente")]
    [InlineData(WorkOrderWaitReason.WaitingMaterial, "aguardando material")]
    public void Wait_description_is_human_readable(WorkOrderWaitReason reason, string expected) =>
        Assert.Equal(expected, WorkOrderWaitRules.Describe(reason));

    [Fact]
    public void Journey_contract_exposes_wait_operation()
    {
        var method = typeof(ICommercialJourneyService).GetMethod("WaitAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(WorkOrderWaitReason), method!.GetParameters()[1].ParameterType);
    }
}
