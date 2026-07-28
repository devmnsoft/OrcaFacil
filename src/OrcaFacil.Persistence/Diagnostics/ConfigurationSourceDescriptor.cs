using Microsoft.Extensions.Configuration;

namespace OrcaFacil.Persistence.Diagnostics;

/// <summary>Describes only the logical provider of a setting; it never exposes the setting value.</summary>
public sealed record ConfigurationSourceDescriptor(string Name, string Detail)
{
    private const string ConnectionKey = "ConnectionStrings:DefaultConnection";

    public static ConfigurationSourceDescriptor Detect(IConfiguration configuration)
    {
        if (configuration is not IConfigurationRoot root)
            return new("AppSettings", "Configuration");

        foreach (var provider in root.Providers.Reverse())
        {
            // TryGet is used solely to identify the winning provider. The value is discarded and never returned or logged.
            if (!provider.TryGet(ConnectionKey, out _)) continue;
            var identity = provider.ToString() ?? provider.GetType().Name;
            if (identity.Contains("EnvironmentVariables", StringComparison.OrdinalIgnoreCase))
                return new("EnvironmentVariable", "ConnectionStrings__DefaultConnection");
            if (identity.Contains("appsettings.Local.json", StringComparison.OrdinalIgnoreCase))
                return new("LocalJson", "appsettings.Local.json");
            if (identity.Contains("secrets.json", StringComparison.OrdinalIgnoreCase))
                return new("UserSecrets", "User secrets");
            if (identity.Contains("appsettings.Development.json", StringComparison.OrdinalIgnoreCase))
                return new("AppSettingsDevelopment", "appsettings.Development.json");
            if (identity.Contains("appsettings.json", StringComparison.OrdinalIgnoreCase))
                return new("AppSettings", "appsettings.json");
            return new("AppSettings", provider.GetType().Name);
        }

        return new("NotConfigured", "ConnectionStrings:DefaultConnection");
    }
}
