using OrcaFacil.Application.Common;
using OrcaFacil.Application.Commercial;
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

        var payment = Invoke(method!, documentId, "Approved", Balance(documentId, workOrderId, 1000m, []));
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

    [Fact]
    public void Approved_quote_with_financial_blockage_routes_to_review_instead_of_payment()
    {
        var documentId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        var method = typeof(QuoteWorkspaceService).GetMethod("NextAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var action = Invoke(method!, documentId, "Approved", Balance(documentId, workOrderId, 1000m,
            ["Há recebimento com vínculo incompatível entre orçamento e ordem."]));

        Assert.Equal("financial-review", action.Code);
        Assert.Equal("/Documents/Details", action.Page);
        Assert.Equal("finance", action.RouteValues?["tab"]);
    }

    [Fact]
    public void Approved_quote_that_is_paid_does_not_offer_a_new_payment()
    {
        var documentId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        var method = typeof(QuoteWorkspaceService).GetMethod("NextAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var action = Invoke(method!, documentId, "Approved", Balance(documentId, workOrderId, 1000m, [], received: 1000m));

        Assert.Equal("receipts", action.Code);
        Assert.Equal("/Documents/Details", action.Page);
        Assert.Equal("finance", action.RouteValues?["tab"]);
    }

    private static NextActionDescriptor Invoke(System.Reflection.MethodInfo method, Guid documentId, string status, CommercialBalance? balance) =>
        Assert.IsType<NextActionDescriptor>(method.Invoke(null, [documentId, status, balance]));

    private static CommercialBalance Balance(Guid documentId, Guid workOrderId, decimal contracted,
        IReadOnlyList<string> blockingWarnings, decimal received = 0m) =>
        CommercialBalanceCalculator.Calculate(documentId, workOrderId, "WorkOrder", contracted,
            received == 0m ? [] : [new CommercialPaymentAmount(received, IsReversed: false)],
            blockingWarnings: blockingWarnings);
}
