using Microsoft.Extensions.DependencyInjection;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Web.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

internal static class DashboardSource
{
    public static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }
}

public sealed class CommercialWorkspaceQueryServiceTests
{
    [Fact]
    public void DashboardQueries_AreTenantBound_NoTracking_AndSequential()
    {
        var source = DashboardSource.Read("src", "OrcaFacil.Persistence", "Services", "CommercialWorkspaceQueryService.cs");
        Assert.Contains("x.AccountId == AccountId", source);
        Assert.Contains("AsNoTracking()", source);
        Assert.DoesNotContain("Task.WhenAll", source);
        Assert.DoesNotContain("Task.Run", source);
    }
}

public sealed class DashboardExperienceServiceTests
{
    [Fact]
    public void Composition_AwaitsScopedServicesWithoutParallelFanOut()
    {
        var source = DashboardSource.Read("src", "OrcaFacil.Web", "Services", "DashboardExperienceService.cs");
        Assert.DoesNotContain("Task.WhenAll", source);
        Assert.DoesNotContain("dashboardTask", source);
        Assert.Contains("await dashboardQueries.GetDashboardAsync", source);
        Assert.Contains("await commercialWorkspace.GetDashboardAsync", source);
    }
}

public sealed class DashboardDbContextConcurrencyTests
{
    [Fact]
    public void DashboardDataServices_DoNotStartParallelTasks()
    {
        var persistence = DashboardSource.Read("src", "OrcaFacil.Persistence", "Services", "CommercialWorkspaceQueryService.cs");
        var web = DashboardSource.Read("src", "OrcaFacil.Web", "Services", "DashboardExperienceService.cs");
        Assert.DoesNotContain("Task.WhenAll", persistence + web);
        Assert.DoesNotContain("Task.Run", persistence + web);
    }
}

public sealed class ServiceLifetimeRegistrationTests
{
    [Fact]
    public void DashboardServices_AreScoped_NotSingleton()
    {
        using var factory = new RouteApplicationFactory();
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var firstDashboard = firstScope.ServiceProvider.GetRequiredService<IDashboardExperienceService>();
        var firstCommercial = firstScope.ServiceProvider.GetRequiredService<ICommercialWorkspaceQueryService>();
        Assert.Same(firstDashboard, firstScope.ServiceProvider.GetRequiredService<IDashboardExperienceService>());
        Assert.Same(firstCommercial, firstScope.ServiceProvider.GetRequiredService<ICommercialWorkspaceQueryService>());
        Assert.NotSame(firstDashboard, secondScope.ServiceProvider.GetRequiredService<IDashboardExperienceService>());
        Assert.NotSame(firstCommercial, secondScope.ServiceProvider.GetRequiredService<ICommercialWorkspaceQueryService>());
    }
}

public sealed class DashboardPageTests
{
    [Fact]
    public void Page_HasUsefulEmptyState_RealMetrics_AndNoRandomData()
    {
        var page = DashboardSource.Read("src", "OrcaFacil.Web", "Pages", "Dashboard", "Index.cshtml");
        Assert.Contains("Seu primeiro documento começa aqui", page);
        Assert.Contains("dashboard.DocumentsThisMonth", page);
        Assert.Contains("experience.Commercial.Attention.Count", page);
        Assert.DoesNotContain("Math.random", page, StringComparison.OrdinalIgnoreCase);
    }
}
