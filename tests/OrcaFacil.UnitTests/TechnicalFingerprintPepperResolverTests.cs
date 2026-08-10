using Microsoft.Extensions.Configuration;
using OrcaFacil.Web.Configuration;

namespace OrcaFacil.UnitTests;

public sealed class TechnicalFingerprintPepperResolverTests
{
    [Fact]
    public void Development_uses_explicit_development_configuration()
    {
        const string developmentValue = "development-value-from-settings";
        var configuration = Configuration((TechnicalFingerprintPepperResolver.ConfigurationKey, developmentValue));

        var resolved = TechnicalFingerprintPepperResolver.Resolve(configuration, "Development");

        Assert.Equal(developmentValue, resolved);
    }

    [Fact]
    public void Development_allows_environment_or_user_secrets_provider_override()
    {
        const string overriddenValue = "long-local-override-value";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TechnicalFingerprintPepperResolver.ConfigurationKey] = "development-default"
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TechnicalFingerprintPepperResolver.ConfigurationKey] = overriddenValue
            })
            .Build();

        Assert.Equal(overriddenValue, TechnicalFingerprintPepperResolver.Resolve(configuration, "Development"));
    }

    [Fact]
    public void Testing_uses_test_only_fallback_when_not_configured()
    {
        var resolved = TechnicalFingerprintPepperResolver.Resolve(Configuration(), "Testing");

        Assert.Equal(TechnicalFingerprintPepperResolver.TestingFallback, resolved);
    }

    [Fact]
    public void Production_without_pepper_fails_with_actionable_message_and_no_secret()
    {
        const string unrelatedSecret = "must-never-appear-in-diagnostics";
        var configuration = Configuration(("SomeOtherSecret", unrelatedSecret));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            TechnicalFingerprintPepperResolver.Resolve(configuration, "Production"));

        Assert.Equal(TechnicalFingerprintPepperResolver.MissingConfigurationMessage, exception.Message);
        Assert.Contains("Security__TechnicalFingerprintPepper", exception.Message);
        Assert.Contains("obrigatório", exception.Message);
        Assert.DoesNotContain(unrelatedSecret, exception.ToString());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Hosted_environments_use_configured_pepper_without_exposing_it(string environment)
    {
        const string configuredSecret = "production-secret-used-only-by-this-test";
        var configuration = Configuration((TechnicalFingerprintPepperResolver.ConfigurationKey, configuredSecret));

        var resolved = TechnicalFingerprintPepperResolver.Resolve(configuration, environment);

        Assert.Equal(configuredSecret, resolved);
    }

    [Fact]
    public void Staging_without_pepper_does_not_use_a_fallback()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TechnicalFingerprintPepperResolver.Resolve(Configuration(), "Staging"));
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(value => value.Key, value => (string?)value.Value))
            .Build();
}
