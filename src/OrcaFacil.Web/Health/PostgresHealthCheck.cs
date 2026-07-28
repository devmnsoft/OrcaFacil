using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrcaFacil.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Health;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly IDatabaseDiagnosticsService _diagnostics;
    private readonly ILogger<PostgresHealthCheck> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresHealthCheck(IDatabaseDiagnosticsService diagnostics, ILogger<PostgresHealthCheck> logger, IServiceScopeFactory scopeFactory)
    {
        _diagnostics = diagnostics;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = await _diagnostics.CheckAsync(cancellationToken);
        var migrationsCurrent = false;
        if (result.CanConnect)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrcaFacilDbContext>();
            migrationsCurrent = !(await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any();
        }
        if (result.CanConnect && result.SchemaExists && result.MissingTables.Count == 0 &&
            result.FreePlanExists && result.PublishedFreeVersionExists && migrationsCurrent)
        {
            return HealthCheckResult.Healthy("PostgreSQL conectado e schema orcafacil íntegro.");
        }

        _logger.LogWarning("Diagnóstico PostgreSQL com pendências: {MissingTables} {Error}", string.Join(",", result.MissingTables), result.Error);
        return HealthCheckResult.Unhealthy(result.Error ?? "Schema orcafacil incompleto.");
    }
}
