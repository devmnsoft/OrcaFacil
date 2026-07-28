using Microsoft.Extensions.Configuration;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.UnitTests;

public sealed class ConfigurationSourceDescriptorTests
{
    [Fact]
    public void Missing_connection_is_not_configured()
    {
        var configuration = new ConfigurationBuilder().Build();
        Assert.Equal("NotConfigured", ConfigurationSourceDescriptor.Detect(configuration).Name);
    }

    [Fact]
    public void Later_provider_has_priority_without_exposing_secret()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Password=local-secret" })
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Password=environment-secret" })
            .Build();

        var source = ConfigurationSourceDescriptor.Detect(configuration);
        Assert.Equal("AppSettings", source.Name);
        Assert.DoesNotContain("secret", source.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
