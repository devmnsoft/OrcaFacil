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
    public void QualityGate_and_schema_contract_resolve_with_scope_validation_enabled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddApplication(CompositionRootSource.RepositoryRoot);
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

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void Schema_contract_and_quality_gate_are_scoped_and_clock_is_available()
    {
        var services = new ServiceCollection();
        services.AddPersistence();
        services.AddApplication(CompositionRootSource.RepositoryRoot);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDatabaseSchemaContractService)
            && descriptor.ImplementationType == typeof(DatabaseSchemaContractService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(QualityGateService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IClock)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IDatabaseSchemaContractService)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
    }
}

public sealed class DatabaseSchemaContractServiceTests
{
    [Fact]
    public void Implementation_matches_the_real_interface_contract()
    {
        var method = typeof(IDatabaseSchemaContractService).GetMethod(nameof(IDatabaseSchemaContractService.CheckRegistrationContractAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DatabaseSchemaContractResult>), method.ReturnType);
        var parameter = Assert.Single(method.GetParameters());
        Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
        Assert.True(parameter.HasDefaultValue);
        Assert.Contains(typeof(IDatabaseSchemaContractService), typeof(DatabaseSchemaContractService).GetInterfaces());
    }

    [Theory]
    [InlineData("documents", "row_version")]
    [InlineData("documents", "template_code")]
    [InlineData("documents", "payment_method")]
    [InlineData("budget_templates", "account_id")]
    [InlineData("plan_feature_values", "plan_version_id")]
    public void Registration_contract_contains_critical_columns(string table, string column)
    {
        Assert.True(DatabaseSchemaContractService.RegistrationContract.TryGetValue(table, out var columns));
        Assert.True(columns.ContainsKey(column));
    }

    [Fact]
    public void Registration_contract_contains_every_critical_table()
    {
        string[] expected =
        [
            "users", "account_members", "business_accounts", "issuer_profiles", "clients", "contacts",
            "service_catalog_items", "documents", "document_items", "document_revisions", "budget_templates",
            "budget_template_items", "audit_logs", "account_onboarding_states", "email_outbox_messages", "plans",
            "plan_versions", "features", "plan_feature_values", "subscriptions", "notifications"
        ];

        Assert.All(expected, table => Assert.True(
            DatabaseSchemaContractService.RegistrationContract.ContainsKey(table), $"Missing contract for {table}."));
        Assert.All(expected, table => Assert.NotEmpty(DatabaseSchemaContractService.RegistrationContract[table]));
    }
}

public sealed class ApiCompositionRootTests
{
    [Fact]
    public void Api_uses_central_application_and_persistence_registration()
    {
        var source = CompositionRootSource.Read("OrcaFacil.Api");

        Assert.Contains("builder.Services.AddApplication(repositoryRoot);", source, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddPersistence();", source, StringComparison.Ordinal);
    }
}

public sealed class WebCompositionRootTests
{
    [Fact]
    public void Web_uses_central_registration_and_maps_system_health()
    {
        var source = CompositionRootSource.Read("OrcaFacil.Web");

        Assert.Contains("builder.Services.AddApplication(repositoryRoot);", source, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddPersistence();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapGet(\"/SystemHealth\"", source, StringComparison.Ordinal);
    }
}

internal static class CompositionRootSource
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string Read(string project) => File.ReadAllText(Path.Combine(RepositoryRoot, "src", project, "Program.cs"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
