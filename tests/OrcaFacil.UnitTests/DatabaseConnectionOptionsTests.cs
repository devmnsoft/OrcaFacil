using Microsoft.Extensions.Configuration;
using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class DatabaseConnectionOptionsTests
{
    [Fact]
    public void Missing_connection_string_is_rejected_without_exposing_a_secret()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.False(DatabaseConnectionOptions.TryCreate(configuration, out _, out var error));
        Assert.Contains("DefaultConnection", error);
    }

    [Fact]
    public void Descriptor_has_safe_fingerprint_and_never_contains_password()
    {
        var values = new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] =
            "Host=localhost;Port=5432;Database=orcafacil;Username=app;Password=local-secret;Pooling=true;Timeout=15" };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        Assert.True(DatabaseConnectionOptions.TryCreate(configuration, out var options, out _));
        var descriptor = DatabaseConnectionDescriptor.From(options!, "Test");
        Assert.Equal(12, descriptor.Fingerprint.Length);
        Assert.DoesNotContain("local-secret", descriptor.ToString());
    }

    [Theory]
    [InlineData("Host=localhost;Database=orca;Username=app")]
    [InlineData("Host=localhost;Database=orca;Username=app;Password=")]
    [InlineData("Host=localhost;Database=orca;Username=app;Password=INFORME_SUA_SENHA")]
    [InlineData("Host=localhost;Database=orca;Username=app;Password=ALTERE_A_SENHA_AQUI")]
    [InlineData("Host=localhost;Database=orca;Username=app;Password=<informada-localmente>")]
    public void Missing_or_placeholder_password_is_rejected(string connectionString)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:DefaultConnection"] = connectionString }).Build();

        Assert.False(DatabaseConnectionOptions.TryCreate(configuration, out var options, out var error));
        Assert.False(options?.HasPassword);
        Assert.Contains("senha válida", error);
        Assert.DoesNotContain("INFORME", error);
    }

    [Theory]
    [InlineData("Host=localhost;Port=1;Database=orca;Username=app;Password=secret;Pooling=true")]
    [InlineData("Host=localhost;Port=5432;Database=unavailable;Username=app;Password=secret;Pooling=true")]
    public void Sentinel_database_configurations_are_rejected(string connectionString)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:DefaultConnection"] = connectionString }).Build();
        Assert.False(DatabaseConnectionOptions.TryCreate(configuration, out _, out _));
        Assert.False(DatabaseConfigurationState.Create(configuration, "/tmp/local.json").IsValid);
    }

    [Fact]
    public void Special_characters_in_password_are_accepted_and_never_retained_by_state()
    {
        const string secret = "a senha!@#$%^&*()";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:DefaultConnection"] = $"Host=localhost;Database=orca;Username=app;Password={secret}" }).Build();

        var state = DatabaseConfigurationState.Create(configuration, "/tmp/appsettings.Local.json");

        Assert.True(state.IsValid);
        Assert.DoesNotContain(secret, state.ToString());
    }

    [Fact]
    public void Invalid_environment_override_has_specific_safe_diagnostic()
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost;Database=orca;Username=app");
            configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            var state = DatabaseConfigurationState.Create(configuration, "/tmp/appsettings.Local.json");
            Assert.Equal(DatabaseConfigurationValidationCode.EnvironmentOverrideInvalid, state.ValidationCode);
            Assert.Contains("sobrescrevendo", state.AdminMessage);
            Assert.DoesNotContain("Username=", state.ToString());
        }
        finally { Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previous); }
    }

    [Fact]
    public void SqlState_28P01_is_classified_as_infrastructure_authentication_failure()
    {
        var failure = PostgresFailureClassifier.Classify(new FakePostgresException("28P01"));
        Assert.Equal(DatabaseFailureCategory.Authentication, failure.Category);
        Assert.DoesNotContain("28P01", failure.PublicMessage);
        Assert.Contains("senha configurada", failure.AdminMessage);
    }

    private sealed class FakePostgresException(string sqlState) : Exception { public string SqlState { get; } = sqlState; }
}
