using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Web.Health;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly IDatabaseDiagnosticsService _diagnostics;
    private readonly ILogger<PostgresHealthCheck> _logger;

    public PostgresHealthCheck(IDatabaseDiagnosticsService diagnostics, ILogger<PostgresHealthCheck> logger)
    {
        _diagnostics = diagnostics;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = await _diagnostics.CheckAsync(cancellationToken);
        if (result.CanConnect && result.SchemaExists && result.MissingTables.Count == 0)
        {
            return HealthCheckResult.Healthy("PostgreSQL conectado e schema orcafacil íntegro.");
        }

        _logger.LogWarning("Diagnóstico PostgreSQL com pendências: {MissingTables} {Error}", string.Join(",", result.MissingTables), result.Error);
        return HealthCheckResult.Unhealthy(result.Error ?? "Schema orcafacil incompleto.");
    }
}
