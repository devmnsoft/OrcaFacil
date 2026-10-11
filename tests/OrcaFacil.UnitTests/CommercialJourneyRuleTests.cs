using OrcaFacil.Application.Commercial;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialJourneyRuleTests
{
    [Fact]
    public void Unspecified_receipt_time_uses_business_zone_not_the_process_zone()
    {
        var civil = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        Assert.Contains(CommercialClock.BusinessTimeZone.Id, new[] { "America/Sao_Paulo", "E. South America Standard Time" });
        var utc = CommercialClock.NormalizeToUtc(civil);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void Same_payment_key_matches_only_the_same_order_amount_method_and_instant()
    {
        var when = new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc);
        var order = Guid.NewGuid();
        Assert.True(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, order, 300m, "pix", when.AddMilliseconds(400)));
        Assert.False(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, order, 700m, "pix", when));
        Assert.False(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, Guid.NewGuid(), 300m, "pix", when));
        Assert.False(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, order, 300m, "cash", when));
        Assert.False(ManualPaymentIdempotency.Matches(order, 300m, "pix", when, order, 300m, "pix", when.AddMinutes(1)));
    }

    [Fact]
    public void Partial_receipts_use_decimal_and_stop_at_the_order_total()
    {
        var total = 1000m;
        var first = CommercialCalculator.Round(300m);
        var second = CommercialCalculator.Round(700m);
        var balance = total - first;
        Assert.Equal(700m, balance);
        Assert.Equal(0m, balance - second);
        Assert.True(CommercialCalculator.Round(0.01m) > 0m);
        Assert.Throws<ArgumentException>(() => CommercialCalculator.Calculate([new(1, 1000m, 1000.01m)]));
    }

    [Fact]
    public void Dashboard_document_metrics_are_scoped_by_account_and_keep_the_actor()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var queries = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Queries", "DashboardQueries.cs"));
        var experience = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Services", "DashboardExperienceService.cs"));
        Assert.Contains("account_id = @accountId", queries);
        Assert.DoesNotContain("where user_id = @userId and is_deleted = false", queries);
        Assert.Contains("actorUserId", queries);
        Assert.Contains("currentUser.UserId", experience);
        Assert.Contains("currentAccount.AccountId", experience);
    }

    [Fact]
    public void Repeated_conversion_looks_up_the_existing_order_before_the_approved_guard()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialJourneyService.cs"));
        var lookup = source.IndexOf("SourceDocumentId == document.Id", StringComparison.Ordinal);
        var approved = source.IndexOf("status != DocumentStatus.Approved", StringComparison.Ordinal);
        Assert.True(lookup > 0 && approved > lookup);
        Assert.Contains("IdempotencyConflict", source);
        Assert.Contains("ManualPaymentIdempotency.Matches", source);
        Assert.Contains("FinancialRecordStatus.Active", source);
    }

    [Fact]
    public void Direct_budget_receipt_respects_blocking_financial_divergence()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "ReceiptApplicationService.cs"));
        var balanceLookup = source.IndexOf("balances.GetForDocumentAsync", StringComparison.Ordinal);
        var blockGuard = source.IndexOf("balance.HasBlockingDivergence", StringComparison.Ordinal);
        var paymentCreation = source.IndexOf("db.ManualPayments.Add(payment)", StringComparison.Ordinal);

        Assert.True(balanceLookup > 0);
        Assert.True(blockGuard > balanceLookup);
        Assert.True(paymentCreation > blockGuard);
        Assert.Contains("vínculo incompatível entre orçamento, ordem e recebimento", source);
    }

    [Fact]
    public void Work_order_balance_delegates_nullable_origin_to_the_canonical_classifier()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialBalanceService.cs"));

        Assert.Contains("CommercialPaymentClassifier.IsSelectedOrigin(payment.DocumentId, payment.WorkOrderId, documentId, selectedWorkOrderId)", source);
        Assert.DoesNotContain("documentId.HasValue && CommercialPaymentClassifier.IsSelectedOrigin", source);
        Assert.True(CommercialPaymentClassifier.IsSelectedOrigin(null, Guid.NewGuid(), null, Guid.NewGuid()) is false);

        var selectedOrderId = Guid.NewGuid();
        Assert.True(CommercialPaymentClassifier.IsSelectedOrigin(null, selectedOrderId, null, selectedOrderId));
    }

    [Fact]
    public void Sharing_link_success_returns_to_the_accessible_mobile_actions_panel()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var model = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Documents", "Details.cshtml.cs"));
        var page = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Documents", "Details.cshtml"));
        var script = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "wwwroot", "js", "commercial-workspace.js"));

        Assert.Contains("return RedirectToPage(new { id, tab = \"sharing\" })", model);
        Assert.Contains("aria-controls=\"document-command-sharing document-command-manage document-command-danger\"", page);
        Assert.Contains("if (event.key !== 'Escape'", script);
        Assert.Contains("commandToggle?.focus({ preventScroll: true })", script);
        Assert.Contains("if (requestedRawTab === 'sharing') focusCommandSection('sharing')", script);
    }

    [Fact]
    public void Quote_list_and_detail_share_the_canonical_next_action_resolver()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var listService = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "QuoteWorkspaceService.cs"));
        var detailService = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialWorkspaceQueryService.cs"));
        var resolver = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Application", "Commercial", "CommercialNextActionResolver.cs"));

        Assert.Contains("CommercialNextActionResolver.ResolveQuoteAction", listService);
        Assert.Contains("CommercialNextActionResolver.ResolveDocumentAction", detailService);
        Assert.Contains("visualizacao nao e aprovacao", resolver);
        Assert.Contains("WorkOrderPaid > 0m && context.LatestPaymentId is not null", resolver);
        Assert.DoesNotContain("private static NextActionDescriptor NextAction", listService);
        Assert.DoesNotContain("private static CommercialNextAction ResolveNext", detailService);
    }

    [Fact]
    public void Document_details_exposes_financial_review_without_hiding_excluded_amounts()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var contracts = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Application", "Commercial", "CommercialWorkspaceContracts.cs"));
        var service = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "CommercialWorkspaceQueryService.cs"));
        var page = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Documents", "Details.cshtml"));

        Assert.Contains("CommercialFinancialReviewView", contracts);
        Assert.Contains("ExcludedAmount", contracts);
        Assert.Contains("BuildFinancialReview", service);
        Assert.Contains("CommercialPaymentClassifier", service);
        Assert.Contains("Excluído por inconsistência", page);
        Assert.Contains("Consulta de histórico, comprovantes válidos existentes e estorno continuam disponíveis", page);
    }
}
