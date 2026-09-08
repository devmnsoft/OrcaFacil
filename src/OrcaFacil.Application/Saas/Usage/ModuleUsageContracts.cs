namespace OrcaFacil.Application.Saas.Usage;

public interface IModuleUsageEventService
{
    Task RegisterAsync(Guid accountId, Guid? userId, string moduleCode, string eventCode, string? featureCode,
        string? route, string correlationId, CancellationToken ct = default);
}

public interface IModuleUsageSnapshotService
{
    Task RefreshAsync(Guid accountId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
}
