using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.UnitTests;

public sealed class RazorRouteUniquenessTests : IClassFixture<RouteApplicationFactory>
{
    private readonly RouteApplicationFactory _factory;

    public RazorRouteUniquenessTests(RouteApplicationFactory factory) => _factory = factory;

    [Fact]
    public void Razor_routes_should_not_have_incompatible_competitors()
    {
        var endpoints = RouteEndpoints();
        var duplicates = endpoints
            .GroupBy(endpoint => new
            {
                Pattern = Normalize(endpoint.RoutePattern.RawText),
                Methods = string.Join(',', endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Order() ?? ["*"]),
                endpoint.Order
            })
            .Where(group => group.Select(endpoint => endpoint.DisplayName).Distinct().Count() > 1)
            .Select(group => $"{group.Key.Pattern} ({group.Key.Methods}, order {group.Key.Order}): " +
                             string.Join(" | ", group.Select(endpoint => endpoint.DisplayName)))
            .ToArray();

        Assert.True(duplicates.Length == 0, "Rotas concorrentes encontradas:\n" + string.Join('\n', duplicates));
    }

    [Fact]
    public void Dashboard_route_should_have_only_one_endpoint()
    {
        var dashboard = RouteEndpoints()
            .Where(endpoint => string.Equals(endpoint.RoutePattern.RawText?.Trim('/'), "Dashboard", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Single(dashboard);
        Assert.Contains("/Dashboard/Index", dashboard[0].DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Documents")]
    [InlineData("/Clients")]
    [InlineData("/Services")]
    [InlineData("/Templates")]
    [InlineData("/Profile")]
    [InlineData("/Subscription")]
    [InlineData("/Notifications")]
    public async Task Main_authenticated_routes_never_return_not_found_or_server_error(string route)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync(route);
        Assert.True((int)response.StatusCode is 200 or 302 or 401 or 403,
            $"{route} retornou {(int)response.StatusCode}.");
    }

    private RouteEndpoint[] RouteEndpoints() => _factory.Services
        .GetServices<EndpointDataSource>()
        .SelectMany(source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .ToArray();

    private static string Normalize(string? route) => "/" + (route ?? string.Empty)
        .Trim('/').Replace("/Index", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
}

public sealed class RouteApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", string.Empty);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDatabaseConfigurationState>();
            services.AddSingleton<IDatabaseConfigurationState>(new DatabaseConfigurationState(
                true, true, true, "Test", "", "127.0.0.1", 1, "test", "test", "Disable", "test",
                DatabaseConfigurationValidationCode.Valid, "", ""));
        });
    }
}
