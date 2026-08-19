using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.WorkOrders;

public interface IWorkOrderStatusTransitionService
{
    bool CanTransition(WorkOrderStatus current, WorkOrderStatus next);
    void EnsureCanTransition(WorkOrderStatus current, WorkOrderStatus next);
}

public sealed class WorkOrderStatusTransitionService : IWorkOrderStatusTransitionService
{
    private static readonly IReadOnlyDictionary<WorkOrderStatus, HashSet<WorkOrderStatus>> Allowed = new Dictionary<WorkOrderStatus, HashSet<WorkOrderStatus>>
    {
        [WorkOrderStatus.Planned] = [WorkOrderStatus.Scheduled, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Scheduled] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled, WorkOrderStatus.Overdue],
        [WorkOrderStatus.Overdue] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.InProgress] = [WorkOrderStatus.Paused, WorkOrderStatus.WaitingCustomer, WorkOrderStatus.WaitingMaterial, WorkOrderStatus.Completed, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Paused] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.WaitingCustomer] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.WaitingMaterial] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled]
    };

    public bool CanTransition(WorkOrderStatus current, WorkOrderStatus next) => current == next || Allowed.TryGetValue(current, out var values) && values.Contains(next);
    public void EnsureCanTransition(WorkOrderStatus current, WorkOrderStatus next)
    {
        if (!CanTransition(current, next)) throw new InvalidOperationException($"A mudança de {current} para {next} não é permitida.");
    }
}
