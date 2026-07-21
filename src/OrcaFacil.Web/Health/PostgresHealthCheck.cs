using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OrcaFacil.Web.Diagnostics;

namespace OrcaFacil.Web.Health;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly DatabaseDiagnosticsService _diagnostics;
    private readonly ILogger<PostgresHealthCheck> _logger;

    public PostgresHealthCheck(DatabaseDiagnosticsService diagnostics, ILogger<PostgresHealthCheck> logger)
    {
        _diagnostics = diagnostics;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _diagnostics.CheckAsync(cancellationToken);
            var missingTables = result.RequiredTables.Where(x => !x.Value).Select(x => x.Key).ToArray();

            if (!result.Connected)
            {
                return HealthCheckResult.Unhealthy("Falha ao conectar no PostgreSQL. Verifique host, porta, database, usuário, senha e firewall.");
            }

            if (!result.SchemaFound || missingTables.Length > 0)
            {
                var message = $"PostgreSQL conectado, mas o schema orcafacil ou tabelas obrigatórias não foram encontrados. Tabelas pendentes: {string.Join(", ", missingTables)}.";
                _logger.LogError("{Message} Último erro: {LastError}", message, result.LastError);
                return HealthCheckResult.Unhealthy(message);
            }

            return HealthCheckResult.Healthy("PostgreSQL conectado, schema orcafacil e tabelas principais encontrados.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            const string message = "Database PostgreSQL não existe ou não está acessível para a connection string informada.";
            _logger.LogError(ex, message);
            return HealthCheckResult.Unhealthy(message, ex);
        }
        catch (NpgsqlException ex)
        {
            const string message = "Falha ao conectar no PostgreSQL. Verifique host, porta, database, usuário, senha e firewall.";
            _logger.LogError(ex, message);
            return HealthCheckResult.Unhealthy(message, ex);
        }
    }
}
