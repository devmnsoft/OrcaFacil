using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace OrcaFacil.Web.Health;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresHealthCheck> _logger;

    public PostgresHealthCheck(IConfiguration configuration, ILogger<PostgresHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            const string message = "ConnectionStrings:DefaultConnection ausente. Configure appsettings, user-secrets ou ConnectionStrings__DefaultConnection.";
            _logger.LogError(message);
            return HealthCheckResult.Unhealthy(message);
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("select to_regclass('identity.users') is not null and to_regclass('core.documents') is not null", connection);
            var hasRequiredTables = (bool?)await command.ExecuteScalarAsync(cancellationToken) == true;

            if (!hasRequiredTables)
            {
                const string message = "PostgreSQL conectado, mas schemas/tabelas obrigatórios não foram encontrados. Execute database/script_completop.sql ou migrations.";
                _logger.LogError(message);
                return HealthCheckResult.Unhealthy(message);
            }

            return HealthCheckResult.Healthy("PostgreSQL conectado e tabelas base encontradas.");
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
