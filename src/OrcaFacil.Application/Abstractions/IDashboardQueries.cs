using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Application.Abstractions;

public interface IDashboardQueries
{
    Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken ct = default);
}
