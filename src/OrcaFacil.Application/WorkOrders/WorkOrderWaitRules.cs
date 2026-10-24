using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.WorkOrders;

/// <summary>Pure mapping between the wait reason the operator picks and the status the order moves to.
/// Kept out of the DbContext-dependent service so the rule is unit-testable without a database.</summary>
public static class WorkOrderWaitRules
{
    /// <summary>Resolves the target status for a wait reason; None has no status (rejected).</summary>
    public static bool TryGetStatus(WorkOrderWaitReason reason, out WorkOrderStatus status)
    {
        switch (reason)
        {
            case WorkOrderWaitReason.WaitingCustomer: status = WorkOrderStatus.WaitingCustomer; return true;
            case WorkOrderWaitReason.WaitingMaterial: status = WorkOrderStatus.WaitingMaterial; return true;
            default: status = default; return false;
        }
    }

    /// <summary>Human label used in the timeline so the audit trail reads naturally.</summary>
    public static string Describe(WorkOrderWaitReason reason) => reason switch
    {
        WorkOrderWaitReason.WaitingCustomer => "aguardando o cliente",
        WorkOrderWaitReason.WaitingMaterial => "aguardando material",
        _ => "aguardando"
    };
}
