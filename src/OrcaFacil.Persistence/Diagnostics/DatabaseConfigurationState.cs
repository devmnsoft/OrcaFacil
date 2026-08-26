using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OrcaFacil.Persistence.Diagnostics;

public enum DatabaseConfigurationValidationCode
{
    ConnectionStringMissing, PasswordMissing, PlaceholderPassword, InvalidFormat,
    EnvironmentOverrideInvalid, LocalFileMissing, Valid
}

public interface IDatabaseConfigurationState
{
    bool IsConfigured { get; }
    bool IsValid { get; }
    bool HasPassword { get; }
    string Source { get; }
    string ExpectedLocalFilePath { get; }
    string Host { get; }
    int Port { get; }
    string Database { get; }
    string Username { get; }
    string SslMode { get; }
    string Fingerprint { get; }
    DatabaseConfigurationValidationCode ValidationCode { get; }
    string PublicMessage { get; }
    string AdminMessage { get; }
}

public sealed record DatabaseConfigurationState(
    bool IsConfigured, bool IsValid, bool HasPassword, string Source,
    string ExpectedLocalFilePath, string Host, int Port, string Database, string Username,
    string SslMode, string Fingerprint, DatabaseConfigurationValidationCode ValidationCode,
    string PublicMessage, string AdminMessage) : IDatabaseConfigurationState
{
    public const string PublicUnavailableMessage = "Não conseguimos acessar os dados agora.";

    public static DatabaseConfigurationState Create(IConfiguration configuration, string expectedLocalFilePath)
    {
        var raw = configuration.GetConnectionString("DefaultConnection");
        var source = ConfigurationSourceDescriptor.Detect(configuration);
        var environmentOverride = source.Name == "EnvironmentVariable";
        if (string.IsNullOrWhiteSpace(raw))
            return Invalid(false, false, source.Name, expectedLocalFilePath, "", 0, "", "", "", "missing",
                DatabaseConfigurationValidationCode.ConnectionStringMissing, "A connection string do banco não foi configurada.");

        try
        {
            var cs = new NpgsqlConnectionStringBuilder(raw);
            var hasAnyPassword = !string.IsNullOrWhiteSpace(cs.Password);
            var validPassword = DatabaseConnectionOptionsValidator.IsPasswordValid(cs.Password);
            var placeholder = hasAnyPassword && !validPassword;
            var metadataValid = !string.IsNullOrWhiteSpace(cs.Host) && !string.IsNullOrWhiteSpace(cs.Database) &&
                                !string.IsNullOrWhiteSpace(cs.Username) && cs.Port is > 1 and <= 65535 &&
                                !cs.Database.Equals("unavailable", StringComparison.OrdinalIgnoreCase);
            var fingerprint = Fingerprints(cs.Host, cs.Port, cs.Database, cs.Username, cs.SslMode.ToString());

            if (!metadataValid)
                return Invalid(true, validPassword, source.Name, expectedLocalFilePath, cs.Host, cs.Port, cs.Database, cs.Username,
                    cs.SslMode.ToString(), fingerprint, environmentOverride ? DatabaseConfigurationValidationCode.EnvironmentOverrideInvalid : DatabaseConfigurationValidationCode.InvalidFormat,
                    environmentOverride ? OverrideMessage() : "A configuração do banco está incompleta.");
            if (!validPassword)
                return Invalid(true, false, source.Name, expectedLocalFilePath, cs.Host, cs.Port, cs.Database, cs.Username,
                    cs.SslMode.ToString(), fingerprint, environmentOverride ? DatabaseConfigurationValidationCode.EnvironmentOverrideInvalid :
                    (placeholder ? DatabaseConfigurationValidationCode.PlaceholderPassword : DatabaseConfigurationValidationCode.PasswordMissing),
                    environmentOverride ? OverrideMessage() : "A configuração do banco não possui uma senha válida.");

            return new(true, true, true, source.Name, expectedLocalFilePath, cs.Host, cs.Port, cs.Database, cs.Username,
                cs.SslMode.ToString(), fingerprint, DatabaseConfigurationValidationCode.Valid, PublicUnavailableMessage, "Configuração válida.");
        }
        catch (ArgumentException)
        {
            return Invalid(true, false, source.Name, expectedLocalFilePath, "", 0, "", "", "", Fingerprints(raw),
                environmentOverride ? DatabaseConfigurationValidationCode.EnvironmentOverrideInvalid : DatabaseConfigurationValidationCode.InvalidFormat,
                environmentOverride ? OverrideMessage() : "A connection string possui formato inválido.");
        }
    }

    private static string OverrideMessage() =>
        "Uma variável de ambiente está sobrescrevendo appsettings.Local.json, mas sua configuração está incompleta.";

    private static DatabaseConfigurationState Invalid(bool configured, bool password, string source, string path,
        string host, int port, string database, string username, string ssl, string fingerprint,
        DatabaseConfigurationValidationCode code, string adminMessage) =>
        new(configured, false, password, source, path, host, port, database, username, ssl, fingerprint,
            code, PublicUnavailableMessage, adminMessage);

    private static string Fingerprints(params object?[] parts)
    {
        var canonical = string.Join("|", parts.Select(x => x?.ToString() ?? ""));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..12].ToLowerInvariant();
    }
}
