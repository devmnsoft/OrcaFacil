using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OrcaFacil.Persistence.Diagnostics;

public sealed record DatabaseConnectionOptions(
    string Host, int Port, string Database, string Username, string SslMode,
    int Timeout, bool Pooling, bool HasPassword, string Source)
{
    public static bool TryCreate(IConfiguration configuration, out DatabaseConnectionOptions? options, out string error)
    {
        options = null;
        var value = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(value)) { error = "ConnectionStrings:DefaultConnection não foi configurada."; return false; }
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(value);
            options = new(cs.Host ?? "", cs.Port, cs.Database ?? "", cs.Username ?? "", cs.SslMode.ToString(),
                cs.Timeout, cs.Pooling, !string.IsNullOrWhiteSpace(cs.Password), ConfigurationSourceDescriptor.Detect(configuration).Name);
            error = DatabaseConnectionOptionsValidator.Validate(options);
            return error.Length == 0;
        }
        catch (ArgumentException) { error = "ConnectionStrings:DefaultConnection possui formato inválido."; return false; }
    }
}

public static class DatabaseConnectionOptionsValidator
{
    public static string Validate(DatabaseConnectionOptions value)
    {
        if (string.IsNullOrWhiteSpace(value.Host)) return "O host do banco não foi configurado.";
        if (value.Port is < 1 or > 65535) return "A porta do banco é inválida.";
        if (string.IsNullOrWhiteSpace(value.Database)) return "O nome do banco não foi configurado.";
        if (string.IsNullOrWhiteSpace(value.Username)) return "O usuário do banco não foi configurado.";
        if (!value.HasPassword) return "A senha do banco não foi fornecida por um provedor seguro.";
        if (value.Timeout is < 1 or > 120) return "O timeout deve estar entre 1 e 120 segundos.";
        if (!value.Pooling) return "O pooling de conexões deve estar habilitado.";
        return string.Empty;
    }
}

public sealed record DatabaseConnectionDescriptor(string Host, int Port, string Database, string Username,
    string SslMode, string Source, string Fingerprint, string Environment)
{
    public static DatabaseConnectionDescriptor From(DatabaseConnectionOptions value, string environment)
    {
        var canonical = $"{value.Host}:{value.Port}/{value.Database}|{value.Username}|{value.SslMode}";
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..12].ToLowerInvariant();
        return new(value.Host, value.Port, value.Database, value.Username, value.SslMode, value.Source, fingerprint, environment);
    }
}
