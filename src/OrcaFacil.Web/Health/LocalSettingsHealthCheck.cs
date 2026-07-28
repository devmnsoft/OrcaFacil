using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Health;

public sealed class LocalSettingsHealthCheck(IHostEnvironment environment, IDatabaseConfigurationState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!state.IsValid)
            return Task.FromResult(HealthCheckResult.Unhealthy(state.AdminMessage));

        if (environment.IsDevelopment() && !File.Exists(state.ExpectedLocalFilePath))
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Configuração válida fornecida por {state.Source}; appsettings.Local.json não foi encontrado."));

        return Task.FromResult(HealthCheckResult.Healthy($"Configuração válida fornecida por {state.Source}."));
    }
}
