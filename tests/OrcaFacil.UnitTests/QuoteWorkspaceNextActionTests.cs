using OrcaFacil.Application.Common;
using OrcaFacil.Application.Documents;
using OrcaFacil.Persistence.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class QuoteWorkspaceNextActionTests
{
    [Fact]
    public void Next_action_routes_match_the_destination_contracts()
    {
        var documentId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        var service = typeof(QuoteWorkspaceService);
        var method = service.GetMethod("NextAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);

        var draft = Invoke(method!, documentId, "Draft", null);
        Assert.Equal("/Documents/CreateBudget", draft.Page);
        Assert.Equal(documentId.ToString(), draft.RouteValues?["id"]);
        Assert.DoesNotContain("documentId", draft.RouteValues?.Keys ?? []);

        var payment = Invoke(method!, documentId, "Approved", workOrderId);
        Assert.Equal("/Payments/Register", payment.Page);
        Assert.Equal(workOrderId.ToString(), payment.RouteValues?["id"]);
        Assert.DoesNotContain("documentId", payment.RouteValues?.Keys ?? []);

        var details = Invoke(method!, documentId, "Sent", null);
        Assert.Equal("/Documents/Details", details.Page);
        Assert.Equal(documentId.ToString(), details.RouteValues?["id"]);
        Assert.Equal("negotiation", details.RouteValues?["tab"]);
    }

    [Fact]
    public void Documents_index_uses_route_values_from_the_next_action()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var page = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Documents", "Index.cshtml"));

        Assert.Contains("asp-all-route-data=\"@quote.NextAction.RouteValues\"", page);
        Assert.DoesNotContain("asp-route-id=\"@quote.Id\">@quote.NextAction.Title", page);
    }

    private static NextActionDescriptor Invoke(System.Reflection.MethodInfo method, Guid documentId, string status, Guid? workOrderId) =>
        Assert.IsType<NextActionDescriptor>(method.Invoke(null, [documentId, status, workOrderId]));
}
