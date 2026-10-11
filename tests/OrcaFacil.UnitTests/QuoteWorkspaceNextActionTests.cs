using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class QuoteWorkspaceNextActionTests
{
    [Fact]
    public void Next_action_routes_match_the_destination_contracts()
    {
        var documentId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        var draft = CommercialNextActionResolver.ResolveQuoteAction(new(documentId, "Draft"));
        Assert.Equal("/Documents/CreateBudget", draft.Page);
        Assert.Equal(documentId.ToString(), draft.RouteValues?["id"]);
        Assert.DoesNotContain("documentId", draft.RouteValues?.Keys ?? []);

        var payment = CommercialNextActionResolver.ResolveQuoteAction(new(documentId, "Approved",
            Balance: Balance(documentId, workOrderId, 1000m, []), WorkOrderId: workOrderId,
            WorkOrderStatus: "Completed", WorkOrderBalance: 1000m));
        Assert.Equal("/Payments/Register", payment.Page);
        Assert.Equal(workOrderId.ToString(), payment.RouteValues?["id"]);
        Assert.DoesNotContain("documentId", payment.RouteValues?.Keys ?? []);

        var details = CommercialNextActionResolver.ResolveQuoteAction(new(documentId, "Sent"));
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

        var action = CommercialNextActionResolver.ResolveQuoteAction(new(documentId, "Approved",
            Balance: Balance(documentId, workOrderId, 1000m, ["Há recebimento com vínculo incompatível entre orçamento e ordem."]),
            WorkOrderId: workOrderId, WorkOrderStatus: "Completed", WorkOrderBalance: 1000m));

        Assert.Equal("financial-review", action.Code);
        Assert.Equal("/Documents/Details", action.Page);
        Assert.Equal("finance", action.RouteValues?["tab"]);
    }

    [Fact]
    public void Approved_quote_that_is_paid_does_not_offer_a_new_payment()
    {
        var documentId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        var action = CommercialNextActionResolver.ResolveQuoteAction(new(documentId, "Approved",
            Balance: Balance(documentId, workOrderId, 1000m, [], received: 1000m),
            WorkOrderId: workOrderId, WorkOrderStatus: "Completed", WorkOrderPaid: 1000m, WorkOrderBalance: 0m,
            LatestPaymentId: Guid.NewGuid()));

        Assert.Equal("receipt-pending", action.Code);
        Assert.Equal("/WorkOrders/Details", action.Page);
    }

    [Fact]
    public void Completed_work_order_with_open_balance_keeps_payment_as_next_action()
    {
        var documentId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        var action = CommercialNextActionResolver.ResolveQuoteAction(new(documentId, "ConvertedToWorkOrder",
            Balance: Balance(documentId, workOrderId, 1000m, [], received: 300m),
            WorkOrderId: workOrderId, WorkOrderStatus: "Completed", WorkOrderPaid: 300m, WorkOrderBalance: 700m));

        Assert.Equal("payment", action.Code);
        Assert.Equal("/Payments/Register", action.Page);
        Assert.Equal(workOrderId.ToString(), action.RouteValues?["id"]);
    }

    [Fact]
    public void Paid_zero_contract_does_not_offer_receipt_without_eligible_payment()
    {
        var documentId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        var action = CommercialNextActionResolver.ResolveQuoteAction(new(documentId, "ConvertedToWorkOrder",
            Balance: Balance(documentId, workOrderId, 0m, []),
            WorkOrderId: workOrderId, WorkOrderStatus: "Completed", WorkOrderPaid: 0m, WorkOrderBalance: 0m));

        Assert.Equal("finance", action.Code);
        Assert.Equal("/Documents/Details", action.Page);
        Assert.Equal("finance", action.RouteValues?["tab"]);
    }

    private static CommercialBalance Balance(Guid documentId, Guid workOrderId, decimal contracted,
        IReadOnlyList<string> blockingWarnings, decimal received = 0m) =>
        CommercialBalanceCalculator.Calculate(documentId, workOrderId, "WorkOrder", contracted,
            received == 0m ? [] : [new CommercialPaymentAmount(received, IsReversed: false)],
            blockingWarnings: blockingWarnings);
}
