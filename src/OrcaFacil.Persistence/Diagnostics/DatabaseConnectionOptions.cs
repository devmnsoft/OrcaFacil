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
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "ConnectionStrings:DefaultConnection não foi configurada.";
            return false;
        }

        try
        {
            var cs = new NpgsqlConnectionStringBuilder(value);
            var passwordValid = DatabaseConnectionOptionsValidator.IsPasswordValid(cs.Password);
            options = new(cs.Host ?? "", cs.Port, cs.Database ?? "", cs.Username ?? "", cs.SslMode.ToString(),
                cs.Timeout, cs.Pooling, passwordValid, ConfigurationSourceDescriptor.Detect(configuration).Name);
            error = DatabaseConnectionOptionsValidator.Validate(options);
            return error.Length == 0;
        }
        catch (ArgumentException)
        {
            error = "ConnectionStrings:DefaultConnection possui formato inválido.";
            return false;
        }
    }
}

public static class DatabaseConnectionOptionsValidator
{
    private static readonly HashSet<string> PlaceholderPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INFORME_SUA_SENHA", "ALTERE_A_SENHA_AQUI", "<informada-localmente>",
        "YOUR_PASSWORD", "CHANGE_ME", "CHANGEME", "PASSWORD", "SUA_SENHA"
    };

    public static bool IsPasswordValid(string? password) =>
        !string.IsNullOrWhiteSpace(password) && !PlaceholderPasswords.Contains(password.Trim());

    public static string Validate(DatabaseConnectionOptions value)
    {
        if (string.IsNullOrWhiteSpace(value.Host)) return "O host do banco não foi configurado.";
        if (value.Port == 1) return "A porta 1 é uma sentinela inválida e não pode ser usada pelo banco.";
        if (value.Port is < 1 or > 65535) return "A porta do banco é inválida.";
        if (string.IsNullOrWhiteSpace(value.Database)) return "O nome do banco não foi configurado.";
        if (value.Database.Equals("unavailable", StringComparison.OrdinalIgnoreCase))
            return "O nome de banco 'unavailable' é uma sentinela inválida.";
        if (string.IsNullOrWhiteSpace(value.Username)) return "O usuário do banco não foi configurado.";
        if (!value.HasPassword) return "A configuração do banco não possui uma senha válida.";
        if (value.Timeout is < 1 or > 120) return "O timeout deve estar entre 1 e 120 segundos.";
        if (!value.Pooling) return "O pooling de conexões deve estar habilitado.";
        return string.Empty;
    }
}

/// <summary>Applies the documented operational alias before ASP.NET's normal configuration priority is evaluated.</summary>
public static class DatabaseConnectionStringResolver
{
    public const string MissingMessage = "Connection string DefaultConnection não configurada. Configure appsettings.Development.json, user-secrets ou ConnectionStrings__DefaultConnection.";

    public static string ResolveRequired(IConfiguration configuration)
    {
        var operationalAlias = Environment.GetEnvironmentVariable("ORCAFACIL_DATABASE_URL");
        var value = !string.IsNullOrWhiteSpace(operationalAlias)
            ? operationalAlias
            : configuration.GetConnectionString("DefaultConnection");
        var effective = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:DefaultConnection"] = value }).Build();
        if (!DatabaseConnectionOptions.TryCreate(effective, out _, out var error))
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(value) ? MissingMessage : $"DefaultConnection inválida: {error}");
        return value!;
    }

    public static void ApplyOperationalAlias(IConfiguration configuration)
    {
        var value = Environment.GetEnvironmentVariable("ORCAFACIL_DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(value)) configuration["ConnectionStrings:DefaultConnection"] = value;
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
