namespace OrcaFacil.Web.Configuration;

/// <summary>
/// Resolves the server-side pepper without ever including its value in diagnostics.
/// A deterministic fallback is intentionally restricted to the automated test environment.
/// </summary>
public static class TechnicalFingerprintPepperResolver
{
    public const string ConfigurationKey = "Security:TechnicalFingerprintPepper";
    public const string TestingFallback = "orcafacil-testing-only-technical-fingerprint-pepper";
    public const string MissingConfigurationMessage =
        "Security:TechnicalFingerprintPepper não configurado. " +
        "Configure via variável de ambiente Security__TechnicalFingerprintPepper " +
        "ou user-secrets no ambiente local. Em produção este valor é obrigatório.";

    public static string Resolve(IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredValue = configuration[ConfigurationKey];
        if (!string.IsNullOrWhiteSpace(configuredValue))
            return configuredValue;

        if (string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase))
            return TestingFallback;

        throw new InvalidOperationException(MissingConfigurationMessage);
    }
}
