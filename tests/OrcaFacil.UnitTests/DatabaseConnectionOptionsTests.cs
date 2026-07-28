using Microsoft.Extensions.Configuration;
using OrcaFacil.Persistence.Diagnostics;

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
