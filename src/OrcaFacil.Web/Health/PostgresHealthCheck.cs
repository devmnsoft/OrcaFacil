using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrcaFacil.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Health;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly IDatabaseDiagnosticsService _diagnostics;
    private readonly ILogger<PostgresHealthCheck> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDatabaseConfigurationState _configurationState;
    private readonly IDatabaseSchemaContractService _schemaContract;

    public PostgresHealthCheck(IDatabaseDiagnosticsService diagnostics, ILogger<PostgresHealthCheck> logger, IServiceScopeFactory scopeFactory,
        IDatabaseConfigurationState configurationState, IDatabaseSchemaContractService schemaContract)
    {
        _diagnostics = diagnostics;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configurationState = configurationState;
        _schemaContract = schemaContract;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_configurationState.IsValid)
            return HealthCheckResult.Unhealthy(_configurationState.AdminMessage);

        var result = await _diagnostics.CheckAsync(cancellationToken);
        var contract = result.CanConnect
            ? await _schemaContract.CheckRegistrationContractAsync(cancellationToken)
            : null;
        var migrationsCurrent = false;
        if (result.CanConnect)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrcaFacilDbContext>();
            migrationsCurrent = !(await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any();
        }
        if (result.CanConnect && result.SchemaExists && result.MissingTables.Count == 0 &&
            result.FreePlanExists && result.PublishedFreeVersionExists && migrationsCurrent && contract?.IsValid == true)
        {
            return HealthCheckResult.Healthy("PostgreSQL conectado e schema orcafacil íntegro.");
        }

        _logger.LogWarning("Diagnóstico PostgreSQL com pendências: {MissingTables} {SchemaIssues} {Error}",
            string.Join(",", result.MissingTables), contract?.Issues.Count ?? 0, result.Error);
        return HealthCheckResult.Unhealthy(result.Error ?? "Schema orcafacil incompleto.");
    }
}
