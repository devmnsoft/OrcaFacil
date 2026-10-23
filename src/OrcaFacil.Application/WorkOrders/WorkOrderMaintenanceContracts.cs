namespace OrcaFacil.Application.WorkOrders;

/// <summary>Background maintenance for operational work-order states that no user action can reach.</summary>
public interface IWorkOrderOverdueService
{
    /// <summary>Marks scheduled orders whose planned end already passed as Overdue. Returns how many changed.</summary>
    Task<int> MarkOverdueAsync(CancellationToken ct = default);
}
