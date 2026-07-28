using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrcaFacil.Web.Configuration;

namespace OrcaFacil.Web.Health;

public sealed class LocalSettingsHealthCheck(IHostEnvironment environment) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
            return Task.FromResult(HealthCheckResult.Healthy());

        var path = Path.Combine(environment.ContentRootPath, LocalConfigurationExtensions.FileName);
        return Task.FromResult(File.Exists(path)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("A configuração local do banco não foi encontrada."));
    }
}
