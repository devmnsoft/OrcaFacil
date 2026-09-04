using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrcaFacil.Application;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Quality;
using OrcaFacil.Persistence;
using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class QualityGateServiceDiTests
{
    [Fact]
    public void QualityGate_And_SchemaContract_Resolve_In_A_Validated_Scope()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddApplication(Directory.GetCurrentDirectory());
        services.AddPersistence();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        Assert.IsType<DatabaseSchemaContractService>(scope.ServiceProvider.GetRequiredService<IDatabaseSchemaContractService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<QualityGateService>());
    }
}

public sealed class DatabaseSchemaContractServiceTests
{
    [Theory]
    [InlineData("documents", "row_version")]
    [InlineData("documents", "template_code")]
    [InlineData("documents", "payment_method")]
    [InlineData("budget_templates", "account_id")]
    public void RegistrationContract_Contains_Critical_Columns(string table, string column) =>
        Assert.True(DatabaseSchemaContractService.RegistrationContract[table].ContainsKey(column));

    [Fact]
    public void RegistrationContract_Is_A_Real_NonEmpty_Contract() =>
        Assert.True(DatabaseSchemaContractService.RegistrationContract.Count >= 21);
}

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void SchemaContract_Is_Registered_Once_As_Scoped()
    {
        var services = new ServiceCollection().AddPersistence();
        var registration = Assert.Single(services.Where(x => x.ServiceType == typeof(IDatabaseSchemaContractService)));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
        Assert.Equal(typeof(DatabaseSchemaContractService), registration.ImplementationType);
    }
}

public sealed class ApiCompositionRootTests
{
    [Fact]
    public void Api_Uses_Central_Persistence_Registration() =>
        Assert.Contains("builder.Services.AddPersistence();", ReadProgram("OrcaFacil.Api"));

    internal static string ReadProgram(string project) => File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", project, "Program.cs"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}

public sealed class WebCompositionRootTests
{
    [Fact]
    public void Web_Uses_Central_Persistence_Registration() =>
        Assert.Contains("builder.Services.AddPersistence();", ApiCompositionRootTests.ReadProgram("OrcaFacil.Web"));
}
