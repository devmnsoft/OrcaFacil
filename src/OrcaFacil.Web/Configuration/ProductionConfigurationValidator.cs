namespace OrcaFacil.Web.Configuration;
public sealed class ProductionConfigurationValidator(IConfiguration configuration, IWebHostEnvironment environment, ILogger<ProductionConfigurationValidator> logger) : IHostedService
{
 public Task StartAsync(CancellationToken ct)
 {
  if (environment.EnvironmentName is not ("Development" or "Production" or "Testing")) throw new InvalidOperationException("APP_ENVIRONMENT_INVALID");
  if (environment.IsProduction() && string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection"))) throw new InvalidOperationException("DATABASE_CONFIGURATION_REQUIRED");
  if (environment.IsProduction() && configuration.GetValue<bool>("Email:Required") && (string.IsNullOrWhiteSpace(configuration["Email:Host"]) || string.IsNullOrWhiteSpace(configuration["Email:Password"]))) throw new InvalidOperationException("SMTP_CONFIGURATION_REQUIRED");
  var configuredPath=configuration["Operational:LogDirectory"] ?? Path.Combine(environment.ContentRootPath,"logs");
  try { Directory.CreateDirectory(configuredPath); var probe=Path.Combine(configuredPath,$".write-{Guid.NewGuid():N}"); File.WriteAllText(probe,string.Empty); File.Delete(probe); }
  catch(Exception ex) { throw new InvalidOperationException("LOG_DIRECTORY_NOT_WRITABLE",ex); }
  logger.LogInformation("CONFIGURATION_VALIDATED Environment {Environment} LogDirectory {LogDirectory}",environment.EnvironmentName,configuredPath);
  return Task.CompletedTask;
 }
 public Task StopAsync(CancellationToken ct)=>Task.CompletedTask;
}
