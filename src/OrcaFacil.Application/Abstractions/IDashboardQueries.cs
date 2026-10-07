using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Application.Abstractions;

public interface IDashboardQueries
{
    /// <summary>
    /// Actor identity stays on <paramref name="actorUserId"/> (plan and usage of the person).
    /// Commercial document metrics are scoped to <paramref name="accountId"/>.
    /// </summary>
    Task<DashboardDto> GetDashboardAsync(Guid actorUserId, Guid accountId, CancellationToken ct = default);
}
