namespace OrcaFacil.Web.Configuration;

public static class LocalConfigurationExtensions
{
    public const string FileName = "appsettings.Local.json";

    /// <summary>Adds the optional developer file and then restores environment variables as the highest-priority provider.</summary>
    public static WebApplicationBuilder AddOrcaFacilLocalConfiguration(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
            return builder;

        builder.Configuration
            .AddJsonFile(FileName, optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder;
    }
}
